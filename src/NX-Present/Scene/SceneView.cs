using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NXPresent;

public sealed class SceneView : FrameworkElement
{
    private readonly DrawingGroup drawingGroup = new();
    private SceneConfig config;
    private bool subscribed;
    private double vinylAngle;
    private long lastFrameTime = Stopwatch.GetTimestamp();

    public SceneConfig Config
    {
        get => config;
        set
        {
            config = value;
            InvalidateVisual();
        }
    }

    public bool Live { get; set; } = true;

    public SceneView(SceneConfig? initialConfig = null)
    {
        config = initialConfig ?? SceneConfig.CreatePreShowTemplate();
        ClipToBounds = true;
        Loaded += (_, _) => UpdateSubscription();
        Unloaded += (_, _) => Unsubscribe();
        IsVisibleChanged += (_, _) => UpdateSubscription();
    }

    private void UpdateSubscription()
    {
        bool run = Live && IsLoaded && IsVisible;
        if (run && !subscribed)
        {
            CompositionTarget.Rendering += OnRendering;
            subscribed = true;
        }
        else if (!run && subscribed)
        {
            Unsubscribe();
        }
    }

    private void Unsubscribe()
    {
        if (subscribed)
        {
            CompositionTarget.Rendering -= OnRendering;
            subscribed = false;
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        double dt = (now - lastFrameTime) / (double)Stopwatch.Frequency;
        lastFrameTime = now;

        // Vinyl rotation (~12 rpm)
        vinylAngle = (vinylAngle + dt * 72.0) % 360.0;

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w = Math.Max(100, ActualWidth);
        double h = Math.Max(100, ActualHeight);
        var bounds = new Rect(0, 0, w, h);

        // Get live track information
        var track = NowPlayingProvider.Instance.CurrentTrack;
        string title = config.Song.AutoWindowsMedia ? track.Title : config.Song.CustomTitle;
        string artist = config.Song.AutoWindowsMedia ? track.Artist : config.Song.CustomArtist;
        string album = config.Song.AutoWindowsMedia ? track.Album : config.Song.CustomAlbum;
        BitmapSource? cover = config.Song.AutoWindowsMedia ? track.Cover : LoadCustomCover(config.Song.CustomCoverPath);

        // 1. Render Background
        RenderBackground(dc, bounds, cover);

        // 2. Render Visualizer
        if (config.Visualizer.Enabled)
        {
            RenderVisualizer(dc, bounds, cover);
        }

        // 3. Render Song Info Widget
        if (config.Song.Enabled)
        {
            RenderSongWidget(dc, bounds, title, artist, album, cover);
        }

        // 4. Render Clock Widget
        if (config.Clock.Enabled)
        {
            RenderClockWidget(dc, bounds);
        }
    }

    private void RenderBackground(DrawingContext dc, Rect bounds, BitmapSource? cover)
    {
        switch (config.BackgroundStyle)
        {
            case SceneBackgroundStyle.SolidDark:
                Color col = ParseColor(config.BackgroundColor1, Color.FromRgb(13, 17, 26));
                dc.DrawRectangle(new SolidColorBrush(col), null, bounds);
                break;

            case SceneBackgroundStyle.Gradient:
                Color c1 = ParseColor(config.BackgroundColor1, Color.FromRgb(15, 20, 32));
                Color c2 = ParseColor(config.BackgroundColor2, Color.FromRgb(24, 34, 56));
                var gradient = new LinearGradientBrush(c1, c2, new Point(0, 0), new Point(1, 1));
                dc.DrawRectangle(gradient, null, bounds);
                break;

            case SceneBackgroundStyle.BlurredCover:
                Color bg = ParseColor(config.BackgroundColor1, Color.FromRgb(16, 16, 24));
                dc.DrawRectangle(new SolidColorBrush(bg), null, bounds);
                if (cover != null)
                {
                    // Draw large dimmed cover background
                    dc.PushOpacity(0.22);
                    dc.DrawImage(cover, bounds);
                    dc.Pop();
                }
                break;

            case SceneBackgroundStyle.Transparent:
                // No background for broadcast lower-third / overlays
                break;
        }
    }

    private void RenderVisualizer(DrawingContext dc, Rect bounds, BitmapSource? cover)
    {
        var viz = config.Visualizer;
        var engine = AudioVisualizerEngine.Instance;
        var (cPrimary, cSecondary) = GetThemeColors(viz.Theme);

        switch (viz.Style)
        {
            case VisualizerStyle.Bars:
                RenderBars(dc, bounds, viz, engine, cPrimary, cSecondary);
                break;

            case VisualizerStyle.Waveform:
                RenderWaveform(dc, bounds, viz, engine, cPrimary, cSecondary);
                break;

            case VisualizerStyle.Circle:
                RenderCircle(dc, bounds, viz, engine, cPrimary, cSecondary);
                break;

            case VisualizerStyle.Particles:
                RenderParticles(dc, bounds, viz, engine, cPrimary, cSecondary);
                break;
        }
    }

    private void RenderBars(DrawingContext dc, Rect bounds, VisualizerWidgetConfig viz, AudioVisualizerEngine engine, Color c1, Color c2)
    {
        int count = viz.BarCount;
        float[] spectrum = engine.GetSpectrum(count, viz.Sensitivity);
        double totalWidth = bounds.Width;
        double maxHeight = Math.Min(viz.Height, bounds.Height * 0.45);
        double barWidth = Math.Max(2, (totalWidth - (count * 3)) / count);

        double baseY = viz.Position switch
        {
            VisualizerPosition.Center => bounds.Height / 2 + maxHeight / 2,
            _ => bounds.Height - 12
        };

        for (int i = 0; i < count; i++)
        {
            double x = i * (barWidth + 3) + 6;
            double barH = Math.Max(3, spectrum[i] * maxHeight);
            double y = baseY - barH;

            double ratio = (double)i / count;
            Color barCol = Interpolate(c1, c2, ratio);
            var brush = new LinearGradientBrush(barCol, Interpolate(barCol, Colors.White, 0.4), new Point(0, 1), new Point(0, 0));

            var rect = new Rect(x, y, barWidth, barH);
            dc.DrawRoundedRectangle(brush, null, rect, 2, 2);

            // Peak cap dot
            dc.DrawRectangle(Brushes.White, null, new Rect(x, y - 4, barWidth, 2));
        }
    }

    private void RenderWaveform(DrawingContext dc, Rect bounds, VisualizerWidgetConfig viz, AudioVisualizerEngine engine, Color c1, Color c2)
    {
        float[] wave = engine.GetWaveform(64);
        double centerY = viz.Position == VisualizerPosition.Center ? bounds.Height / 2 : bounds.Height - viz.Height / 2 - 20;
        double amplitude = Math.Min(viz.Height * 0.5, bounds.Height * 0.25) * viz.Sensitivity;
        double stepX = bounds.Width / Math.Max(1, wave.Length - 1);

        var pen = new Pen(new SolidColorBrush(c1), 3);
        var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(80, c1.R, c1.G, c1.B)), 8);

