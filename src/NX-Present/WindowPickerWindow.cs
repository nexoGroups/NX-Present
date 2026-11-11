using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NXPresent;

internal sealed class WindowPickerWindow : Window
{
    private readonly ListBox list;
    public Cue? SelectedWindow { get; private set; }

    public WindowPickerWindow(IReadOnlyList<Cue> windows)
    {
        Title = "Anwendung auswählen";
        Width = 500;
        Height = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("Bg");
        Foreground = (Brush)Application.Current.FindResource("Ink");
        FontFamily = (FontFamily)Application.Current.FindResource("NxFont");
        FontSize = 12;

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(new TextBlock
        {
            Text = "Geöffnete Anwendung auswählen",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });
        header.Children.Add(new TextBlock
        {
            Text = "Wähle ein geöffnetes Anwendungsfenster zur Live-Erfassung.",
            Style = (Style)Application.Current.FindResource("Hint"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        list = new ListBox
        {
            ItemsSource = windows,
            SelectionMode = SelectionMode.Single,
            Background = (Brush)Application.Current.FindResource("Inset"),
            BorderBrush = (Brush)Application.Current.FindResource("Line"),
            BorderThickness = new Thickness(1)
        };
        list.ItemTemplate = CreateItemTemplate();
        list.MouseDoubleClick += (s, e) =>
        {
            if (list.SelectedItem is Cue cue)
            {
                SelectedWindow = cue;
                DialogResult = true;
            }
        };
        if (windows.Count > 0) list.SelectedIndex = 0;
        Grid.SetRow(list, 1);
        root.Children.Add(list);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var cancelBtn = new Button
        {
            Content = "Abbrechen",
            Style = (Style)Application.Current.FindResource("QuietButton"),
            Padding = new Thickness(14, 5, 14, 5),
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        };
        var okBtn = new Button
        {
            Content = "Hinzufügen",
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            Padding = new Thickness(16, 5, 16, 5),
            IsDefault = true
        };
        okBtn.Click += (s, e) =>
        {
            if (list.SelectedItem is Cue cue)
            {
                SelectedWindow = cue;
                DialogResult = true;
            }
        };
        buttons.Children.Add(cancelBtn);
        buttons.Children.Add(okBtn);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);

        Content = root;
    }

    private static DataTemplate CreateItemTemplate()
    {
        var template = new DataTemplate(typeof(Cue));
        var factory = new FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        factory.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 6, 4, 6));

        var icon = new FrameworkElementFactory(typeof(TextBlock));
        icon.SetValue(TextBlock.TextProperty, "");
        icon.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe MDL2 Assets"));
        icon.SetValue(TextBlock.FontSizeProperty, 16.0);
        icon.SetValue(TextBlock.ForegroundProperty, Application.Current.FindResource("Accent"));
        icon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
        icon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(icon);

        var textPanel = new FrameworkElementFactory(typeof(StackPanel));
        var title = new FrameworkElementFactory(typeof(TextBlock));
        title.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
        title.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        title.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        textPanel.AppendChild(title);

        var cat = new FrameworkElementFactory(typeof(TextBlock));
        cat.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Category"));
        cat.SetValue(TextBlock.StyleProperty, Application.Current.FindResource("Eyebrow"));
        cat.SetValue(TextBlock.FontSizeProperty, 9.0);
        cat.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 0));
        textPanel.AppendChild(cat);

        factory.AppendChild(textPanel);
        template.VisualTree = factory;
        return template;
    }
}
