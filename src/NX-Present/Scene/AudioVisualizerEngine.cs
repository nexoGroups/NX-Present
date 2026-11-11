using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace NXPresent;

public sealed class AudioVisualizerEngine : IDisposable
{
    private static readonly Lazy<AudioVisualizerEngine> instance = new(() => new AudioVisualizerEngine());
    public static AudioVisualizerEngine Instance => instance.Value;

    private readonly Thread captureThread;
    private readonly float[] rawFft = new float[64];
    private readonly float[] smoothedFft = new float[64];
    private readonly float[] waveform = new float[128];
    private readonly object sync = new();
    private volatile bool running = true;
    private float beatPulse = 0f;
    private long lastAudioPacketTime;

    private AudioVisualizerEngine()
    {
        captureThread = new Thread(CaptureLoop)
        {
            IsBackground = true,
            Name = "NX-Present-AudioCapture"
        };
        captureThread.SetApartmentState(ApartmentState.MTA);
        captureThread.Start();
    }

    public float[] GetSpectrum(int count, double sensitivity = 1.0)
    {
        count = Math.Clamp(count, 8, 128);
        var result = new float[count];
        lock (sync)
        {
            for (int i = 0; i < count; i++)
            {
                int srcIndex = (int)((double)i / count * smoothedFft.Length);
                srcIndex = Math.Clamp(srcIndex, 0, smoothedFft.Length - 1);
                float val = smoothedFft[srcIndex] * (float)sensitivity;
                result[i] = Math.Clamp(val, 0f, 1f);
            }
        }
        return result;
    }

    public float[] GetWaveform(int count)
    {
        count = Math.Clamp(count, 16, 256);
        var result = new float[count];
        lock (sync)
        {
            for (int i = 0; i < count; i++)
            {
                int srcIndex = (int)((double)i / count * waveform.Length);
                srcIndex = Math.Clamp(srcIndex, 0, waveform.Length - 1);
                result[i] = waveform[srcIndex];
            }
        }
        return result;
    }

    public float GetBeatPulse()
    {
        lock (sync) { return beatPulse; }
    }