        StreamGeometry geom = new();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(new Point(0, centerY + wave[0] * amplitude), false, false);
            for (int i = 1; i < wave.Length; i++)
            {
                double x = i * stepX;
                double y = centerY + wave[i] * amplitude;
                ctx.LineTo(new Point(x, y), true, true);
            }
        }
        geom.Freeze();

        dc.DrawGeometry(null, glowPen, geom);
        dc.DrawGeometry(null, pen, geom);
    }

    private void RenderCircle(DrawingContext dc, Rect bounds, VisualizerWidgetConfig viz, AudioVisualizerEngine engine, Color c1, Color c2)
    {
        Point center = new(bounds.Width / 2, bounds.Height / 2 - 20);
        float pulse = engine.GetBeatPulse();
        double radius = (120 + pulse * 25) * config.Song.Scale;
        int count = viz.BarCount;
        float[] spectrum = engine.GetSpectrum(count, viz.Sensitivity);

        for (int i = 0; i < count; i++)
        {
            double angle = (i / (double)count) * Math.PI * 2.0;
            double barLen = 10 + spectrum[i] * 90;

            double x1 = center.X + Math.Cos(angle) * radius;
            double y1 = center.Y + Math.Sin(angle) * radius;
            double x2 = center.X + Math.Cos(angle) * (radius + barLen);
            double y2 = center.Y + Math.Sin(angle) * (radius + barLen);

            Color col = Interpolate(c1, c2, (double)i / count);
            var pen = new Pen(new SolidColorBrush(col), 3);
            dc.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
        }
    }

    private void RenderParticles(DrawingContext dc, Rect bounds, VisualizerWidgetConfig viz, AudioVisualizerEngine engine, Color c1, Color c2)
    {
        float pulse = engine.GetBeatPulse();
        Point center = new(bounds.Width / 2, bounds.Height / 2);

        // Expanding ambient glow pulses
        double r1 = (bounds.Width * 0.25) * (0.8 + pulse * 0.4);
        var glowBrush1 = new RadialGradientBrush(Color.FromArgb((byte)(70 + pulse * 60), c1.R, c1.G, c1.B), Colors.Transparent);
        dc.DrawEllipse(glowBrush1, null, center, r1, r1 * 0.6);

        double r2 = (bounds.Width * 0.35) * (0.9 + pulse * 0.3);
        var glowBrush2 = new RadialGradientBrush(Color.FromArgb((byte)(40 + pulse * 40), c2.R, c2.G, c2.B), Colors.Transparent);
        dc.DrawEllipse(glowBrush2, null, center, r2, r2 * 0.6);
    }

    private void RenderSongWidget(DrawingContext dc, Rect bounds, string title, string artist, string album, BitmapSource? cover)
    {
        var song = config.Song;
        double scale = song.Scale;
        double coverSize = 180 * scale;

        Point pos = song.Position switch
        {
            SongPosition.LowerThirdLeft => new Point(50, bounds.Height - 140),
            SongPosition.LowerThirdRight => new Point(bounds.Width - 480, bounds.Height - 140),
            SongPosition.TopLeft => new Point(50, 50),
            SongPosition.TopRight => new Point(bounds.Width - 480, 50),
            _ => new Point(bounds.Width / 2 - (song.ShowCover ? 260 : 160) * scale, bounds.Height / 2 - 80 * scale)
        };

        if (song.Position is SongPosition.LowerThirdLeft or SongPosition.LowerThirdRight)
        {
            coverSize = 80 * scale;
            // Draw broadcast pill backdrop
            var pillRect = new Rect(pos.X - 10, pos.Y - 10, 480 * scale, 100 * scale);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(200, 15, 20, 30)), new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1), pillRect, 10, 10);
        }

        double textX = pos.X;

        // Render Cover
        if (song.ShowCover)
        {
            var coverRect = new Rect(pos.X, pos.Y, coverSize, coverSize);
            if (song.CoverStyle == CoverStyle.Vinyl)
            {
                // Render Vinyl Record Disk
                Point vinylCenter = new(coverRect.Left + coverSize / 2, coverRect.Top + coverSize / 2);
                dc.PushTransform(new RotateTransform(vinylAngle, vinylCenter.X, vinylCenter.Y));
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(15, 15, 18)), new Pen(Brushes.DimGray, 1), vinylCenter, coverSize / 2, coverSize / 2);
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), vinylCenter, coverSize * 0.38, coverSize * 0.38);
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), vinylCenter, coverSize * 0.28, coverSize * 0.28);
                if (cover != null)
                {
                    var innerCoverRect = new Rect(vinylCenter.X - coverSize * 0.2, vinylCenter.Y - coverSize * 0.2, coverSize * 0.4, coverSize * 0.4);
                    dc.DrawImage(cover, innerCoverRect);
                }
                dc.DrawEllipse(Brushes.Black, null, vinylCenter, coverSize * 0.06, coverSize * 0.06);
                dc.Pop();
            }
            else
            {
                if (cover != null)
                {
                    double radius = song.CoverStyle == CoverStyle.Rounded ? 12 : 0;
                    dc.DrawRoundedRectangle(new ImageBrush(cover), null, coverRect, radius, radius);
                    dc.DrawRoundedRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 1), coverRect, radius, radius);
                }
                else
                {
                    // Fallback Music Icon Badge
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(30, 36, 52)), null, coverRect, 12, 12);
                    var iconText = new FormattedText("", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe MDL2 Assets"), 40 * scale, Brushes.Gray, 1.25);
                    dc.DrawText(iconText, new Point(coverRect.Left + (coverSize - iconText.Width) / 2, coverRect.Top + (coverSize - iconText.Height) / 2));
                }
            }

            textX += coverSize + 24 * scale;
        }

        // Render Title & Artist Text
        var textBrush = new SolidColorBrush(ParseColor(song.TextColor, Colors.White));
        var subTextBrush = new SolidColorBrush(Color.FromArgb(200, 200, 210, 225));

        var titleFormat = new FormattedText(title, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            Math.Max(14, 28 * scale), textBrush, 1.25)
        {
            MaxTextWidth = Math.Max(200, bounds.Width - textX - 40),
            MaxLineCount = 2,
            Trimming = TextTrimming.CharacterEllipsis
        };

        var artistFormat = new FormattedText(artist + (string.IsNullOrEmpty(album) ? "" : $" · {album}"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
            Math.Max(11, 18 * scale), subTextBrush, 1.25)
        {
            MaxTextWidth = Math.Max(200, bounds.Width - textX - 40),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis
        };

        double textY = pos.Y + Math.Max(0, (coverSize - (titleFormat.Height + artistFormat.Height + 8)) / 2);
        dc.DrawText(titleFormat, new Point(textX, textY));
        dc.DrawText(artistFormat, new Point(textX, textY + titleFormat.Height + 6));
    }

    private void RenderClockWidget(DrawingContext dc, Rect bounds)
    {
        var clock = config.Clock;
        var now = DateTime.Now;
        string timeStr = clock.ShowSeconds ? now.ToString("HH:mm:ss") : now.ToString("HH:mm");
        string dateStr = clock.ShowDate ? now.ToString("dddd, dd. MMMM yyyy", CultureInfo.GetCultureInfo("de-DE")) : "";

        double scale = clock.Scale;
        var timeBrush = new SolidColorBrush(ParseColor(clock.Color, Colors.White));
        var dateBrush = new SolidColorBrush(Color.FromArgb(190, 200, 210, 230));

        var timeFormat = new FormattedText(timeStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            Math.Max(16, 42 * scale), timeBrush, 1.25);

        Point clockPos = clock.Position switch
        {
            ClockPosition.TopLeft => new Point(50, 40),
            ClockPosition.Center => new Point(bounds.Width / 2 - timeFormat.Width / 2, bounds.Height * 0.2),
            ClockPosition.BelowSong => new Point(bounds.Width / 2 - timeFormat.Width / 2, bounds.Height * 0.65),
            ClockPosition.BottomRight => new Point(bounds.Width - timeFormat.Width - 60, bounds.Height - 80),
            _ => new Point(bounds.Width - timeFormat.Width - 60, 40)
        };

        // Draw Clock Pill Background
        var pill = new Rect(clockPos.X - 16, clockPos.Y - 8, timeFormat.Width + 32, (clock.ShowDate ? timeFormat.Height + 28 : timeFormat.Height) + 16);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(140, 10, 14, 24)), new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1), pill, 10, 10);

        dc.DrawText(timeFormat, clockPos);

        if (clock.ShowDate && !string.IsNullOrEmpty(dateStr))
        {
            var dateFormat = new FormattedText(dateStr, CultureInfo.GetCultureInfo("de-DE"), FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                Math.Max(10, 14 * scale), dateBrush, 1.25);
            dc.DrawText(dateFormat, new Point(clockPos.X + (timeFormat.Width - dateFormat.Width) / 2, clockPos.Y + timeFormat.Height + 4));
        }
    }

    private static (Color, Color) GetThemeColors(VisualizerTheme theme) => theme switch
    {
        VisualizerTheme.Neon => (Color.FromRgb(0, 240, 255), Color.FromRgb(255, 0, 128)),
        VisualizerTheme.ElectricBlue => (Color.FromRgb(0, 150, 255), Color.FromRgb(100, 230, 255)),
        VisualizerTheme.AmberFire => (Color.FromRgb(255, 140, 0), Color.FromRgb(255, 40, 0)),
        VisualizerTheme.Rainbow => (Color.FromRgb(255, 50, 50), Color.FromRgb(50, 100, 255)),
        VisualizerTheme.CleanWhite => (Color.FromRgb(230, 230, 240), Color.FromRgb(255, 255, 255)),
        VisualizerTheme.Emerald => (Color.FromRgb(0, 230, 150), Color.FromRgb(0, 255, 220)),
        _ => (Colors.Cyan, Colors.Magenta)
    };

    private static Color Interpolate(Color c1, Color c2, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        byte r = (byte)(c1.R + (c2.R - c1.R) * t);
        byte g = (byte)(c1.G + (c2.G - c1.G) * t);
        byte b = (byte)(c1.B + (c2.B - c1.B) * t);
        return Color.FromRgb(r, g, b);
    }

    private static Color ParseColor(string hex, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return fallback;
        }
    }

    private static BitmapSource? LoadCustomCover(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return null;
        try { return ImageLoader.Load(path, 400); }
        catch { return null; }
    }
}
