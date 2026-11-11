using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace NXPresent;

internal sealed class AuxChannel(int number, BusKey source) : INotifyPropertyChanged
{
    private bool isSelected;
    public int Number { get; } = number;
    public string Name => $"AUX {Number}";
    public IReadOnlyList<BusKey> Choices { get; private set; } = [];
    public BusKey Source { get; private set; } = source;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value) return;
            isSelected = value;
            PropertyChanged?.Invoke(this, new(nameof(IsSelected)));
        }
    }
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
    private BusKey[] windowCaptureKeys = [];
    private readonly BusKey cueShowKey = new(Cue.ShowRunner);
    private readonly ObservableCollection<BusKey> auxSourceKeys = new();
    private ICollectionView auxSourceView = null!;
    private string auxSourceFilter = "all";
    private AuxChannel? selectedAuxChannel;
    private bool syncingAux, refreshingCapture, samplingAux;
    private CancellationTokenSource? auxSampleCancellation;
    private BusKey? samplingKey;

    public ItemsControl AuxRoutes => AuxOutputs;

    private void InitializeAux()
    {
        blackKey.RequestThumbnail();
        cueShowKey.RequestThumbnail();
        auxChannels = Enumerable.Range(1, AuxRouting.ChannelCount).Select(n => new AuxChannel(n, blackKey)).ToArray();
        AuxTiles.ItemsSource = auxChannels;
        AuxOutputs.ItemsSource = auxChannels;
        auxSourceView = CollectionViewSource.GetDefaultView(auxSourceKeys);
        auxSourceView.Filter = MatchesAuxSource;
        AuxSources.ItemsSource = auxSourceView;
        RestoreAux(savedSettings.AuxSources);
        if (auxChannels.Length > 0) SelectAuxDestination(auxChannels[0]);
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
        var choices = new List<BusKey> { blackKey, cueShowKey };
        choices.AddRange(captureKeys);
        choices.AddRange(windowCaptureKeys);
        choices.AddRange(poolKeys.DistinctBy(key => key.Cue));
        foreach (var cue in routes.Where(cue => cue.Kind is CueKind.Capture or CueKind.WindowCapture or CueKind.Scene or CueKind.CueShow).DistinctBy(cue => cue.Source))
        {
            if (choices.Any(key => key.Cue.Kind == cue.Kind && key.Cue.Source == cue.Source)) continue;
            var existing = auxChannels.Select(channel => channel.Source).FirstOrDefault(key => key.Cue == cue);
            choices.Add(existing ?? new BusKey(cue));
        }

        auxSourceKeys.Clear();
        foreach (var key in choices)
        {
            key.RequestThumbnail();
            auxSourceKeys.Add(key);
        }

        syncingAux = true;
        try
        {
            for (int i = 0; i < auxChannels.Length; i++)
            {
                var cue = routes[i];
                var key = choices.FirstOrDefault(key => (cue.Kind is CueKind.Capture or CueKind.WindowCapture or CueKind.Scene)
                    ? key.Cue.Kind == cue.Kind && key.Cue.Source == cue.Source
                    : key.Cue == cue);
                auxChannels[i].UpdateChoices(choices, key ?? blackKey);
            }
        }
        finally { syncingAux = false; }

        if (selectedAuxChannel == null && auxChannels.Length > 0)
            SelectAuxDestination(auxChannels[0]);
        else
            SyncAuxRoutedState();

        RefreshAuxSourceView();
        ScheduleAuxThumbnails();
    }

    internal void SelectAuxDestination(AuxChannel channel)
    {
        selectedAuxChannel = channel;
        foreach (var ch in auxChannels)
            ch.IsSelected = (ch == channel);

        if (SelectedAuxTargetHeader != null)
            SelectedAuxTargetHeader.Text = $"Aktives Routing-Ziel: {channel.Name}";
        if (SelectedAuxTargetLabel != null)
            SelectedAuxTargetLabel.Text = $"Zuweisung für: {channel.Name} (aktuell: {channel.Source.Name})";

        SyncAuxRoutedState();
    }

    private void SyncAuxRoutedState()
    {
        var targetCue = selectedAuxChannel?.Source.Cue;
        foreach (var key in auxSourceKeys)
            key.IsRouted = (targetCue != null && key.Cue == targetCue);
    }

    private void RouteAux(AuxChannel channel, BusKey key)
    {
        if (channel.Source == key) return;
        key.ThumbnailError = null;
        channel.SetSource(key);
        SyncAuxRoutedState();
        if (SelectedAuxTargetLabel != null)
            SelectedAuxTargetLabel.Text = $"Zuweisung für: {channel.Name} (aktuell: {key.Name})";
        dirty = true;
        QueueSettingsSave();
        Status($"{channel.Name} belegt mit {key.Name}.");
        ScheduleAuxThumbnails();
    }

    private void AuxPatcherChannelClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AuxChannel channel })
            SelectAuxDestination(channel);
    }

    private void AuxPatcherSourceClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BusKey key } && selectedAuxChannel != null)
            RouteAux(selectedAuxChannel, key);
    }

    private async void AuxOutputPreviewClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget is FrameworkElement { DataContext: AuxChannel channel })
                await SelectStandby(channel.Source.Cue, channel.Name);
        }
        catch (Exception ex) { App.Log(ex); Status($"Vorschau fehlgeschlagen: {ex.Message}"); }
    }

    private async void AuxOutputCutClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget is FrameworkElement { DataContext: AuxChannel channel })
                await HotCut(channel.Source.Cue);
        }
        catch (Exception ex) { App.Log(ex); Status($"Schnitt fehlgeschlagen: {ex.Message}"); }
    }

    private void AuxOutputEditSceneClicked(object sender, RoutedEventArgs e)
    {
        if (((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget is FrameworkElement { DataContext: AuxChannel channel })
            OpenSceneEditorForAux(channel);
    }

    private void EditCurrentAuxScene(object sender, RoutedEventArgs e)
    {
        if (selectedAuxChannel != null)
            OpenSceneEditorForAux(selectedAuxChannel);
    }

    private void OpenSceneEditorForAux(AuxChannel channel)
    {
        SceneModel initial;
        if (channel.Source.Cue.Kind == CueKind.Scene)
        {
            initial = SceneModel.FromJson(channel.Source.Cue.Source);
        }
        else
        {
            initial = SceneModel.CreatePreShowTemplate();
            initial.Name = $"{channel.Name}: Szene";
        }

        var editor = new SceneEditorWindow(initial, channel.Number) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            var cue = new Cue(CueKind.Scene, editor.ResultModel.Name, editor.ResultModel.ToJson());
            if (editor.TargetAuxChannel is { } targetNum)
            {
                var targetChannel = auxChannels.FirstOrDefault(ch => ch.Number == targetNum) ?? channel;
                var key = new BusKey(cue);
                key.RequestThumbnail();
                RouteAux(targetChannel, key);
                if (!cues.Any(c => c.Kind == CueKind.Scene && c.Name == cue.Name)) cues.Add(cue);
            }
            else
            {
                AddCue(cue);
            }
        }
    }

    private bool MatchesAuxSource(object value)
    {
        if (value is not BusKey key) return false;
        var cue = key.Cue;
        bool category = auxSourceFilter switch
        {
            "capture" => cue.Kind == CueKind.Capture || key.IsBlack,
            "windows" => cue.Kind == CueKind.WindowCapture,
            "scenes" => cue.Kind is CueKind.Scene or CueKind.CueShow,
            "cueshow" => cue.Kind == CueKind.CueShow,
            "media" => cue.Kind is CueKind.Image or CueKind.Video or CueKind.Website or CueKind.Text or CueKind.Clock,
            "patterns" => cue.Kind == CueKind.Pattern,
            _ => true
        };
        string search = AuxSourceSearch?.Text.Trim() ?? "";
        return category && (search.Length == 0 || key.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) || key.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase));
    }

    private void RefreshAuxSourceView()
    {
        if (AuxSourceCount == null || auxSourceView == null) return;
        auxSourceView.Refresh();
        int shown = auxSourceView.Cast<object>().Count();
        AuxSourceCount.Text = shown == auxSourceKeys.Count ? $"{auxSourceKeys.Count} QUELLEN" : $"{shown} VON {auxSourceKeys.Count} QUELLEN";
        if (EmptyAuxSources != null)
            EmptyAuxSources.Visibility = shown == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AuxSourceFilterChanged(object sender, RoutedEventArgs e)
    {
        auxSourceFilter = (sender as RadioButton)?.Tag as string ?? "all";
        RefreshAuxSourceView();
    }

    private void AuxSourceSearchChanged(object sender, TextChangedEventArgs e) => RefreshAuxSourceView();

    private void AuxSourceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncingAux || sender is not ComboBox { DataContext: AuxChannel channel, SelectedItem: BusKey key } || channel.Source == key) return;
        RouteAux(channel, key);
    }

    private async void AuxClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement { DataContext: AuxChannel channel }) await SelectStandby(channel.Source.Cue, channel.Name);
        }
        catch (Exception ex) { App.Log(ex); Status($"Auswahl fehlgeschlagen: {ex.Message}"); }
    }

    private async void RefreshCaptureClicked(object sender, RoutedEventArgs e)
    {
        try { await RefreshCaptureSourcesAsync(); }
        catch (Exception ex) { App.Log(ex); }
    }

    private async Task RefreshCaptureSourcesAsync()
    {
        if (refreshingCapture || isolated) return;
        refreshingCapture = true;
        RefreshCaptureButton.IsEnabled = false;
        try
        {
            var devices = await CaptureFeed.FindSourcesAsync();
            var windows = WindowCaptureFeed.FindWindows();
            if (closing) return;
            var known = captureKeys.Concat(windowCaptureKeys).Concat(auxChannels.Select(channel => channel.Source)).ToArray();
            captureKeys = devices.Select(cue => known.FirstOrDefault(key => key.Cue == cue) ?? new BusKey(cue)).ToArray();
            windowCaptureKeys = windows.Select(cue => known.FirstOrDefault(key => key.Cue.Kind == CueKind.WindowCapture && key.Cue.Source == cue.Source) ?? new BusKey(cue)).ToArray();
            var available = devices.Select(cue => cue.Source).ToHashSet();
            foreach (var key in known.Where(key => key.Cue.Kind == CueKind.Capture && !available.Contains(key.Cue.Source)).Distinct())
            {
                if (samplingKey == key) CancelAuxSampling();
                key.Thumbnail = null;
                key.ThumbnailError = "Aufnahmegerät nicht verbunden.";
            }
            var availableWindows = windows.Select(cue => cue.Source).ToHashSet();
            foreach (var key in known.Where(key => key.Cue.Kind == CueKind.WindowCapture && !availableWindows.Contains(key.Cue.Source)).Distinct())
            {
                if (samplingKey == key) CancelAuxSampling();
                key.Thumbnail = null;
                key.ThumbnailError = "Fenster nicht mehr verfügbar.";
            }
            foreach (var key in captureKeys) key.ThumbnailError = null;
            foreach (var key in windowCaptureKeys) key.ThumbnailError = null;
            RefreshAuxChoices();
            CaptureSummary.Text = $"{devices.Length} Aufnahmegerät(e), {windows.Count} Fenster erkannt.";
        }
        catch (Exception ex)
        {
            App.Log(ex);
            if (!closing) CaptureSummary.Text = "Aufnahmegeräte & Fenster konnten nicht gelesen werden.";
        }
        finally
        {
            refreshingCapture = false;
            RefreshCaptureButton.IsEnabled = true;
        }
    }

    private void CancelAuxSampling()
    {
        try { auxSampleCancellation?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    private void ScheduleAuxThumbnails()
    {
        if (!auxChannels.Any(channel => channel.Source == samplingKey)) CancelAuxSampling();
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
                    && key.Cue.Kind is CueKind.Video or CueKind.Website or CueKind.Capture or CueKind.WindowCapture or CueKind.Scene);
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
                    // Cancelled during fast route change; do not permanently disable thumbnails
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
