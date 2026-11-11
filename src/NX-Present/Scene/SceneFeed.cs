using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NXPresent;

internal sealed class SceneFeed : SourceFeed
{
    private readonly SceneModel model;

    public SceneFeed(Cue cue) : base(cue)
    {
        model = SceneModel.FromJson(cue.Source);
    }

    protected override Task LoadAsync(CancellationToken cancellation)
    {
        return Task.CompletedTask;
    }

    public override FrameworkElement CreateView(Stretch fit)
    {
        return new SceneCanvasView(model) { Live = true };
    }

    protected override void Release()
    {
    }

    public static BitmapSource? CaptureThumbnail(Cue cue)
    {
        try
        {
            var model = SceneModel.FromJson(cue.Source);
            var view = new SceneCanvasView(model) { Live = false };
            return CueVisual.Capture(view, new Size(1920, 1080), 240);
        }
        catch (Exception ex)
        {
            App.Log(ex);
            return null;
        }
    }
}
