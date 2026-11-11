using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;

namespace NXPresent;

public partial class SceneEditorWindow : Window
{
    private readonly SceneCanvasView canvas;
    private SceneModel model;
    private readonly ObservableCollection<SceneItem> layers = new();
    private bool syncing = true;

    public SceneModel ResultModel => model;
    public int? TargetAuxChannel { get; private set; } // 1..10 or null for pool

    public SceneEditorWindow(SceneModel? initialModel = null, int? preselectedAuxChannel = null)
    {
        InitializeComponent();
        model = initialModel ?? SceneModel.CreatePreShowTemplate();

        canvas = new SceneCanvasView(model)
        {
            IsEditorMode = true,
            Live = true
        };
        canvas.SelectionChanged += Canvas_SelectionChanged;
        canvas.ItemModified += Canvas_ItemModified;
        CanvasHost.Child = canvas;

        if (preselectedAuxChannel is >= 1 and <= 10)
        {
            TargetChannelPicker.SelectedIndex = preselectedAuxChannel.Value; // 0 = pool, 1 = AUX 1, ...
        }

        syncing = false;
        RebuildLayersList();
        SyncModelToHeader();
    }

    private void SyncModelToHeader()
    {
        syncing = true;
        try
        {
            SceneNameBox.Text = model.Name;
        }
        finally { syncing = false; }
    }

    private void RebuildLayersList()
    {
        layers.Clear();
        foreach (var item in model.Items)
            layers.Add(item);

        LayersList.ItemsSource = layers;
        if (layers.Count > 0 && canvas.SelectedItem == null)
            canvas.SelectedItem = layers[0];

        LayersList.SelectedItem = canvas.SelectedItem;
        UpdatePropertiesView();
    }

    private void Canvas_SelectionChanged(SceneItem? selected)
    {
        if (model.Items.Count != layers.Count)
        {
            RebuildLayersList();
            return;
        }
        syncing = true;
        try
        {
            LayersList.SelectedItem = selected;
            UpdatePropertiesView();
        }
        finally { syncing = false; }
    }

    private void Canvas_ItemModified()
    {
        if (syncing) return;
        UpdateTransformBoxes();
    }

