using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NXPresent;

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BITMAPINFOHEADER
{
    public uint biSize;
    public int biWidth;
    public int biHeight;
    public ushort biPlanes;
    public ushort biBitCount;
    public uint biCompression;
    public uint biSizeImage;
    public int biXPelsPerMeter;
    public int biYPelsPerMeter;
    public uint biClrUsed;
    public uint biClrImportant;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BITMAPINFO
{
    public BITMAPINFOHEADER bmiHeader;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public uint[] bmiColors;
}

internal sealed class WindowCaptureFeed : SourceFeed
{
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private readonly DispatcherTimer timer;
    private IntPtr targetHwnd;
    private WriteableBitmap? frame;
    private byte[]? pixelBuffer;
    private int bufferWidth, bufferHeight;
    private bool capturing;
    private readonly List<WeakReference<Image>> views = new();

    public WindowCaptureFeed(Cue cue) : base(cue)
    {
        targetHwnd = ParseHwnd(cue.Source);
        timer = new DispatcherTimer(DispatcherPriority.Render, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(33) // ~30 fps
        };
        timer.Tick += OnTick;
    }

    internal static IntPtr ParseHwnd(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return IntPtr.Zero;
        string part = source.Split(':')[0];
        if (long.TryParse(part, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long val)) return new IntPtr(val);
        return IntPtr.Zero;
    }

    public static List<Cue> FindWindows()
    {
        var result = new List<Cue>();
        uint currentPid = (uint)Environment.ProcessId;

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindow(hWnd) || !IsWindowVisible(hWnd) || IsIconic(hWnd)) return true;
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == currentPid || pid == 0) return true;

            // Ignore cloaked windows (hidden UWP, other virtual desktops)
            if (DwmGetWindowAttribute(hWnd, 14 /* DWMWA_CLOAKED */, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
                return true;

            int len = GetWindowTextLength(hWnd);
            if (len <= 0) return true;

            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(title)) return true;

            if (title is "Program Manager" or "Settings" or "Microsoft Text Input Application" or "Windows Input Experience")
                return true;

            if (GetWindowRect(hWnd, out var rect))
            {
                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;
                if (w < 80 || h < 80) return true;
            }

            string processName = "";
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch { }

            string displayName = string.IsNullOrEmpty(processName) ? title : $"{title} · {processName}";
            string source = $"{hWnd.ToInt64()}:{processName}:{title}";
            result.Add(new Cue(CueKind.WindowCapture, displayName, source));
            return true;
        }, IntPtr.Zero);

        return result;
    }

    private IntPtr ResolveHwnd()
    {
        if (IsWindow(targetHwnd)) return targetHwnd;

        var parts = Cue.Source.Split(':');
        if (parts.Length >= 3)
        {
            string title = parts[2];
            if (!string.IsNullOrEmpty(title))
            {
                var currentWindows = FindWindows();
                var match = currentWindows.FirstOrDefault(c => c.Name.StartsWith(title, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    targetHwnd = ParseHwnd(match.Source);
                    return targetHwnd;
                }
            }
        }
        return IntPtr.Zero;
    }

    protected override Task LoadAsync(CancellationToken cancellation)
    {
        var hWnd = ResolveHwnd();
        if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
            throw new InvalidOperationException("Das gewählte Anwendungsfenster ist nicht mehr geöffnet.");

        CaptureSingleFrame(hWnd);
        if (frame == null)
            throw new InvalidOperationException("Das Fenster liefert aktuell kein Bild.");

        return Task.CompletedTask;
    }

    public override void Arm()
    {
        var hWnd = ResolveHwnd();
        if (hWnd != IntPtr.Zero) CaptureSingleFrame(hWnd);
    }

    public override void Start()
    {
        timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (capturing || IsDisposed || State != FeedState.Ready) return;
        var hWnd = ResolveHwnd();
        if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
        {
            StopWithError("Das Anwendungsfenster wurde geschlossen.");
            return;
        }

        if (IsIconic(hWnd)) return; // Fenster ist minimiert - letztes Bild beibehalten

        capturing = true;
        try
        {
            CaptureSingleFrame(hWnd);
        }
        finally
        {
            capturing = false;
        }
    }

    private void CaptureSingleFrame(IntPtr hWnd)
    {
        if (!GetWindowRect(hWnd, out var rect)) return;
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width < 2 || height < 2) return;

        // Cap maximum capture dimension for performance
        width = Math.Min(width, 2560);
        height = Math.Min(height, 1440);

        IntPtr hdcWnd = GetDC(hWnd);
        if (hdcWnd == IntPtr.Zero) return;
        try
        {
            IntPtr hdcMem = CreateCompatibleDC(hdcWnd);
            if (hdcMem == IntPtr.Zero) return;
            try
            {
                IntPtr hBmp = CreateCompatibleBitmap(hdcWnd, width, height);
                if (hBmp == IntPtr.Zero) return;
                try
                {
                    IntPtr oldBmp = SelectObject(hdcMem, hBmp);
                    try
                    {
                        // Try PW_RENDERFULLCONTENT (0x02) first, fall back to default PrintWindow or BitBlt
                        bool ok = PrintWindow(hWnd, hdcMem, 2);
                        if (!ok) ok = PrintWindow(hWnd, hdcMem, 0);
                        if (!ok) ok = BitBlt(hdcMem, 0, 0, width, height, hdcWnd, 0, 0, 0x00CC0020 /* SRCCOPY */);

                        if (ok)
                        {
                            int bufferSize = width * height * 4;
                            if (pixelBuffer == null || pixelBuffer.Length != bufferSize)
                            {
                                pixelBuffer = new byte[bufferSize];
                                bufferWidth = width;
                                bufferHeight = height;
                            }

                            BITMAPINFO bi = new()
                            {
                                bmiHeader = new BITMAPINFOHEADER
                                {
                                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                                    biWidth = width,
                                    biHeight = -height, // top-down
                                    biPlanes = 1,
                                    biBitCount = 32,
                                    biCompression = 0 // BI_RGB
                                }
                            };

                            if (GetDIBits(hdcMem, hBmp, 0, (uint)height, pixelBuffer, ref bi, 0) != 0)
                            {
                                if (frame == null || frame.PixelWidth != width || frame.PixelHeight != height)
                                {
                                    frame = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
                                    UpdateViews();
                                }
                                frame.WritePixels(new Int32Rect(0, 0, width, height), pixelBuffer, width * 4, 0);
                            }
                        }
                    }
                    finally
                    {
                        if (oldBmp != IntPtr.Zero) SelectObject(hdcMem, oldBmp);
                    }
                }
                finally
                {
                    DeleteObject(hBmp);
                }
            }
            finally
            {
                DeleteDC(hdcMem);
            }
        }
        finally
        {
            ReleaseDC(hWnd, hdcWnd);
        }
    }

    private void UpdateViews()
    {
        lock (views)
        {
            views.RemoveAll(r => !r.TryGetTarget(out _));
            foreach (var r in views)
            {
                if (r.TryGetTarget(out var img) && img != null && img.Source != frame)
                    img.Source = frame;
            }
        }
    }

    public static BitmapSource? CaptureThumbnail(Cue cue)
    {
        IntPtr hWnd = ParseHwnd(cue.Source);
        if (!IsWindow(hWnd) || !GetWindowRect(hWnd, out var rect)) return null;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width < 2 || height < 2) return null;

        width = Math.Min(width, 1920);
        height = Math.Min(height, 1080);

        IntPtr hdcWnd = GetDC(hWnd);
        if (hdcWnd == IntPtr.Zero) return null;
        try
        {
            IntPtr hdcMem = CreateCompatibleDC(hdcWnd);
            if (hdcMem == IntPtr.Zero) return null;
            try
            {
                IntPtr hBmp = CreateCompatibleBitmap(hdcWnd, width, height);
                if (hBmp == IntPtr.Zero) return null;
                try
                {
                    IntPtr oldBmp = SelectObject(hdcMem, hBmp);
                    try
                    {
                        bool ok = PrintWindow(hWnd, hdcMem, 2);
                        if (!ok) ok = PrintWindow(hWnd, hdcMem, 0);
                        if (!ok) ok = BitBlt(hdcMem, 0, 0, width, height, hdcWnd, 0, 0, 0x00CC0020);

                        if (ok)
                        {
                            byte[] pixels = new byte[width * height * 4];
                            BITMAPINFO bi = new()
                            {
                                bmiHeader = new BITMAPINFOHEADER
                                {
                                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                                    biWidth = width,
                                    biHeight = -height,
                                    biPlanes = 1,
                                    biBitCount = 32,
                                    biCompression = 0
                                }
                            };
                            if (GetDIBits(hdcMem, hBmp, 0, (uint)height, pixels, ref bi, 0) != 0)
                            {
                                var full = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, width * 4);
                                // Scale to thumbnail (240px max width)
                                double scale = Math.Min(1.0, 240.0 / width);
                                var transformed = new TransformedBitmap(full, new ScaleTransform(scale, scale));
                                transformed.Freeze();
                                return transformed;
                            }
                        }
                    }
                    finally
                    {
                        if (oldBmp != IntPtr.Zero) SelectObject(hdcMem, oldBmp);
                    }
                }
                finally
                {
                    DeleteObject(hBmp);
                }
            }
            finally
            {
                DeleteDC(hdcMem);
            }
        }
        finally
        {
            ReleaseDC(hWnd, hdcWnd);
        }

        return null;
    }

    public override FrameworkElement CreateView(Stretch fit)
    {
        var view = new Image { Source = frame, Stretch = fit };
        lock (views)
        {
            views.RemoveAll(r => !r.TryGetTarget(out _));
            views.Add(new WeakReference<Image>(view));
        }
        return view;
    }

    private void StopWithError(string message)
    {
        timer.Stop();
        if (State != FeedState.Ready) return;
        Fail(message);
    }

    protected override void Release()
    {
        timer.Stop();
        timer.Tick -= OnTick;
        lock (views) { views.Clear(); }
        pixelBuffer = null;
        frame = null;
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbi, uint uUsage);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);
}
