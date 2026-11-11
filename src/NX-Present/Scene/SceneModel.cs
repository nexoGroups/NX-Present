using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NXPresent;

public enum SceneItemType
{
    SongInfo,
    Visualizer,
    Clock,
    Countdown,
    WindowCapture,
    CaptureDevice,
    Media,
    Text,
    ColorBox
}

public enum CountdownMode
{
    TargetTime,
    Duration
}

public sealed class SceneItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Ebene";
    public SceneItemType Type { get; set; } = SceneItemType.SongInfo;
    public double X { get; set; } = 100;
    public double Y { get; set; } = 100;
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 200;
    public double Opacity { get; set; } = 1.0;
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; } = false;

    // --- SongInfo Properties ---
    public bool AutoMedia { get; set; } = true;
    public string CustomTitle { get; set; } = "Aktueller Titel";
    public string CustomArtist { get; set; } = "Künstler / Band";
    public string CustomAlbum { get; set; } = "";
    public string? CustomCoverPath { get; set; }
    public bool ShowCover { get; set; } = true;
    public CoverStyle CoverStyle { get; set; } = CoverStyle.Rounded;
    public string SongTextColor { get; set; } = "#FFFFFF";
    public double SongFontSize { get; set; } = 28;

    // --- Visualizer Properties ---
    public VisualizerStyle VizStyle { get; set; } = VisualizerStyle.Bars;
    public VisualizerTheme VizTheme { get; set; } = VisualizerTheme.Neon;
    public int BarCount { get; set; } = 36;
    public double Sensitivity { get; set; } = 1.2;

    // --- Clock Properties ---
    public bool ShowSeconds { get; set; } = false;
    public bool ShowDate { get; set; } = false;
    public string ClockColor { get; set; } = "#FFFFFF";
    public double ClockFontSize { get; set; } = 48;

    // --- Countdown Properties ---
    public CountdownMode CountdownMode { get; set; } = CountdownMode.TargetTime;
    public string TargetTimeText { get; set; } = "20:00";
    public double CountdownDurationSeconds { get; set; } = 900; // 15 Min
    public string CountdownPrefix { get; set; } = "Die Vorstellung beginnt in";
    public bool ShowCountdownPrefix { get; set; } = true;
    public string CountdownExpiredText { get; set; } = "Die Vorstellung beginnt gleich …";
    public string CountdownColor { get; set; } = "#38BDF8";
    public double CountdownFontSize { get; set; } = 64;
    public bool TriggerCueOnZero { get; set; } = true;

    // --- WindowCapture Properties ---
    public string WindowSource { get; set; } = "";

    // --- Media Properties ---
    public string MediaPath { get; set; } = "";

    // --- Text Properties ---
    public string TextContent { get; set; } = "Textzeile";
    public double TextFontSize { get; set; } = 32;
    public string TextColor { get; set; } = "#FFFFFF";
    public bool TextBold { get; set; } = true;

    // --- ColorBox Properties ---
    public string ColorHex { get; set; } = "#151C2C";
    public double CornerRadius { get; set; } = 12;

    public SceneItem Clone()
    {
        var json = JsonSerializer.Serialize(this);
        var clone = JsonSerializer.Deserialize<SceneItem>(json)!;
        clone.Id = Guid.NewGuid().ToString("N");
        clone.Name += " (Kopie)";
        clone.X += 30;
        clone.Y += 30;
        return clone;
    }
}

