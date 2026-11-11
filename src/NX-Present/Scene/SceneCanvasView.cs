using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NXPresent;

public sealed class SceneCanvasView : FrameworkElement, IDisposable
{
    private SceneModel model;
    private bool subscribed;
    private double vinylAngle;
    private long lastFrameTime = Stopwatch.GetTimestamp();

    // Editor-specific interactive state
    public bool IsEditorMode { get; set; } = false;
    public SceneItem? SelectedItem { get; set; }
    public event Action<SceneItem?>? SelectionChanged;
    public event Action? ItemModified;

    private enum DragMode { None, Move, ResizeTL, ResizeT, ResizeTR, ResizeR, ResizeBR, ResizeB, ResizeBL, ResizeL }
    private DragMode currentDrag = DragMode.None;
    private Point dragStartMouse;
    private Rect dragStartItemBounds;

    public SceneModel Model
    {
        get => model;
        set
        {
            model = value;
            InvalidateVisual();
        }
    }

    private bool live = true;
    public bool Live
    {
        get => live;
        set
        {
            if (live == value) return;
            live = value;
            UpdateSubscription();
        }
    }

    public void Dispose()
    {
        Unsubscribe();
    }

    public SceneCanvasView(SceneModel? initialModel = null)
    {
        model = initialModel ?? SceneModel.CreatePreShowTemplate();
        ClipToBounds = true;
        Focusable = true;

        Loaded += (_, _) => UpdateSubscription();
        Unloaded += (_, _) => Unsubscribe();
        IsVisibleChanged += (_, _) => UpdateSubscription();

        MouseDown += OnCanvasMouseDown;
        MouseMove += OnCanvasMouseMove;
        MouseUp += OnCanvasMouseUp;
        KeyDown += OnCanvasKeyDown;
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

        vinylAngle = (vinylAngle + dt * 72.0) % 360.0;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double canvasW = Math.Max(100, ActualWidth);
        double canvasH = Math.Max(100, ActualHeight);

        // Virtual 16:9 canvas dimensions: 1920 x 1080
        double scaleX = canvasW / 1920.0;
        double scaleY = canvasH / 1080.0;

        dc.PushTransform(new ScaleTransform(scaleX, scaleY));

        // Background
        if (!model.TransparentBackground)
        {
            Color bgColor = ParseColor(model.BackgroundColor, Color.FromRgb(13, 17, 26));
            dc.DrawRectangle(new SolidColorBrush(bgColor), null, new Rect(0, 0, 1920, 1080));
        }

        // Render each item in z-order
        foreach (var item in model.Items)
        {
            if (!item.Visible) continue;
            dc.PushOpacity(item.Opacity);
            RenderSceneItem(dc, item);
            dc.Pop();
        }

        // Editor selection overlay
        if (IsEditorMode && SelectedItem != null)
        {
            RenderSelectionGizmo(dc, SelectedItem);
        }

        dc.Pop(); // Pop scale transform
    }

