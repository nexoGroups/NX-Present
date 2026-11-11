using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace NXPresent;

public sealed class TrackInfo
{
    public string Title { get; init; } = "Kein Titel aktiv";
    public string Artist { get; init; } = "Unbekannter Künstler";
    public string Album { get; init; } = "";
    public BitmapSource? Cover { get; init; }
    public bool HasTrack => Title != "Kein Titel aktiv" && !string.IsNullOrWhiteSpace(Title);
}

public sealed class NowPlayingProvider
{
    private static readonly Lazy<NowPlayingProvider> instance = new(() => new NowPlayingProvider());
    public static NowPlayingProvider Instance => instance.Value;

    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private GlobalSystemMediaTransportControlsSessionManager? manager;
    private GlobalSystemMediaTransportControlsSession? currentSession;
    private TrackInfo currentTrack = new();
    private bool initialized;

    public TrackInfo CurrentTrack => currentTrack;
    public event Action<TrackInfo>? TrackChanged;

    private NowPlayingProvider()
    {
        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        if (initialized) return;
        try
        {
            manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (manager != null)
            {
                manager.CurrentSessionChanged += Manager_CurrentSessionChanged;
                UpdateCurrentSession();
                initialized = true;
            }
        }
        catch (Exception ex)
        {
            App.Log(ex);
        }
    }

    private void Manager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        dispatcher.BeginInvoke(() => UpdateCurrentSession());
    }

    private void UpdateCurrentSession()
    {
        if (manager == null) return;
        try
        {
            if (currentSession != null)
            {
                currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
            }
            currentSession = manager.GetCurrentSession();
            if (currentSession != null)
            {
                currentSession.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                _ = RefreshTrackInfoAsync();
            }
            else
            {
                currentTrack = new TrackInfo();
                TrackChanged?.Invoke(currentTrack);
            }
        }
        catch (Exception ex)
        {
            App.Log(ex);
        }
    }

    private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        dispatcher.BeginInvoke(async () => await RefreshTrackInfoAsync());
    }

    public async Task RefreshTrackInfoAsync()
    {
        if (currentSession == null) return;
        try
        {
            var props = await currentSession.TryGetMediaPropertiesAsync();
            if (props != null)
            {
                BitmapSource? cover = null;
                if (props.Thumbnail != null)
                {
                    try
                    {
                        using var stream = await props.Thumbnail.OpenReadAsync();
                        using var netStream = stream.AsStreamForRead();
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = netStream;
                        bi.EndInit();
                        bi.Freeze();
                        cover = bi;
                    }
                    catch { }
                }

                currentTrack = new TrackInfo
                {
                    Title = string.IsNullOrWhiteSpace(props.Title) ? "Unbekannter Titel" : props.Title,
                    Artist = string.IsNullOrWhiteSpace(props.Artist) ? "Unbekannter Interpret" : props.Artist,
                    Album = props.AlbumTitle ?? "",
                    Cover = cover
                };
                TrackChanged?.Invoke(currentTrack);
            }
        }
        catch (Exception ex)
        {
            App.Log(ex);
        }
    }
}
