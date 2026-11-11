using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace NXPresent;

public enum CueKind { Image, Video, Website, Pattern, Text, Clock, Capture, WindowCapture, Scene, CueShow }
public record Cue(CueKind Kind, string Name, string Source)
{
    /// <summary>The permanent black input of the switcher. It is not part of the library.</summary>
    public static readonly Cue Black = new(CueKind.Pattern, "Schwarz", "blackout");
    /// <summary>The live show runner input of the cue page.</summary>
    public static readonly Cue ShowRunner = new(CueKind.CueShow, "Show-Ablauf (Cue-Mode)", "cue-show-engine");
    [JsonIgnore] public string Glyph => Kind switch { CueKind.Image => "", CueKind.Video => "", CueKind.Website => "", CueKind.Pattern => "", CueKind.Clock => "", CueKind.Capture => "", CueKind.WindowCapture => "", CueKind.Scene => "", CueKind.CueShow => "", _ => "" };
    [JsonIgnore] public string Category => Kind switch { CueKind.Image => "BILD", CueKind.Video => "VIDEO", CueKind.Website => "WEBSEITE", CueKind.Pattern => "TESTBILD", CueKind.Clock => "UHR", CueKind.Capture => "VIDEOEINGANG", CueKind.WindowCapture => "ANWENDUNG", CueKind.Scene => "SZENE", CueKind.CueShow => "CUE-SHOW", _ => "TEXT" };
    [JsonIgnore] public string Detail => Kind is CueKind.Image or CueKind.Video ? Path.GetFileName(Source) : Kind == CueKind.Website ? Source : Category;
    public static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff"];
    public static readonly string[] VideoExtensions = [".mp4", ".mkv", ".mov", ".avi", ".webm", ".m4v", ".wmv", ".mpeg", ".mpg", ".ts"];
    public static Cue FromFile(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        CueKind kind = ImageExtensions.Contains(extension) ? CueKind.Image : VideoExtensions.Contains(extension) ? CueKind.Video : throw new ArgumentException("Dieses Dateiformat wird nicht unterstützt: " + extension);
        return new Cue(kind, Path.GetFileName(path), Path.GetFullPath(path));
    }
    public static bool TryWebsite(string? value, out string address)
    {
        address = "";
        if (string.IsNullOrWhiteSpace(value)) return false;
        value = value.Trim();
        if (!value.Contains("://")) value = "https://" + value;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http") || string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        address = uri.AbsoluteUri;
        return true;
    }
    public static void Validate(Cue? cue)
    {
        if (cue == null || cue.Name == null || cue.Source == null || !Enum.IsDefined(cue.Kind)) throw new InvalidDataException("Ungültiger Inhalt.");
        if (cue.Kind == CueKind.Website && !TryWebsite(cue.Source, out _)) throw new InvalidDataException("Ungültige Webseitenadresse.");
        if (cue.Kind is CueKind.Image or CueKind.Video && string.IsNullOrWhiteSpace(cue.Source)) throw new InvalidDataException("Der Dateipfad fehlt.");
        if (cue.Kind == CueKind.Capture && string.IsNullOrWhiteSpace(cue.Source)) throw new InvalidDataException("Das Aufnahmegerät fehlt.");
        if (cue.Kind == CueKind.WindowCapture && string.IsNullOrWhiteSpace(cue.Source)) throw new InvalidDataException("Das Anwendungsfenster fehlt.");
        if (cue.Kind == CueKind.Scene && string.IsNullOrWhiteSpace(cue.Source)) throw new InvalidDataException("Die Szenen-Konfiguration fehlt.");
        if (cue.Kind == CueKind.CueShow && string.IsNullOrWhiteSpace(cue.Source)) throw new InvalidDataException("Die Cue-Show-Quelle fehlt.");
    }
}