    private void RenderSceneItem(DrawingContext dc, SceneItem item)
    {
        var rect = new Rect(item.X, item.Y, item.Width, item.Height);

        switch (item.Type)
        {
            case SceneItemType.ColorBox:
                Color boxCol = ParseColor(item.ColorHex, Color.FromRgb(20, 26, 40));
                dc.DrawRoundedRectangle(new SolidColorBrush(boxCol), new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), rect, item.CornerRadius, item.CornerRadius);
                break;

            case SceneItemType.Text:
                var textBrush = new SolidColorBrush(ParseColor(item.TextColor, Colors.White));
                var ft = new FormattedText(item.TextContent, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, item.TextBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                    Math.Max(10, item.TextFontSize), textBrush, 1.25)
                {
                    MaxTextWidth = rect.Width,
                    MaxTextHeight = rect.Height,
                    Trimming = TextTrimming.CharacterEllipsis
                };
                dc.DrawText(ft, rect.TopLeft);
                break;

            case SceneItemType.Clock:
                var now = DateTime.Now;
                string timeStr = item.ShowSeconds ? now.ToString("HH:mm:ss") : now.ToString("HH:mm");
                string dateStr = item.ShowDate ? now.ToString("dddd, dd. MMMM yyyy", CultureInfo.GetCultureInfo("de-DE")) : "";
                var clockBrush = new SolidColorBrush(ParseColor(item.ClockColor, Colors.White));

                var timeFt = new FormattedText(timeStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    Math.Max(12, item.ClockFontSize), clockBrush, 1.25);
                dc.DrawText(timeFt, rect.TopLeft);

                if (item.ShowDate && !string.IsNullOrEmpty(dateStr))
                {
                    var dateBrush = new SolidColorBrush(Color.FromArgb(190, 200, 210, 230));
                    var dateFt = new FormattedText(dateStr, CultureInfo.GetCultureInfo("de-DE"), FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                        Math.Max(10, item.ClockFontSize * 0.38), dateBrush, 1.25);
                    dc.DrawText(dateFt, new Point(rect.Left, rect.Top + timeFt.Height + 4));
                }
                break;

            case SceneItemType.Countdown:
                RenderCountdownItem(dc, item, rect);
                break;

            case SceneItemType.SongInfo:
                RenderSongItem(dc, item, rect);
                break;

            case SceneItemType.Visualizer:
                RenderVisualizerItem(dc, item, rect);
                break;

            case SceneItemType.WindowCapture:
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(20, 28, 44)), new Pen(Brushes.DodgerBlue, 1), rect, 8, 8);
                var winFt = new FormattedText($"🪟 {item.Name}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 16, Brushes.White, 1.25);
                dc.DrawText(winFt, new Point(rect.Left + 12, rect.Top + 12));
                break;

            case SceneItemType.Media:
                if (!string.IsNullOrWhiteSpace(item.MediaPath) && System.IO.File.Exists(item.MediaPath))
                {
                    try
                    {
                        var bmp = ImageLoader.Load(item.MediaPath, (int)rect.Width);
                        dc.DrawImage(bmp, rect);
                    }
                    catch { }
                }
                else
                {
                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(25, 30, 42)), new Pen(Brushes.Gray, 1), rect);
                }
                break;
        }
    }

    public static event Action? CountdownZeroReached;
    private static long lastGlobalCountdownTriggerTick;
    private readonly HashSet<string> triggeredCountdownItemIds = new();
    private readonly Dictionary<string, DateTime> durationStartTimes = new();

    private void RenderCountdownItem(DrawingContext dc, SceneItem item, Rect rect)
    {
        TimeSpan remaining = TimeSpan.Zero;
        var now = DateTime.Now;

        if (item.CountdownMode == CountdownMode.TargetTime)
        {
            if (!string.IsNullOrWhiteSpace(item.TargetTimeText) && TimeSpan.TryParse(item.TargetTimeText.Trim(), CultureInfo.InvariantCulture, out var targetTimeOfDay))
            {
                var target = now.Date + targetTimeOfDay;
                if (target < now && (now - target) > TimeSpan.FromHours(2)) target = target.AddDays(1);
                remaining = target - now;
            }
            else
            {
                remaining = TimeSpan.FromSeconds(1);
            }
        }
        else
        {
            if (!durationStartTimes.TryGetValue(item.Id, out var start))
            {
                start = now;
                durationStartTimes[item.Id] = start;
            }
            double elapsedSec = (now - start).TotalSeconds;
            double remSec = item.CountdownDurationSeconds - elapsedSec;
            remaining = TimeSpan.FromSeconds(Math.Max(0, remSec));
        }

        double currentY = rect.Top;

        if (item.ShowCountdownPrefix && !string.IsNullOrWhiteSpace(item.CountdownPrefix))
        {
            var prefixBrush = new SolidColorBrush(Color.FromArgb(200, 220, 230, 245));
            prefixBrush.Freeze();
            var prefixFt = new FormattedText(item.CountdownPrefix.ToUpperInvariant(), CultureInfo.GetCultureInfo("de-DE"), FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                Math.Max(12, item.CountdownFontSize * 0.35), prefixBrush, 1.25)
            {
                MaxTextWidth = rect.Width,
                Trimming = TextTrimming.CharacterEllipsis
            };
            dc.DrawText(prefixFt, new Point(rect.Left + (rect.Width - prefixFt.Width) / 2, currentY));
            currentY += prefixFt.Height + 8;
        }

        if (remaining.TotalSeconds <= 0)
        {
            string expText = string.IsNullOrWhiteSpace(item.CountdownExpiredText) ? "Die Vorstellung beginnt gleich …" : item.CountdownExpiredText;
            var expBrush = new SolidColorBrush(Color.FromRgb(52, 211, 153));
            expBrush.Freeze();
            var expFt = new FormattedText(expText, CultureInfo.GetCultureInfo("de-DE"), FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                Math.Max(16, item.CountdownFontSize * 0.55), expBrush, 1.25)
            {
                MaxTextWidth = rect.Width,
                Trimming = TextTrimming.CharacterEllipsis
            };
            dc.DrawText(expFt, new Point(rect.Left + (rect.Width - expFt.Width) / 2, currentY + 6));

            if (item.TriggerCueOnZero && !IsEditorMode && Live && !triggeredCountdownItemIds.Contains(item.Id))
            {
                long currentTick = Stopwatch.GetTimestamp();
                if (Stopwatch.GetElapsedTime(lastGlobalCountdownTriggerTick).TotalSeconds >= 3.0)
                {
                    lastGlobalCountdownTriggerTick = currentTick;
                    triggeredCountdownItemIds.Add(item.Id);
                    CountdownZeroReached?.Invoke();
                }
            }
        }
        else
        {
            triggeredCountdownItemIds.Remove(item.Id);
            string timeStr = remaining.TotalHours >= 1
                ? $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
                : $"{remaining.Minutes:00}:{remaining.Seconds:00}";

            var color = ParseColor(item.CountdownColor, Color.FromRgb(56, 189, 248));
            var digitsBrush = new SolidColorBrush(color);
            digitsBrush.Freeze();

            var digitsFt = new FormattedText(timeStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                Math.Max(16, item.CountdownFontSize), digitsBrush, 1.25);

            dc.DrawText(digitsFt, new Point(rect.Left + (rect.Width - digitsFt.Width) / 2, currentY));
        }
    }

    private void RenderSongItem(DrawingContext dc, SceneItem item, Rect rect)
    {
        var track = NowPlayingProvider.Instance.CurrentTrack;
        string title = item.AutoMedia ? track.Title : item.CustomTitle;
        string artist = item.AutoMedia ? track.Artist : item.CustomArtist;
        string album = item.AutoMedia ? track.Album : item.CustomAlbum;
        BitmapSource? cover = item.AutoMedia ? track.Cover : LoadCover(item.CustomCoverPath);

        double coverSize = Math.Min(rect.Width * 0.4, rect.Height);
        double textX = rect.Left;

        if (item.ShowCover)
        {
            var coverRect = new Rect(rect.Left, rect.Top + (rect.Height - coverSize) / 2, coverSize, coverSize);
            if (item.CoverStyle == CoverStyle.Vinyl)
            {
                Point center = new(coverRect.Left + coverSize / 2, coverRect.Top + coverSize / 2);
                dc.PushTransform(new RotateTransform(vinylAngle, center.X, center.Y));
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(15, 15, 18)), new Pen(Brushes.DimGray, 1), center, coverSize / 2, coverSize / 2);
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), center, coverSize * 0.38, coverSize * 0.38);
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), center, coverSize * 0.28, coverSize * 0.28);
                if (cover != null)
                {
                    var inner = new Rect(center.X - coverSize * 0.2, center.Y - coverSize * 0.2, coverSize * 0.4, coverSize * 0.4);
                    dc.DrawImage(cover, inner);
                }
                dc.DrawEllipse(Brushes.Black, null, center, coverSize * 0.06, coverSize * 0.06);
                dc.Pop();
            }
            else
            {
                double rad = item.CoverStyle == CoverStyle.Rounded ? 10 : 0;
                if (cover != null)
                {
                    dc.DrawRoundedRectangle(new ImageBrush(cover), null, coverRect, rad, rad);
                }
                else
                {
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(30, 36, 52)), null, coverRect, rad, rad);
                    var iconFt = new FormattedText("", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe MDL2 Assets"), coverSize * 0.4, Brushes.Gray, 1.25);
                    dc.DrawText(iconFt, new Point(coverRect.Left + (coverSize - iconFt.Width) / 2, coverRect.Top + (coverSize - iconFt.Height) / 2));
                }
                dc.DrawRoundedRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1), coverRect, rad, rad);
            }
            textX += coverSize + 20;
        }

        var textBrush = new SolidColorBrush(ParseColor(item.SongTextColor, Colors.White));
        var subBrush = new SolidColorBrush(Color.FromArgb(200, 200, 210, 225));

        var titleFt = new FormattedText(title, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            Math.Max(12, item.SongFontSize), textBrush, 1.25)
        {
            MaxTextWidth = Math.Max(100, rect.Right - textX),
            MaxLineCount = 2,
            Trimming = TextTrimming.CharacterEllipsis
        };

        var artistFt = new FormattedText(artist + (string.IsNullOrEmpty(album) ? "" : $" · {album}"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
            Math.Max(10, item.SongFontSize * 0.62), subBrush, 1.25)
        {
            MaxTextWidth = Math.Max(100, rect.Right - textX),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis
        };

        double textY = rect.Top + Math.Max(0, (rect.Height - (titleFt.Height + artistFt.Height + 6)) / 2);
        dc.DrawText(titleFt, new Point(textX, textY));
        dc.DrawText(artistFt, new Point(textX, textY + titleFt.Height + 6));
    }

    private void RenderVisualizerItem(DrawingContext dc, SceneItem item, Rect rect)
    {
        var engine = AudioVisualizerEngine.Instance;
        var (c1, c2) = GetThemeColors(item.VizTheme);

        switch (item.VizStyle)
        {
            case VisualizerStyle.Bars:
                int count = Math.Clamp(item.BarCount, 8, 64);
                float[] spectrum = engine.GetSpectrum(count, item.Sensitivity);
                double barW = Math.Max(2, (rect.Width - (count * 2)) / count);

                for (int i = 0; i < count; i++)
                {
                    double x = rect.Left + i * (barW + 2);
                    double barH = Math.Max(2, spectrum[i] * rect.Height);
                    double y = rect.Bottom - barH;

                    Color barCol = Interpolate(c1, c2, (double)i / count);
                    var brush = new LinearGradientBrush(barCol, Interpolate(barCol, Colors.White, 0.4), new Point(0, 1), new Point(0, 0));
                    dc.DrawRoundedRectangle(brush, null, new Rect(x, y, barW, barH), 2, 2);
                    dc.DrawRectangle(Brushes.White, null, new Rect(x, y - 3, barW, 2));
                }
                break;

            case VisualizerStyle.Waveform:
                float[] wave = engine.GetWaveform(64);
                double centerY = rect.Top + rect.Height / 2;
                double amp = rect.Height * 0.45 * item.Sensitivity;
                double stepX = rect.Width / Math.Max(1, wave.Length - 1);

                StreamGeometry geom = new();
                using (var ctx = geom.Open())
                {
                    ctx.BeginFigure(new Point(rect.Left, centerY + wave[0] * amp), false, false);
                    for (int i = 1; i < wave.Length; i++)
                        ctx.LineTo(new Point(rect.Left + i * stepX, centerY + wave[i] * amp), true, true);
                }
                geom.Freeze();
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(70, c1.R, c1.G, c1.B)), 6), geom);
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(c1), 2.5), geom);
                break;

            case VisualizerStyle.Circle:
                Point center = new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                float pulse = engine.GetBeatPulse();
                double radius = (Math.Min(rect.Width, rect.Height) * 0.3) * (0.85 + pulse * 0.2);
                int circCount = Math.Clamp(item.BarCount, 16, 64);
                float[] circSpec = engine.GetSpectrum(circCount, item.Sensitivity);

                for (int i = 0; i < circCount; i++)
                {
                    double angle = (i / (double)circCount) * Math.PI * 2.0;
                    double len = 6 + circSpec[i] * (rect.Height * 0.25);
                    double x1 = center.X + Math.Cos(angle) * radius;
                    double y1 = center.Y + Math.Sin(angle) * radius;
                    double x2 = center.X + Math.Cos(angle) * (radius + len);
                    double y2 = center.Y + Math.Sin(angle) * (radius + len);

                    Color col = Interpolate(c1, c2, (double)i / circCount);
                    dc.DrawLine(new Pen(new SolidColorBrush(col), 3), new Point(x1, y1), new Point(x2, y2));
                }
                break;

            case VisualizerStyle.Particles:
                Point pCenter = new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                float pPulse = engine.GetBeatPulse();
                double pR = (Math.Min(rect.Width, rect.Height) * 0.4) * (0.8 + pPulse * 0.3);
                var glow = new RadialGradientBrush(Color.FromArgb((byte)(80 + pPulse * 70), c1.R, c1.G, c1.B), Colors.Transparent);
                dc.DrawEllipse(glow, null, pCenter, pR, pR);
                break;
        }
    }

    private void RenderSelectionGizmo(DrawingContext dc, SceneItem item)
    {
        var rect = new Rect(item.X, item.Y, item.Width, item.Height);

        // Selection border
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 220, 255)), 2);
        dc.DrawRectangle(null, borderPen, rect);

        // 8 Handles
        double handleSize = 10;
        var handleBrush = Brushes.White;
        var handlePen = new Pen(Brushes.Black, 1.5);

        Point[] handles =
        [
            rect.TopLeft,
            new Point(rect.Left + rect.Width / 2, rect.Top),
            rect.TopRight,
            new Point(rect.Right, rect.Top + rect.Height / 2),
            rect.BottomRight,
            new Point(rect.Left + rect.Width / 2, rect.Bottom),
            rect.BottomLeft,
            new Point(rect.Left, rect.Top + rect.Height / 2)
        ];

        foreach (var p in handles)
        {
            var hRect = new Rect(p.X - handleSize / 2, p.Y - handleSize / 2, handleSize, handleSize);
            dc.DrawRectangle(handleBrush, handlePen, hRect);
        }
    }

    // --- Interactive Mouse Handlers ---
    private Point CanvasToVirtual(Point p)
    {
        double scaleX = 1920.0 / Math.Max(1, ActualWidth);
        double scaleY = 1080.0 / Math.Max(1, ActualHeight);
        return new Point(p.X * scaleX, p.Y * scaleY);
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsEditorMode) return;
        Focus();
        Point p = CanvasToVirtual(e.GetPosition(this));

        // Check handle click on currently selected item
        if (SelectedItem != null && !SelectedItem.Locked)
        {
            var handle = HitTestHandles(SelectedItem, p);
            if (handle != DragMode.None)
            {
                currentDrag = handle;
                dragStartMouse = p;
                dragStartItemBounds = new Rect(SelectedItem.X, SelectedItem.Y, SelectedItem.Width, SelectedItem.Height);
                CaptureMouse();
                return;
            }
        }

        // Hit test items from top to bottom
        SceneItem? hit = null;
        for (int i = model.Items.Count - 1; i >= 0; i--)
        {
            var item = model.Items[i];
            if (!item.Visible) continue;
            var rect = new Rect(item.X, item.Y, item.Width, item.Height);
            if (rect.Contains(p))
            {
                hit = item;
                break;
            }
        }

        SelectedItem = hit;
        SelectionChanged?.Invoke(SelectedItem);

        if (SelectedItem != null && !SelectedItem.Locked)
        {
            currentDrag = DragMode.Move;
            dragStartMouse = p;
            dragStartItemBounds = new Rect(SelectedItem.X, SelectedItem.Y, SelectedItem.Width, SelectedItem.Height);
            CaptureMouse();
        }

        InvalidateVisual();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!IsEditorMode) return;
        Point p = CanvasToVirtual(e.GetPosition(this));

        if (currentDrag != DragMode.None && SelectedItem != null)
        {
            double dx = p.X - dragStartMouse.X;
            double dy = p.Y - dragStartMouse.Y;

            switch (currentDrag)
            {
                case DragMode.Move:
                    SelectedItem.X = Math.Round(dragStartItemBounds.X + dx);
                    SelectedItem.Y = Math.Round(dragStartItemBounds.Y + dy);
                    break;
                case DragMode.ResizeBR:
                    SelectedItem.Width = Math.Max(40, Math.Round(dragStartItemBounds.Width + dx));
                    SelectedItem.Height = Math.Max(20, Math.Round(dragStartItemBounds.Height + dy));
                    break;
                case DragMode.ResizeR:
                    SelectedItem.Width = Math.Max(40, Math.Round(dragStartItemBounds.Width + dx));
                    break;
                case DragMode.ResizeB:
                    SelectedItem.Height = Math.Max(20, Math.Round(dragStartItemBounds.Height + dy));
                    break;
                case DragMode.ResizeTL:
                    double newW_TL = Math.Max(40, dragStartItemBounds.Width - dx);
                    double newH_TL = Math.Max(20, dragStartItemBounds.Height - dy);
                    SelectedItem.X = Math.Round(dragStartItemBounds.Right - newW_TL);
                    SelectedItem.Y = Math.Round(dragStartItemBounds.Bottom - newH_TL);
                    SelectedItem.Width = newW_TL;
                    SelectedItem.Height = newH_TL;
                    break;
                case DragMode.ResizeTR:
                    double newW_TR = Math.Max(40, Math.Round(dragStartItemBounds.Width + dx));
                    double newH_TR = Math.Max(20, dragStartItemBounds.Height - dy);
                    SelectedItem.Y = Math.Round(dragStartItemBounds.Bottom - newH_TR);
                    SelectedItem.Width = newW_TR;
                    SelectedItem.Height = newH_TR;
                    break;
                case DragMode.ResizeBL:
                    double newW_BL = Math.Max(40, dragStartItemBounds.Width - dx);
                    double newH_BL = Math.Max(20, Math.Round(dragStartItemBounds.Height + dy));
                    SelectedItem.X = Math.Round(dragStartItemBounds.Right - newW_BL);
                    SelectedItem.Width = newW_BL;
                    SelectedItem.Height = newH_BL;
                    break;
                case DragMode.ResizeT:
                    double newH_T = Math.Max(20, dragStartItemBounds.Height - dy);
                    SelectedItem.Y = Math.Round(dragStartItemBounds.Bottom - newH_T);
                    SelectedItem.Height = newH_T;
                    break;
                case DragMode.ResizeL:
                    double newW_L = Math.Max(40, dragStartItemBounds.Width - dx);
                    SelectedItem.X = Math.Round(dragStartItemBounds.Right - newW_L);
                    SelectedItem.Width = newW_L;
                    break;
            }

            ItemModified?.Invoke();
            InvalidateVisual();
            return;
        }

        // Update cursor
        if (SelectedItem != null)
        {
            var h = HitTestHandles(SelectedItem, p);
            Cursor = h switch
            {
                DragMode.ResizeTL or DragMode.ResizeBR => Cursors.SizeNWSE,
                DragMode.ResizeTR or DragMode.ResizeBL => Cursors.SizeNESW,
                DragMode.ResizeT or DragMode.ResizeB => Cursors.SizeNS,
                DragMode.ResizeL or DragMode.ResizeR => Cursors.SizeWE,
                _ => Cursors.Arrow
            };
        }
        else
        {
            Cursor = Cursors.Arrow;
        }
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (currentDrag != DragMode.None)
        {
            currentDrag = DragMode.None;
            ReleaseMouseCapture();
            ItemModified?.Invoke();
        }
    }

    private void OnCanvasKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEditorMode || SelectedItem == null || SelectedItem.Locked) return;

        double step = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 10 : 1;
        bool handled = true;

        switch (e.Key)
        {
            case Key.Left: SelectedItem.X -= step; break;
            case Key.Right: SelectedItem.X += step; break;
            case Key.Up: SelectedItem.Y -= step; break;
            case Key.Down: SelectedItem.Y += step; break;
            case Key.Delete:
                model.Items.Remove(SelectedItem);
                SelectedItem = null;
                SelectionChanged?.Invoke(null);
                break;
            default: handled = false; break;
        }

        if (handled)
        {
            ItemModified?.Invoke();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    private static DragMode HitTestHandles(SceneItem item, Point p)
    {
        var r = new Rect(item.X, item.Y, item.Width, item.Height);
        double tol = 12;

        if (IsNear(p, r.TopLeft, tol)) return DragMode.ResizeTL;
        if (IsNear(p, r.TopRight, tol)) return DragMode.ResizeTR;
        if (IsNear(p, r.BottomRight, tol)) return DragMode.ResizeBR;
        if (IsNear(p, r.BottomLeft, tol)) return DragMode.ResizeBL;
        if (IsNear(p, new Point(r.Right, r.Top + r.Height / 2), tol)) return DragMode.ResizeR;
        if (IsNear(p, new Point(r.Left + r.Width / 2, r.Bottom), tol)) return DragMode.ResizeB;
        if (IsNear(p, new Point(r.Left, r.Top + r.Height / 2), tol)) return DragMode.ResizeL;
        if (IsNear(p, new Point(r.Left + r.Width / 2, r.Top), tol)) return DragMode.ResizeT;

        return DragMode.None;
    }

    private static bool IsNear(Point p1, Point p2, double tolerance) =>
        Math.Abs(p1.X - p2.X) <= tolerance && Math.Abs(p1.Y - p2.Y) <= tolerance;

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
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return fallback; }
    }

    private static BitmapSource? LoadCover(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return null;
        try { return ImageLoader.Load(path, 400); }
        catch { return null; }
    }
}
