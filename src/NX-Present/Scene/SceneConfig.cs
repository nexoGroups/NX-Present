using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NXPresent;

public enum VisualizerStyle { Bars, Waveform, Circle, Particles }
public enum VisualizerTheme { Neon, ElectricBlue, AmberFire, Rainbow, CleanWhite, Emerald }
public enum VisualizerPosition { Bottom, Center, AroundCover, Fullscreen }

public enum SongPosition { FullscreenCenter, LowerThirdLeft, LowerThirdRight, TopLeft, TopRight, BottomCenter }
public enum CoverStyle { Square, Rounded, Vinyl }

public enum ClockPosition { TopRight, TopLeft, BelowSong, Center, BottomRight }

public enum SceneBackgroundStyle { SolidDark, Gradient, BlurredCover, Transparent }

public sealed class SongWidgetConfig
{
    public bool Enabled { get; set; } = true;
    public bool AutoWindowsMedia { get; set; } = true;
    public string CustomTitle { get; set; } = "Aktueller Titel";
    public string CustomArtist { get; set; } = "Künstler / Band";
    public string CustomAlbum { get; set; } = "Album";
    public string? CustomCoverPath { get; set; }
    public bool ShowCover { get; set; } = true;
    public CoverStyle CoverStyle { get; set; } = CoverStyle.Rounded;
    public SongPosition Position { get; set; } = SongPosition.FullscreenCenter;
    public double Scale { get; set; } = 1.0;
    public string TextColor { get; set; } = "#FFFFFF";
}

public sealed class ClockWidgetConfig
{
    public bool Enabled { get; set; } = true;
    public bool ShowSeconds { get; set; } = false;
    public bool ShowDate { get; set; } = false;
    public ClockPosition Position { get; set; } = ClockPosition.TopRight;
    public double Scale { get; set; } = 1.0;
    public string Color { get; set; } = "#FFFFFF";
}

public sealed class VisualizerWidgetConfig
{
    public bool Enabled { get; set; } = true;
    public VisualizerStyle Style { get; set; } = VisualizerStyle.Bars;
    public VisualizerTheme Theme { get; set; } = VisualizerTheme.Neon;
    public VisualizerPosition Position { get; set; } = VisualizerPosition.Bottom;
    public int BarCount { get; set; } = 36;
    public double Sensitivity { get; set; } = 1.2;
    public double Height { get; set; } = 180;
}

public sealed class SceneConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Musik & Visualizer";
    public SceneBackgroundStyle BackgroundStyle { get; set; } = SceneBackgroundStyle.Gradient;
    public string BackgroundColor1 { get; set; } = "#0D111A";
    public string BackgroundColor2 { get; set; } = "#1B2234";

    public SongWidgetConfig Song { get; set; } = new();
    public ClockWidgetConfig Clock { get; set; } = new();
    public VisualizerWidgetConfig Visualizer { get; set; } = new();

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = false });

    public static SceneConfig FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SceneConfig>(json) ?? CreatePreShowTemplate();
        }
        catch
        {
            return CreatePreShowTemplate();
        }
    }

    public static SceneConfig CreatePreShowTemplate() => new()
    {
        Name = "Vollbild Pre-Show mit Uhrzeit",
        BackgroundStyle = SceneBackgroundStyle.Gradient,
        BackgroundColor1 = "#0F1420",
        BackgroundColor2 = "#182238",
        Song = new SongWidgetConfig
        {
            Enabled = true,
            AutoWindowsMedia = true,
            Position = SongPosition.FullscreenCenter,
            Scale = 1.1,
            CoverStyle = CoverStyle.Rounded
        },
        Clock = new ClockWidgetConfig
        {
            Enabled = true,
            ShowSeconds = false,
            ShowDate = true,
            Position = ClockPosition.TopRight,
            Scale = 1.0
        },
        Visualizer = new VisualizerWidgetConfig
        {
            Enabled = true,
            Style = VisualizerStyle.Bars,
            Theme = VisualizerTheme.Neon,
            Position = VisualizerPosition.Bottom,
            BarCount = 36,
            Height = 180
        }
    };

    public static SceneConfig CreateMinimalSongTemplate() => new()
    {
        Name = "Minimalistischer Song-Screen (ohne Uhr)",
        BackgroundStyle = SceneBackgroundStyle.BlurredCover,
        BackgroundColor1 = "#101018",
        BackgroundColor2 = "#181824",
        Song = new SongWidgetConfig
        {
            Enabled = true,
            AutoWindowsMedia = true,
            Position = SongPosition.FullscreenCenter,
            Scale = 1.2,
            CoverStyle = CoverStyle.Rounded
        },
        Clock = new ClockWidgetConfig
        {
            Enabled = false
        },
        Visualizer = new VisualizerWidgetConfig
        {
            Enabled = true,
            Style = VisualizerStyle.Waveform,
            Theme = VisualizerTheme.ElectricBlue,
            Position = VisualizerPosition.Bottom,
            Height = 140
        }
    };

    public static SceneConfig CreateLowerThirdTemplate() => new()
    {
        Name = "Broadcast Lower-Third (Bauchbinde)",
        BackgroundStyle = SceneBackgroundStyle.Transparent,
        Song = new SongWidgetConfig
        {
            Enabled = true,
            AutoWindowsMedia = true,
            Position = SongPosition.LowerThirdLeft,
            Scale = 0.9,
            CoverStyle = CoverStyle.Rounded
        },
        Clock = new ClockWidgetConfig
        {
            Enabled = false
        },
        Visualizer = new VisualizerWidgetConfig
        {
            Enabled = true,
            Style = VisualizerStyle.Bars,
            Theme = VisualizerTheme.CleanWhite,
            Position = VisualizerPosition.Bottom,
            BarCount = 28,
            Height = 60
        }
    };

    public static SceneConfig CreateVinylCircleTemplate() => new()
    {
        Name = "Vinyl & Kreis-Visualizer",
        BackgroundStyle = SceneBackgroundStyle.SolidDark,
        BackgroundColor1 = "#0A0D14",
        Song = new SongWidgetConfig
        {
            Enabled = true,
            AutoWindowsMedia = true,
            Position = SongPosition.FullscreenCenter,
            CoverStyle = CoverStyle.Vinyl,
            Scale = 1.0
        },
        Clock = new ClockWidgetConfig
        {
            Enabled = true,
            Position = ClockPosition.TopRight,
            ShowSeconds = true
        },
        Visualizer = new VisualizerWidgetConfig
        {
            Enabled = true,
            Style = VisualizerStyle.Circle,
            Theme = VisualizerTheme.AmberFire,
            Position = VisualizerPosition.AroundCover,
            BarCount = 48,
            Height = 220
        }
    };
}
