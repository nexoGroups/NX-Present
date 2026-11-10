using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NXPresent;

internal sealed class AuxChannel(int number, BusKey source) : INotifyPropertyChanged
{
    public int Number { get; } = number;
    public string Name => $"AUX {Number}";
    public IReadOnlyList<BusKey> Choices { get; private set; } = [];
    public BusKey Source { get; private set; } = source;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdateChoices(IReadOnlyList<BusKey> choices, BusKey selected)
    {
        Choices = choices;
        PropertyChanged?.Invoke(this, new(nameof(Choices)));
        SetSource(selected);
    }

    public void SetSource(BusKey selected)
    {
        Source = selected;
        PropertyChanged?.Invoke(this, new(nameof(Source)));
    }
}

public partial class MainWindow
{
    private AuxChannel[] auxChannels = [];
    private BusKey[] captureKeys = [];
    private bool syncingAux, refreshingCapture, samplingAux;
    private CancellationTokenSource? auxSampleCancellation;
    private BusKey? samplingKey;

    private void InitializeAux()
    {
        blackKey.RequestThumbnail();
        auxChannels = Enumerable.Range(1, AuxRouting.ChannelCount).Select(n => new AuxChannel(n, blackKey)).ToArray();
        AuxTiles.ItemsSource = AuxRoutes.ItemsSource = auxChannels;
        RestoreAux(savedSettings.AuxSources);
    }

    private void RestoreAux(IReadOnlyList<Cue>? routes)
    {
        var selections = routes ?? cues.Take(AuxRouting.ChannelCount).ToArray();
        RefreshAuxChoices(Enumerable.Range(0, AuxRouting.ChannelCount).Select(index =>
            index < selections.Count ? selections[index] : Cue.Black).ToArray());
    }

    private List<Cue> SnapshotAux() => auxChannels.Select(channel => channel.Source.Cue).ToList();

    private void RefreshAuxChoices(IReadOnlyList<Cue>? routes = null)
    {
        if (auxChannels.Length == 0) return;
        routes ??= SnapshotAux();
        var choices = new List<BusKey> { blackKey };
        choices.AddRange(poolKeys.DistinctBy(key => key.Cue));
        choices.AddRange(captureKeys);
        foreach (var cue in routes.Where(cue => cue.Kind == CueKind.Capture).DistinctBy(cue => cue.Source))
        {
            if (choices.Any(key => key.Cue.Kind == CueKind.Capture && key.Cue.Source == cue.Source)) continue;
            var existing = auxChannels.Select(channel => channel.Source).FirstOrDefault(key => key.Cue == cue);
            choices.Add(existing ?? new BusKey(cue));
        }
        syncingAux = true;
        try
        {
            for (int i = 0; i < auxChannels.Length; i++)
            {
                var cue = routes[i];
                var key = choices.FirstOrDefault(key => cue.Kind == CueKind.Capture
                    ? key.Cue.Kind == CueKind.Capture && key.Cue.Source == cue.Source
                    : key.Cue == cue);
                auxChannels[i].UpdateChoices(choices, key ?? blackKey);
            }
        }
        finally { syncingAux = false; }
        ScheduleAuxThumbnails();
    }

    private void AuxSourceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncingAux || sender is not ComboBox { DataContext: AuxChannel channel, SelectedItem: BusKey key } || channel.Source == key) return;
        channel.SetSource(key);
        dirty = true;
        QueueSettingsSave();
        Status($"{channel.Name} belegt mit {key.Name}.");
        ScheduleAuxThumbnails();
    }

    private async void AuxClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AuxChannel channel }) await SelectStandby(channel.Source.Cue, channel.Name);
    }

    private async void RefreshCaptureClicked(object sender, RoutedEventArgs e) => await RefreshCaptureSourcesAsync();

    private async Task RefreshCaptureSourcesAsync()
    {
        if (refreshingCapture || isolated) return;
        refreshingCapture = true;
        RefreshCaptureButton.IsEnabled = false;
        try
        {
            var devices = await CaptureFeed.FindSourcesAsync();
            if (closing) return;
            var known = captureKeys.Concat(auxChannels.Select(channel => channel.Source)).ToArray();
            captureKeys = devices.Select(cue => known.FirstOrDefault(key => key.Cue == cue) ?? new BusKey(cue)).ToArray();
            var available = devices.Select(cue => cue.Source).ToHashSet();
            foreach (var key in known.Where(key => key.Cue.Kind == CueKind.Capture && !available.Contains(key.Cue.Source)).Distinct())
            {
                if (samplingKey == key) auxSampleCancellation?.Cancel();
                key.Thumbnail = null;
                key.ThumbnailError = "Aufnahmegerät nicht verbunden.";
            }
            foreach (var key in captureKeys) key.ThumbnailError = null;
            RefreshAuxChoices();
            CaptureSummary.Text = devices.Length == 0 ? "Keine Aufnahmegeräte erkannt." : $"{devices.Length} Aufnahmegerät(e) erkannt.";
        }
        catch (Exception ex)
        {
            App.Log(ex);
            if (!closing) CaptureSummary.Text = "Aufnahmegeräte konnten nicht gelesen werden.";
        }
        finally
        {
            refreshingCapture = false;
            RefreshCaptureButton.IsEnabled = true;
        }
    }

    private void ScheduleAuxThumbnails()
    {
        if (!auxChannels.Any(channel => channel.Source == samplingKey)) auxSampleCancellation?.Cancel();
        if (!samplingAux && !closing) _ = CaptureAuxThumbnailsAsync();
    }

    private async Task CaptureAuxThumbnailsAsync()
    {
        samplingAux = true;
        try
        {
            while (!closing)
            {
                var key = auxChannels.Select(channel => channel.Source).FirstOrDefault(key => !key.HasThumbnail && key.ThumbnailError == null
                    && key.Cue.Kind is CueKind.Video or CueKind.Website or CueKind.Capture);
                if (key == null) break;
                using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                auxSampleCancellation = cancellation;
                samplingKey = key;
                try
                {
                    var active = new[] { switcher.Program, switcher.Preview }.FirstOrDefault(feed => feed?.Cue == key.Cue && feed.State == FeedState.Ready);
                    if (active != null) CaptureAuxThumbnail(key, active);
                    else
                    {
                        using var feed = factory.Create(key.Cue);
                        await feed.OpenAsync(cancellation.Token);
                        if (feed.State == FeedState.Ready) CaptureAuxThumbnail(key, feed);
                        else key.ThumbnailError = feed.Error ?? "Kein Vorschaubild verfügbar.";
                    }
                }
                catch (OperationCanceledException)
                {
                    if (key.ThumbnailError == null && auxChannels.Any(channel => channel.Source == key)) key.ThumbnailError = "Kein Vorschaubild empfangen.";
                }
                catch (Exception ex) { App.Log(ex); key.ThumbnailError = "Vorschaubild konnte nicht erstellt werden."; }
                finally { auxSampleCancellation = null; samplingKey = null; }
            }
        }
        finally { samplingAux = false; }
    }

    private static void CaptureAuxThumbnail(BusKey key, SourceFeed feed)
    {
        key.Thumbnail = CueVisual.Capture(feed.CreateView(Stretch.Uniform), new Size(1920, 1080), 240);
        key.ThumbnailError = null;
    }
}