public sealed class SceneModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Neue Szene";
    public string BackgroundColor { get; set; } = "#0D111A";
    public bool TransparentBackground { get; set; } = false;
    public List<SceneItem> Items { get; set; } = new();

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = false });

    public static SceneModel FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SceneModel>(json) ?? CreatePreShowTemplate();
        }
        catch
        {
            return CreatePreShowTemplate();
        }
    }

    public static SceneModel CreateCinemaCountdownTemplate()
    {
        return new SceneModel
        {
            Name = "Kino Pre-Show (Countdown + Musik + Visualizer)",
            BackgroundColor = "#0C101B",
            Items = new List<SceneItem>
            {
                new SceneItem
                {
                    Name = "Hintergrund-Karte",
                    Type = SceneItemType.ColorBox,
                    X = 80,
                    Y = 60,
                    Width = 1760,
                    Height = 960,
                    ColorHex = "#121829",
                    CornerRadius = 24,
                    Opacity = 0.90
                },
                new SceneItem
                {
                    Name = "Kino Countdown",
                    Type = SceneItemType.Countdown,
                    X = 360,
                    Y = 120,
                    Width = 1200,
                    Height = 240,
                    CountdownMode = CountdownMode.TargetTime,
                    TargetTimeText = "20:00",
                    CountdownPrefix = "Die Vorstellung beginnt in",
                    ShowCountdownPrefix = true,
                    CountdownExpiredText = "Die Vorstellung beginnt gleich …",
                    CountdownColor = "#38BDF8",
                    CountdownFontSize = 76,
                    TriggerCueOnZero = true
                },
                new SceneItem
                {
                    Name = "Song-Anzeige (Now Playing)",
                    Type = SceneItemType.SongInfo,
                    X = 410,
                    Y = 380,
                    Width = 1100,
                    Height = 220,
                    AutoMedia = true,
                    ShowCover = true,
                    CoverStyle = CoverStyle.Vinyl,
                    SongFontSize = 32
                },
                new SceneItem
                {
                    Name = "Frequenz-Balken (Neon)",
                    Type = SceneItemType.Visualizer,
                    X = 140,
                    Y = 680,
                    Width = 1640,
                    Height = 260,
                    VizStyle = VisualizerStyle.Bars,
                    VizTheme = VisualizerTheme.Neon,
                    BarCount = 48,
                    Sensitivity = 1.3
                }
            }
        };
    }

    public static SceneModel CreatePreShowTemplate()
    {
        var model = new SceneModel
        {
            Name = "Pre-Show: Musik, Visualizer & Uhr",
            BackgroundColor = "#0F1420",
            Items = new List<SceneItem>
            {
                // Background dark gradient card
                new SceneItem
                {
                    Name = "Hintergrund-Karte",
                    Type = SceneItemType.ColorBox,
                    X = 80,
                    Y = 80,
                    Width = 1760,
                    Height = 920,
                    ColorHex = "#131929",
                    CornerRadius = 24,
                    Opacity = 0.85
                },
                // Music visualizer bars at the bottom
                new SceneItem
                {
                    Name = "Frequenz-Balken (Neon)",
                    Type = SceneItemType.Visualizer,
                    X = 120,
                    Y = 700,
                    Width = 1680,
                    Height = 220,
                    VizStyle = VisualizerStyle.Bars,
                    VizTheme = VisualizerTheme.Neon,
                    BarCount = 48,
                    Sensitivity = 1.3
                },
                // Now Playing Song Info in the center
                new SceneItem
                {
                    Name = "Song-Anzeige (Now Playing)",
                    Type = SceneItemType.SongInfo,
                    X = 300,
                    Y = 260,
                    Width = 900,
                    Height = 220,
                    AutoMedia = true,
                    ShowCover = true,
                    CoverStyle = CoverStyle.Rounded,
                    SongFontSize = 34
                },
                // Digital Clock in the upper right
                new SceneItem
                {
                    Name = "Uhrzeit & Datum",
                    Type = SceneItemType.Clock,
                    X = 1350,
                    Y = 140,
                    Width = 420,
                    Height = 130,
                    ShowSeconds = false,
                    ShowDate = true,
                    ClockFontSize = 52
                }
            }
        };
        return model;
    }

    public static SceneModel CreateMinimalSongTemplate()
    {
        var model = new SceneModel
        {
            Name = "Minimalistischer Song-Screen (ohne Uhr)",
            BackgroundColor = "#0A0D14",
            Items = new List<SceneItem>
            {
                // Center Vinyl & Cover
                new SceneItem
                {
                    Name = "Vinyl Song-Anzeige",
                    Type = SceneItemType.SongInfo,
                    X = 460,
                    Y = 280,
                    Width = 1000,
                    Height = 260,
                    AutoMedia = true,
                    ShowCover = true,
                    CoverStyle = CoverStyle.Vinyl,
                    SongFontSize = 36
                },
                // Waveform along the bottom
                new SceneItem
                {
                    Name = "Audiowelle (Electric Blue)",
                    Type = SceneItemType.Visualizer,
                    X = 100,
                    Y = 720,
                    Width = 1720,
                    Height = 200,
                    VizStyle = VisualizerStyle.Waveform,
                    VizTheme = VisualizerTheme.ElectricBlue,
                    Sensitivity = 1.5
                }
            }
        };
        return model;
    }

    public static SceneModel CreateLowerThirdTemplate()
    {
        var model = new SceneModel
        {
            Name = "Broadcast Lower-Third (Bauchbinde)",
            TransparentBackground = true,
            BackgroundColor = "#00000000",
            Items = new List<SceneItem>
            {
                // Semi-transparent pill backdrop
                new SceneItem
                {
                    Name = "Bauchbinde Hintergrund",
                    Type = SceneItemType.ColorBox,
                    X = 60,
                    Y = 880,
                    Width = 680,
                    Height = 120,
                    ColorHex = "#E6101624",
                    CornerRadius = 14,
                    Opacity = 0.92
                },
                // Compact Song Info
                new SceneItem
                {
                    Name = "Song Info",
                    Type = SceneItemType.SongInfo,
                    X = 75,
                    Y = 890,
                    Width = 650,
                    Height = 100,
                    AutoMedia = true,
                    ShowCover = true,
                    CoverStyle = CoverStyle.Rounded,
                    SongFontSize = 22
                },
                // Compact spectrum visualizer inside pill
                new SceneItem
                {
                    Name = "Mini Equalizer",
                    Type = SceneItemType.Visualizer,
                    X = 580,
                    Y = 905,
                    Width = 140,
                    Height = 65,
                    VizStyle = VisualizerStyle.Bars,
                    VizTheme = VisualizerTheme.CleanWhite,
                    BarCount = 14,
                    Sensitivity = 1.0
                }
            }
        };
        return model;
    }
}
