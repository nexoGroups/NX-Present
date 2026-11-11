using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace NXPresent;

/// <summary>A source shown in the AUX patcher or media pool.</summary>
public sealed class BusKey : INotifyPropertyChanged
{
    private bool isProgram, isPreview, isRouted, thumbnailRequested;
    private ImageSource? thumbnail;
    private string? thumbnailError;

    public BusKey(Cue cue) => Cue = cue;

    public Cue Cue { get; }
    public bool IsBlack => Cue == Cue.Black;
    public string Name => IsBlack ? "SCHWARZ" : Cue.Name;
    public string Glyph => IsBlack ? "" : Cue.Glyph;
    public string Category => IsBlack ? "EINGANG" : Cue.Category;
    public bool IsProgram { get => isProgram; set { if (isProgram == value) return; isProgram = value; Changed(nameof(IsProgram)); } }
    public bool IsPreview { get => isPreview; set { if (isPreview == value) return; isPreview = value; Changed(nameof(IsPreview)); } }
    public bool IsRouted { get => isRouted; set { if (isRouted == value) return; isRouted = value; Changed(nameof(IsRouted)); } }
    public ImageSource? Thumbnail { get => thumbnail; internal set { thumbnail = value; Changed(nameof(Thumbnail)); Changed(nameof(HasThumbnail)); } }
    public bool HasThumbnail => thumbnail != null;
    public string? ThumbnailError { get => thumbnailError; internal set { thumbnailError = value; Changed(nameof(ThumbnailError)); } }

    /// <summary>Renders the picture for the media pool, once, off the UI thread where possible.</summary>
    public void RequestThumbnail()
    {
        if (thumbnailRequested) return;
        thumbnailRequested = true;
        var dispatcher = Dispatcher.CurrentDispatcher;
        if (Cue.Kind == CueKind.Image)
        {
            string path = Cue.Source;
            _ = Task.Run(() =>
            {
                try { return ImageLoader.Load(path, 240); }
                catch (Exception) { return null; }
            }).ContinueWith(task =>
            {
                dispatcher.BeginInvoke(() =>
                {
                    if (task.Status == TaskStatus.RanToCompletion && task.Result != null) Thumbnail = task.Result;
                    else ThumbnailError = "Bilddatei konnte nicht gelesen werden.";
                });
            });
        }
        else if (Cue.Kind is CueKind.Pattern or CueKind.Clock or CueKind.Text)
        {
            try { Thumbnail = CueVisual.Capture(CueVisual.Create(Cue, false, live: false), new Size(1920, 1080), 240); }
            catch (Exception ex) { App.Log(ex); }
        }
        else if (Cue.Kind == CueKind.CueShow)
        {
            try { Thumbnail = CueShowFeed.CaptureThumbnail(); }
            catch (Exception ex) { App.Log(ex); }
        }
        else if (Cue.Kind == CueKind.WindowCapture)
        {
            _ = Task.Run(() =>
            {
                try { return WindowCaptureFeed.CaptureThumbnail(Cue); }
                catch (Exception) { return null; }
            }).ContinueWith(task =>
            {
                dispatcher.BeginInvoke(() =>
                {
                    if (task.Status == TaskStatus.RanToCompletion && task.Result != null) Thumbnail = task.Result;
                    else ThumbnailError = "Fenster konnte nicht erfasst werden.";
                });
            });
        }
        else if (Cue.Kind == CueKind.Scene)
        {
            _ = Task.Run(() =>
            {
                try { return SceneFeed.CaptureThumbnail(Cue); }
                catch (Exception) { return null; }
            }).ContinueWith(task =>
            {
                dispatcher.BeginInvoke(() =>
                {
                    if (task.Status == TaskStatus.RanToCompletion && task.Result != null) Thumbnail = task.Result;
                    else ThumbnailError = "Szene konnte nicht geladen werden.";
                });
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
