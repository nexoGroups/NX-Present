using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NXPresent;

internal sealed class CueShowFeed : SourceFeed
{
    public CueShowFeed(Cue cue) : base(cue) { }

    protected override Task LoadAsync(CancellationToken cancellation) => Task.CompletedTask;

    public override FrameworkElement CreateView(Stretch fit)
    {
        var grid = new Grid { Background = new SolidColorBrush(Color.FromRgb(12, 16, 26)) };

        var border = new Border
        {
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Color.FromRgb(18, 24, 38)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)),
            BorderThickness = new Thickness(2),
            Margin = new Thickness(40),
            Padding = new Thickness(24)
        };

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = "",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 64,
            Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "CUE-SHOW ABLAUF",
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.Bold,
            FontSize = 26,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Live-Signal des Show-Ablaufs (QLab Mode)",
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 200, 215, 230)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });

        border.Child = stack;
        grid.Children.Add(border);
        return grid;
    }

    protected override void Release() { }

    public static BitmapSource CaptureThumbnail()
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(16, 22, 34)), null, new Rect(0, 0, 240, 135));
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(24, 32, 50)), new Pen(new SolidColorBrush(Color.FromRgb(56, 189, 248)), 2), new Rect(10, 10, 220, 115), 8, 8);
            var ft = new FormattedText("CUE-SHOW", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                18, Brushes.White, 1.25);
            dc.DrawText(ft, new Point((240 - ft.Width) / 2, 52));
        }
        var bmp = new RenderTargetBitmap(240, 135, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();
        return bmp;
    }
}