    private void LayersSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing) return;
        canvas.SelectedItem = LayersList.SelectedItem as SceneItem;
        canvas.InvalidateVisual();
        UpdatePropertiesView();
    }

    private void LayerVisibilityChanged(object sender, RoutedEventArgs e)
    {
        canvas.InvalidateVisual();
    }

    private void UpdatePropertiesView()
    {
        var item = canvas.SelectedItem;
        if (item == null)
        {
            NoSelectionHint.Visibility = Visibility.Visible;
            TransformGroup.Visibility = Visibility.Collapsed;
            SongGroup.Visibility = Visibility.Collapsed;
            VizGroup.Visibility = Visibility.Collapsed;
            ClockGroup.Visibility = Visibility.Collapsed;
            CountdownGroup.Visibility = Visibility.Collapsed;
            TextGroup.Visibility = Visibility.Collapsed;
            ColorBoxGroup.Visibility = Visibility.Collapsed;
            WinCaptureGroup.Visibility = Visibility.Collapsed;
            return;
        }

        NoSelectionHint.Visibility = Visibility.Collapsed;
        TransformGroup.Visibility = Visibility.Visible;

        UpdateTransformBoxes();

        syncing = true;
        try
        {
            // Visibility of specific groups
            SongGroup.Visibility = item.Type == SceneItemType.SongInfo ? Visibility.Visible : Visibility.Collapsed;
            VizGroup.Visibility = item.Type == SceneItemType.Visualizer ? Visibility.Visible : Visibility.Collapsed;
            ClockGroup.Visibility = item.Type == SceneItemType.Clock ? Visibility.Visible : Visibility.Collapsed;
            CountdownGroup.Visibility = item.Type == SceneItemType.Countdown ? Visibility.Visible : Visibility.Collapsed;
            TextGroup.Visibility = item.Type == SceneItemType.Text ? Visibility.Visible : Visibility.Collapsed;
            ColorBoxGroup.Visibility = item.Type == SceneItemType.ColorBox ? Visibility.Visible : Visibility.Collapsed;
            WinCaptureGroup.Visibility = item.Type == SceneItemType.WindowCapture ? Visibility.Visible : Visibility.Collapsed;

            // Song fields
            if (item.Type == SceneItemType.SongInfo)
            {
                SongAutoMediaBox.IsChecked = item.AutoMedia;
                SongManualInputs.Visibility = item.AutoMedia ? Visibility.Collapsed : Visibility.Visible;
                SongTitleBox.Text = item.CustomTitle;
                SongArtistBox.Text = item.CustomArtist;
                SongAlbumBox.Text = item.CustomAlbum;
                SongShowCoverBox.IsChecked = item.ShowCover;
                SongCoverStylePicker.SelectedIndex = (int)item.CoverStyle;
                SongFontSizeSlider.Value = item.SongFontSize;
            }
            // Visualizer fields
            else if (item.Type == SceneItemType.Visualizer)
            {
                VizStylePicker.SelectedIndex = (int)item.VizStyle;
                VizThemePicker.SelectedIndex = (int)item.VizTheme;
                VizBarCountSlider.Value = item.BarCount;
                VizSensSlider.Value = item.Sensitivity;
            }
            // Clock fields
            else if (item.Type == SceneItemType.Clock)
            {
                ClockSecondsBox.IsChecked = item.ShowSeconds;
                ClockDateBox.IsChecked = item.ShowDate;
                ClockFontSizeSlider.Value = item.ClockFontSize;
            }
            // Countdown fields
            else if (item.Type == SceneItemType.Countdown)
            {
                CountdownModeTargetRadio.IsChecked = item.CountdownMode == CountdownMode.TargetTime;
                CountdownModeDurationRadio.IsChecked = item.CountdownMode == CountdownMode.Duration;
                CountdownTargetBox.Text = item.TargetTimeText;
                CountdownDurationBox.Text = item.CountdownDurationSeconds.ToString("F0");
                CountdownPrefixBox.Text = item.CountdownPrefix;
                CountdownShowPrefixBox.IsChecked = item.ShowCountdownPrefix;
                CountdownExpiredBox.Text = item.CountdownExpiredText;
                CountdownFontBox.Text = item.CountdownFontSize.ToString("F0");
                CountdownColorBox.Text = item.CountdownColor;
                CountdownTriggerCueBox.IsChecked = item.TriggerCueOnZero;
            }
            // Text fields
            else if (item.Type == SceneItemType.Text)
            {
                TextContentBox.Text = item.TextContent;
                TextBoldBox.IsChecked = item.TextBold;
                TextFontSizeSlider.Value = item.TextFontSize;
            }
            // ColorBox fields
            else if (item.Type == SceneItemType.ColorBox)
            {
                ColorBoxHexBox.Text = item.ColorHex;
                ColorBoxRadiusSlider.Value = item.CornerRadius;
            }
            // WindowCapture fields
            else if (item.Type == SceneItemType.WindowCapture)
            {
                WinSelectedLabel.Text = string.IsNullOrWhiteSpace(item.WindowSource) ? "Kein Fenster gewählt." : item.Name;
            }
        }
        finally { syncing = false; }
    }

    private void UpdateTransformBoxes()
    {
        var item = canvas.SelectedItem;
        if (item == null) return;

        syncing = true;
        try
        {
            PropXBox.Text = item.X.ToString("F0");
            PropYBox.Text = item.Y.ToString("F0");
            PropWBox.Text = item.Width.ToString("F0");
            PropHBox.Text = item.Height.ToString("F0");
            PropOpacitySlider.Value = item.Opacity * 100;
            PropOpacityLabel.Text = $"{Math.Round(item.Opacity * 100)}%";
        }
        finally { syncing = false; }
    }

    private void PropTransformChanged(object sender, TextChangedEventArgs e)
    {
        if (syncing || canvas.SelectedItem is not { } item) return;
        if (double.TryParse(PropXBox.Text, out double x)) item.X = x;
        if (double.TryParse(PropYBox.Text, out double y)) item.Y = y;
        if (double.TryParse(PropWBox.Text, out double w)) item.Width = Math.Max(20, w);
        if (double.TryParse(PropHBox.Text, out double h)) item.Height = Math.Max(20, h);
        canvas.InvalidateVisual();
    }

    private void PropOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (syncing || canvas.SelectedItem is not { } item) return;
        item.Opacity = PropOpacitySlider.Value / 100.0;
        PropOpacityLabel.Text = $"{Math.Round(item.Opacity * 100)}%";
        canvas.InvalidateVisual();
    }

    private void SongPropChanged(object sender, RoutedEventArgs e)
    {
        if (syncing || canvas.SelectedItem is not { } item || item.Type != SceneItemType.SongInfo) return;
        item.AutoMedia = SongAutoMediaBox.IsChecked == true;
        SongManualInputs.Visibility = item.AutoMedia ? Visibility.Collapsed : Visibility.Visible;
        item.CustomTitle = SongTitleBox.Text;
        item.CustomArtist = SongArtistBox.Text;
        item.CustomAlbum = SongAlbumBox.Text;
        item.ShowCover = SongShowCoverBox.IsChecked == true;
        item.CoverStyle = (CoverStyle)Math.Max(0, SongCoverStylePicker.SelectedIndex);
        item.SongFontSize = SongFontSizeSlider.Value;
        canvas.InvalidateVisual();
    }

    private void VizPropChanged(object sender, RoutedEventArgs e)
    {
        if (syncing || canvas.SelectedItem is not { } item || item.Type != SceneItemType.Visualizer) return;
        item.VizStyle = (VisualizerStyle)Math.Max(0, VizStylePicker.SelectedIndex);
        item.VizTheme = (VisualizerTheme)Math.Max(0, VizThemePicker.SelectedIndex);
        item.BarCount = (int)VizBarCountSlider.Value;
        item.Sensitivity = VizSensSlider.Value;
        canvas.InvalidateVisual();
    }

    private void ClockPropChanged(object sender, RoutedEventArgs e)
    {
        if (syncing || canvas.SelectedItem is not { } item || item.Type != SceneItemType.Clock) return;
        item.ShowSeconds = ClockSecondsBox.IsChecked == true;
        item.ShowDate = ClockDateBox.IsChecked == true;
        item.ClockFontSize = ClockFontSizeSlider.Value;
        canvas.InvalidateVisual();
    }

    private void TextPropChanged(object sender, RoutedEventArgs e)
    {
        if (syncing || canvas.SelectedItem is not { } item || item.Type != SceneItemType.Text) return;
        item.TextContent = TextContentBox.Text;
        item.TextBold = TextBoldBox.IsChecked == true;
        item.TextFontSize = TextFontSizeSlider.Value;
        canvas.InvalidateVisual();
    }

    private void ColorBoxPropChanged(object sender, RoutedEventArgs e)
    {
        if (syncing || canvas.SelectedItem is not { } item || item.Type != SceneItemType.ColorBox) return;
        item.ColorHex = ColorBoxHexBox.Text;
        item.CornerRadius = ColorBoxRadiusSlider.Value;
        canvas.InvalidateVisual();
    }

    private void PickWindowForLayer(object sender, RoutedEventArgs e)
    {
        if (canvas.SelectedItem is not { } item || item.Type != SceneItemType.WindowCapture) return;
        var windows = WindowCaptureFeed.FindWindows();
        if (windows.Count == 0)
        {
            MessageBox.Show(this, "Es wurden keine geöffneten Fenster gefunden.", "Fenster erfassen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var picker = new WindowPickerWindow(windows) { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedWindow != null)
        {
            item.WindowSource = picker.SelectedWindow.Source;
            item.Name = picker.SelectedWindow.Name;
            WinSelectedLabel.Text = item.Name;
            canvas.InvalidateVisual();
            RebuildLayersList();
        }
    }

    // --- Toolbar Actions ---
    private void OpenAddSourceMenu(object sender, RoutedEventArgs e)
    {
        AddSourceButton.ContextMenu.PlacementTarget = AddSourceButton;
        AddSourceButton.ContextMenu.Placement = PlacementMode.Bottom;
        AddSourceButton.ContextMenu.IsOpen = true;
    }

    private void AddSongSource(object sender, RoutedEventArgs e)
    {
        var item = new SceneItem
        {
            Name = "Song-Anzeige",
            Type = SceneItemType.SongInfo,
            X = 200,
            Y = 400,
            Width = 800,
            Height = 200,
            AutoMedia = true
        };
        model.Items.Add(item);
        canvas.SelectedItem = item;
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    private void AddVisualizerSource(object sender, RoutedEventArgs e)
    {
        var item = new SceneItem
        {
            Name = "Musik-Visualizer",
            Type = SceneItemType.Visualizer,
            X = 100,
            Y = 750,
            Width = 1720,
            Height = 180,
            VizStyle = VisualizerStyle.Bars,
            VizTheme = VisualizerTheme.Neon
        };
        model.Items.Add(item);
        canvas.SelectedItem = item;
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    private void AddClockSource(object sender, RoutedEventArgs e)
    {
        var item = new SceneItem
        {
            Name = "Uhrzeit",
            Type = SceneItemType.Clock,
            X = 1400,
            Y = 80,
            Width = 400,
            Height = 120,
            ShowSeconds = false,
            ShowDate = true,
            ClockFontSize = 48
        };
        model.Items.Add(item);
        canvas.SelectedItem = item;
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    private void AddCountdownSource(object sender, RoutedEventArgs e)
    {
        var item = new SceneItem
        {
            Name = "Vorstellungs-Countdown",
            Type = SceneItemType.Countdown,
            X = 360,
            Y = 120,
            Width = 1200,
            Height = 240,
            CountdownMode = CountdownMode.TargetTime,
            TargetTimeText = "20:00",
            CountdownDurationSeconds = 900,
            CountdownPrefix = "Die Vorstellung beginnt in",
            ShowCountdownPrefix = true,
            CountdownExpiredText = "Die Vorstellung beginnt gleich …",
            CountdownColor = "#38BDF8",
            CountdownFontSize = 76,
            TriggerCueOnZero = true
        };
        model.Items.Add(item);
        canvas.SelectedItem = item;
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    private void AddWindowSource(object sender, RoutedEventArgs e)
    {
        var windows = WindowCaptureFeed.FindWindows();
        var picker = new WindowPickerWindow(windows) { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedWindow != null)
        {
            var item = new SceneItem
            {
                Name = picker.SelectedWindow.Name,
                Type = SceneItemType.WindowCapture,
                WindowSource = picker.SelectedWindow.Source,
                X = 200,
                Y = 150,
                Width = 960,
                Height = 540
            };
            model.Items.Add(item);
            canvas.SelectedItem = item;
            RebuildLayersList();
            canvas.InvalidateVisual();
        }
    }

    private void AddMediaSource(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Bilder und Videos|*.jpg;*.jpeg;*.png;*.mp4;*.mkv;*.mov|Alle Dateien|*.*" };
        if (dialog.ShowDialog(this) == true)
        {
            var item = new SceneItem
            {
                Name = System.IO.Path.GetFileName(dialog.FileName),
                Type = SceneItemType.Media,
                MediaPath = dialog.FileName,
                X = 250,
                Y = 180,
                Width = 800,
                Height = 450
            };
            model.Items.Add(item);
            canvas.SelectedItem = item;
            RebuildLayersList();
            canvas.InvalidateVisual();
        }
    }

    private void AddTextSource(object sender, RoutedEventArgs e)
    {
        var item = new SceneItem
        {
            Name = "Textfeld",
            Type = SceneItemType.Text,
            TextContent = "Willkommen!",
            X = 300,
            Y = 300,
            Width = 600,
            Height = 120,
            TextFontSize = 36
        };
        model.Items.Add(item);
        canvas.SelectedItem = item;
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    private void AddColorBoxSource(object sender, RoutedEventArgs e)
    {
        var item = new SceneItem
        {
            Name = "Farbfläche",
            Type = SceneItemType.ColorBox,
            X = 100,
            Y = 100,
            Width = 600,
            Height = 350,
            ColorHex = "#162032",
            CornerRadius = 16
        };
        model.Items.Insert(0, item); // Insert background layers at the bottom
        canvas.SelectedItem = item;
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    private void MoveUpClicked(object sender, RoutedEventArgs e)
    {
        if (canvas.SelectedItem is not { } item) return;
        int idx = model.Items.IndexOf(item);
        if (idx > 0)
        {
            model.Items.RemoveAt(idx);
            model.Items.Insert(idx - 1, item);
            RebuildLayersList();
            canvas.SelectedItem = item;
            canvas.InvalidateVisual();
        }
    }

    private void MoveDownClicked(object sender, RoutedEventArgs e)
    {
        if (canvas.SelectedItem is not { } item) return;
        int idx = model.Items.IndexOf(item);
        if (idx >= 0 && idx < model.Items.Count - 1)
        {
            model.Items.RemoveAt(idx);
            model.Items.Insert(idx + 1, item);
            RebuildLayersList();
            canvas.SelectedItem = item;
            canvas.InvalidateVisual();
        }
    }

    private void DuplicateClicked(object sender, RoutedEventArgs e)
    {
        if (canvas.SelectedItem is not { } item) return;
        var clone = item.Clone();
        model.Items.Add(clone);
        canvas.SelectedItem = clone;
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    private void DeleteClicked(object sender, RoutedEventArgs e)
    {
        if (canvas.SelectedItem is not { } item) return;
        model.Items.Remove(item);
        canvas.SelectedItem = null;
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    private void CountdownPropChanged(object sender, RoutedEventArgs e)
    {
        if (syncing || canvas.SelectedItem is not { } item || item.Type != SceneItemType.Countdown) return;
        item.CountdownMode = CountdownModeDurationRadio.IsChecked == true ? CountdownMode.Duration : CountdownMode.TargetTime;
        item.TargetTimeText = CountdownTargetBox.Text.Trim();
        if (double.TryParse(CountdownDurationBox.Text, out double dur)) item.CountdownDurationSeconds = Math.Max(0, dur);
        item.CountdownPrefix = CountdownPrefixBox.Text.Trim();
        item.ShowCountdownPrefix = CountdownShowPrefixBox.IsChecked == true;
        item.CountdownExpiredText = CountdownExpiredBox.Text.Trim();
        if (double.TryParse(CountdownFontBox.Text, out double fs)) item.CountdownFontSize = Math.Clamp(fs, 16, 200);
        item.CountdownColor = CountdownColorBox.Text.Trim();
        item.TriggerCueOnZero = CountdownTriggerCueBox.IsChecked == true;
        canvas.InvalidateVisual();
    }

    private void TemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing || canvas == null) return;
        switch (TemplatePicker.SelectedIndex)
        {
            case 0: model = SceneModel.CreateCinemaCountdownTemplate(); break;
            case 1: model = SceneModel.CreatePreShowTemplate(); break;
            case 2: model = SceneModel.CreateMinimalSongTemplate(); break;
            case 3: model = SceneModel.CreateLowerThirdTemplate(); break;
            case 4: model = new SceneModel { Name = "Leere Szene", Items = new() }; break;
        }
        canvas.Model = model;
        canvas.SelectedItem = null;
        SyncModelToHeader();
        RebuildLayersList();
        canvas.InvalidateVisual();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        canvas.SelectionChanged -= Canvas_SelectionChanged;
        canvas.ItemModified -= Canvas_ItemModified;
        CanvasHost.Child = null;
        canvas.Dispose();
    }

    private void SceneNameChanged(object sender, TextChangedEventArgs e)
    {
        if (syncing) return;
        model.Name = SceneNameBox.Text.Trim();
    }

    private void SaveClicked(object sender, RoutedEventArgs e)
    {
        model.Name = SceneNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(model.Name)) model.Name = "Szene";

        int targetIndex = TargetChannelPicker.SelectedIndex;
        TargetAuxChannel = targetIndex >= 1 ? targetIndex : null;

        DialogResult = true;
    }
}
