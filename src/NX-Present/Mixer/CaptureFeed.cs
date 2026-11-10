using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage.Streams;
using FrameBuffer = Windows.Storage.Streams.Buffer;

namespace NXPresent;

internal sealed class CaptureFeed(Cue cue) : SourceFeed(cue)
{
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private readonly CancellationTokenSource disposal = new();
    private readonly TaskCompletionSource firstPicture = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private MediaCapture? capture;
    private MediaFrameReader? reader;
    private WriteableBitmap? frame;
    private FrameBuffer? buffer;
    private byte[]? pixels;
    private int copying;

    internal static async Task<Cue[]> FindSourcesAsync()
    {
        var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
        return devices.Select(device => new Cue(CueKind.Capture, device.Name, device.Id)).ToArray();
    }

    protected override async Task LoadAsync(CancellationToken cancellation)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, disposal.Token);
        linked.CancelAfter(TimeSpan.FromSeconds(15));
        try { await OpenCaptureAsync(linked.Token); }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested && !disposal.IsCancellationRequested)
        {
            Release();
            throw new InvalidOperationException("Das Aufnahmegerät hat nicht rechtzeitig geantwortet.");
        }
        catch { Release(); throw; }
    }

    private async Task OpenCaptureAsync(CancellationToken token)
    {
        capture = new MediaCapture();
        capture.Failed += CaptureFailed;
        await capture.InitializeAsync(new MediaCaptureInitializationSettings
        {
            VideoDeviceId = Cue.Source,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            SharingMode = MediaCaptureSharingMode.SharedReadOnly,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu
        }).AsTask(token);
        token.ThrowIfCancellationRequested();

        var source = capture.FrameSources.Values
            .Where(item => item.Info.SourceKind == MediaFrameSourceKind.Color)
            .OrderBy(item => item.Info.MediaStreamType == MediaStreamType.VideoPreview ? 0 : 1)
            .FirstOrDefault() ?? throw new InvalidOperationException("Das Aufnahmegerät stellt keinen Videostream bereit.");
        reader = await capture.CreateFrameReaderAsync(source).AsTask(token);
        token.ThrowIfCancellationRequested();
        reader.FrameArrived += FrameArrived;
        var status = await reader.StartAsync().AsTask(token);
        if (status != MediaFrameReaderStartStatus.Success)
            throw new InvalidOperationException("Aufnahmegerät konnte nicht gestartet werden: " + status);
        try { await firstPicture.Task.WaitAsync(TimeSpan.FromSeconds(10), token); }
        catch (TimeoutException) { throw new InvalidOperationException("Das Aufnahmegerät liefert kein Bild. Prüfe das Eingangssignal."); }
    }

    private void CaptureFailed(MediaCapture sender, MediaCaptureFailedEventArgs args)
    {
        string message = "Aufnahmegerät gestört: " + args.Message;
        firstPicture.TrySetException(new InvalidOperationException(message));
        dispatcher.InvokeAsync(() => StopWithError(message));
    }

    private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (disposal.IsCancellationRequested || Interlocked.Exchange(ref copying, 1) != 0) return;
        bool queued = false;
        try
        {
            using var sample = sender.TryAcquireLatestFrame();
            using var bitmap = sample?.VideoMediaFrame?.SoftwareBitmap;
            if (bitmap == null) return;
            using var converted = bitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8
                ? null : SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            var picture = converted ?? bitmap;
            int width = picture.PixelWidth, height = picture.PixelHeight;
            int size = checked(width * height * 4);
            if (pixels?.Length != size)
            {
                pixels = new byte[size];
                buffer = new FrameBuffer((uint)size) { Length = (uint)size };
            }
            picture.CopyToBuffer(buffer!);
            using var data = DataReader.FromBuffer(buffer!);
            data.ReadBytes(pixels);
            dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (IsDisposed || State == FeedState.Failed) return;
                    // Keep the original bitmap bound to every monitor. Capture format is fixed for this session.
                    frame ??= new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
                    if (frame.PixelWidth != width || frame.PixelHeight != height)
                    {
                        StopWithError("Das Aufnahmeformat hat sich geändert. Wähle die Quelle erneut.");
                        return;
                    }
                    frame.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                    firstPicture.TrySetResult();
                }
                finally { Volatile.Write(ref copying, 0); }
            }, DispatcherPriority.Render);
            queued = true;
        }
        catch (Exception ex)
        {
            if (!disposal.IsCancellationRequested)
            {
                firstPicture.TrySetException(ex);
                dispatcher.InvokeAsync(() => StopWithError("Aufnahmegerät gestört: " + ex.Message));
            }
        }
        finally { if (!queued) Volatile.Write(ref copying, 0); }
    }

    public override FrameworkElement CreateView(Stretch fit) => new Image { Source = frame, Stretch = fit };

    private void StopWithError(string message)
    {
        if (State != FeedState.Ready) return;
        Release();
        Fail(message);
    }

    protected override void Release()
    {
        disposal.Cancel();
        if (reader != null) reader.FrameArrived -= FrameArrived;
        if (capture != null) capture.Failed -= CaptureFailed;
        _ = CloseAsync(reader, capture);
        reader = null;
        capture = null;
    }

    private static async Task CloseAsync(MediaFrameReader? reader, MediaCapture? capture)
    {
        try { if (reader != null) await reader.StopAsync(); }
        catch (Exception ex) { App.Log(ex); }
        finally
        {
            reader?.Dispose();
            capture?.Dispose();
        }
    }
}