    private void CaptureLoop()
    {
        IntPtr pEnumerator = IntPtr.Zero;
        IntPtr pDevice = IntPtr.Zero;
        IntPtr pAudioClient = IntPtr.Zero;
        IntPtr pCaptureClient = IntPtr.Zero;
        IntPtr pFormat = IntPtr.Zero;

        try
        {
            var enumeratorGuid = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
            var iidEnumerator = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
            var iidAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
            var iidCaptureClient = new Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317");

            int hr = CoCreateInstance(ref enumeratorGuid, IntPtr.Zero, 1 /* CLSCTX_INPROC_SERVER */, ref iidEnumerator, out pEnumerator);
            if (hr >= 0 && pEnumerator != IntPtr.Zero)
            {
                var enumerator = (IMMDeviceEnumerator)Marshal.GetObjectForIUnknown(pEnumerator);
                if (enumerator.GetDefaultAudioEndpoint(0 /* eRender */, 1 /* eMultimedia */, out pDevice) >= 0 && pDevice != IntPtr.Zero)
                {
                    var device = (IMMDevice)Marshal.GetObjectForIUnknown(pDevice);
                    if (device.Activate(ref iidAudioClient, 1, IntPtr.Zero, out pAudioClient) >= 0 && pAudioClient != IntPtr.Zero)
                    {
                        var audioClient = (IAudioClient)Marshal.GetObjectForIUnknown(pAudioClient);
                        if (audioClient.GetMixFormat(out pFormat) >= 0 && pFormat != IntPtr.Zero)
                        {
                            var waveFormat = Marshal.PtrToStructure<WAVEFORMATEX>(pFormat);
                            // AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000
                            hr = audioClient.Initialize(0 /* SHARED */, 0x00020000, 1000000 /* 100ms */, 0, pFormat, IntPtr.Zero);
                            if (hr >= 0)
                            {
                                if (audioClient.GetService(ref iidCaptureClient, out pCaptureClient) >= 0 && pCaptureClient != IntPtr.Zero)
                                {
                                    var captureClient = (IAudioCaptureClient)Marshal.GetObjectForIUnknown(pCaptureClient);
                                    audioClient.Start();

                                    ProcessAudioStream(captureClient, waveFormat);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            App.Log(ex);
        }
        finally
        {
            if (pCaptureClient != IntPtr.Zero) Marshal.Release(pCaptureClient);
            if (pAudioClient != IntPtr.Zero) Marshal.Release(pAudioClient);
            if (pDevice != IntPtr.Zero) Marshal.Release(pDevice);
            if (pEnumerator != IntPtr.Zero) Marshal.Release(pEnumerator);
            if (pFormat != IntPtr.Zero) CoTaskMemFree(pFormat);
        }

        // If WASAPI capture exited or failed, run synthetic fallback loop so visualizer still dances
        RunSyntheticLoop();
    }

    private void ProcessAudioStream(IAudioCaptureClient capture, WAVEFORMATEX format)
    {
        int channels = format.nChannels;
        int bitsPerSample = format.wBitsPerSample;
        bool isFloat = format.wFormatTag == 3 /* WAVE_FORMAT_IEEE_FLOAT */ || format.wFormatTag == 0xFFFE;

        while (running)
        {
            int hr = capture.GetBuffer(out IntPtr pData, out uint numFrames, out uint flags, out _, out _);
            if (hr >= 0 && numFrames > 0 && pData != IntPtr.Zero)
            {
                lastAudioPacketTime = Stopwatch.GetTimestamp();
                try
                {
                    ProcessPcmData(pData, (int)numFrames, channels, bitsPerSample, isFloat, flags);
                }
                finally
                {
                    capture.ReleaseBuffer(numFrames);
                }
            }
            else
            {
                ApplyDecay();
                // Check if audio has been silent for a while; if so, blend in subtle ambient pulse
                double silentSeconds = Stopwatch.GetElapsedTime(lastAudioPacketTime).TotalSeconds;
                if (silentSeconds > 1.5)
                {
                    ApplyIdlePulse();
                }
                Thread.Sleep(15);
            }
        }
    }

    private void ProcessPcmData(IntPtr pData, int frames, int channels, int bitsPerSample, bool isFloat, uint flags)
    {
        if (frames <= 0 || pData == IntPtr.Zero || (flags & 1 /* AUDCLNT_BUFFERFLAGS_SILENT */) != 0)
        {
            ApplyDecay();
            return;
        }

        float[] samples = new float[Math.Min(frames, 512)];
        if (samples.Length == 0)
        {
            ApplyDecay();
            return;
        }
        int sampleStep = Math.Max(1, frames / samples.Length);

        if (isFloat && bitsPerSample == 32)
        {
            float[] frameData = new float[frames * channels];
            Marshal.Copy(pData, frameData, 0, frameData.Length);
            for (int i = 0; i < samples.Length; i++)
            {
                int idx = i * sampleStep * channels;
                if (idx < frameData.Length)
                {
                    float left = frameData[idx];
                    float right = channels > 1 && idx + 1 < frameData.Length ? frameData[idx + 1] : left;
                    samples[i] = (left + right) * 0.5f;
                }
            }
        }
        else if (bitsPerSample == 16)
        {
            short[] frameData = new short[frames * channels];
            Marshal.Copy(pData, frameData, 0, frameData.Length);
            for (int i = 0; i < samples.Length; i++)
            {
                int idx = i * sampleStep * channels;
                if (idx < frameData.Length)
                {
                    float left = frameData[idx] / 32768f;
                    float right = channels > 1 && idx + 1 < frameData.Length ? frameData[idx + 1] / 32768f : left;
                    samples[i] = (left + right) * 0.5f;
                }
            }
        }

        // Compute Frequency Energy Bins (approximate FFT via sub-band energy integration)
        float totalEnergy = 0f;
        int bands = rawFft.Length;
        int windowSize = samples.Length / bands;
        if (windowSize < 2) windowSize = 2;

        lock (sync)
        {
            // Copy samples to waveform
            for (int i = 0; i < waveform.Length; i++)
            {
                int sampleIdx = (int)((double)i / waveform.Length * samples.Length);
                if (sampleIdx < samples.Length)
                    waveform[i] = samples[sampleIdx];
            }

            for (int b = 0; b < bands; b++)
            {
                float bandSum = 0f;
                int start = Math.Min(b * windowSize, samples.Length - 1);
                int end = Math.Min(start + windowSize, samples.Length);

                for (int s = start; s < end; s++)
                {
                    float val = Math.Abs(samples[s]);
                    bandSum += val;
                }
                float avg = bandSum / Math.Max(1, end - start);
                // Logarithmic frequency boost for higher frequencies
                float freqBoost = 1.0f + (b / (float)bands) * 2.2f;
                float currentVal = Math.Min(1.0f, avg * freqBoost * 2.5f);

                // Smooth attack and decay
                if (currentVal > smoothedFft[b])
                    smoothedFft[b] = smoothedFft[b] * 0.3f + currentVal * 0.7f;
                else
                    smoothedFft[b] = smoothedFft[b] * 0.85f + currentVal * 0.15f;

                totalEnergy += smoothedFft[b];
            }

            // Beat pulse from low bass bands (bands 0..4)
            float bassEnergy = (smoothedFft[0] + smoothedFft[1] + smoothedFft[2] + smoothedFft[3]) * 0.25f;
            beatPulse = beatPulse * 0.8f + bassEnergy * 0.2f;
        }
    }

    private void ApplyDecay()
    {
        lock (sync)
        {
            for (int i = 0; i < smoothedFft.Length; i++)
                smoothedFft[i] *= 0.88f;
            for (int i = 0; i < waveform.Length; i++)
                waveform[i] *= 0.85f;
            beatPulse *= 0.85f;
        }
    }

    private void ApplyIdlePulse()
    {
        double time = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        lock (sync)
        {
            for (int i = 0; i < smoothedFft.Length; i++)
            {
                double wave = Math.Sin(time * 2.5 + i * 0.18) * 0.5 + 0.5;
                float target = (float)(wave * 0.22);
                smoothedFft[i] = smoothedFft[i] * 0.9f + target * 0.1f;
            }
            for (int i = 0; i < waveform.Length; i++)
            {
                double w = Math.Sin(time * 3.0 + i * 0.12) * 0.15;
                waveform[i] = (float)w;
            }
            beatPulse = (float)(Math.Sin(time * 2.5) * 0.1 + 0.1);
        }
    }

    private void RunSyntheticLoop()
    {
        while (running)
        {
            ApplyIdlePulse();
            Thread.Sleep(25);
        }
    }

    public void Dispose()
    {
        running = false;
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoCreateInstance(ref Guid clsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid iid, out IntPtr ppv);

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern void CoTaskMemFree(IntPtr pv);

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, uint dwStateMask, out IntPtr ppDevices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IntPtr ppEndpoint);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
    }

    [ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, IntPtr audioSessionGuid);
        [PreserveSig] int GetBufferSize(out uint pNumBufferFrames);
        [PreserveSig] int GetStreamLatency(out long phnsLatency);
        [PreserveSig] int GetCurrentPadding(out uint pNumPaddingFrames);
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr pFormat, out IntPtr ppClosestMatch);
        [PreserveSig] int GetMixFormat(out IntPtr ppDeviceFormat);
        [PreserveSig] int GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
        [PreserveSig] int GetService(ref Guid riid, out IntPtr ppv);
    }

    [ComImport, Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
        [PreserveSig] int GetBuffer(out IntPtr ppData, out uint pNumFramesToRead, out uint pdwFlags, out ulong pu64DevicePosition, out ulong pu64QPCPosition);
        [PreserveSig] int ReleaseBuffer(uint numFramesRead);
        [PreserveSig] int GetNextPacketSize(out uint pNumFramesInNextPacket);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }
}
