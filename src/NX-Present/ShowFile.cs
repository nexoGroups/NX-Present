using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NXPresent;

/// <summary>Version 1: library. Version 2: rundown. Version 3: cue list. Version 4: AUX routing.</summary>
public record ShowDocument(int Version, List<Cue> Cues, List<RunCueData>? Rundown = null, List<Cue>? AuxSources = null);
public static class ShowFile
{
    public static void Save(string path, IEnumerable<Cue> cues, IEnumerable<RunCueData>? rundown = null, IReadOnlyList<Cue>? auxSources = null)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var list = new List<Cue>();
        foreach (var cue in cues)
        {
            if (list.Count >= 10000) throw new InvalidDataException("Die Show enthält zu viele Inhalte.");
            Cue.Validate(cue);
            list.Add(Relative(cue, folder)!);
        }
        string temp = path + ".tmp";
        var steps = new List<RunCueData>(rundown ?? []);
        CueRundown.Validate(steps);
        var relativeSteps = steps.ConvertAll(step => step with { Cue = Relative(step.Cue, folder) });
        AuxRouting.Validate(auxSources);
        var relativeAux = auxSources == null ? null : new List<Cue>(auxSources).ConvertAll(cue => Relative(cue, folder)!);
        File.WriteAllText(temp, JsonSerializer.Serialize(new ShowDocument(relativeAux != null ? 4 : steps.Count > 0 ? 3 : 1, list, relativeSteps, relativeAux), new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, path, true);
    }
    public static List<Cue> Load(string path) => LoadDocument(path).Cues;
    public static ShowDocument LoadDocument(string path)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var document = JsonSerializer.Deserialize<ShowDocument>(File.ReadAllText(path)) ?? throw new InvalidDataException("Ungültige Showdatei.");
        if (document.Version is < 1 or > 4 || document.Cues == null || document.Cues.Count > 10000) throw new InvalidDataException("Unbekannte oder zu große Showdatei.");
        var result = new List<Cue>();
        foreach (var cue in document.Cues)
        {
            Cue.Validate(cue);
            result.Add(Absolute(cue, folder)!);
        }
        var steps = document.Rundown ?? [];
        CueRundown.Validate(steps);
        var absoluteSteps = steps.ConvertAll(step => step with { Cue = Absolute(step.Cue, folder) });
        AuxRouting.Validate(document.AuxSources);
        var absoluteAux = document.AuxSources?.ConvertAll(cue => Absolute(cue, folder)!);
        return new ShowDocument(document.Version, result, absoluteSteps, absoluteAux);
    }

    // Media are linked by a path relative to the show, so a show and its media can move together.
    private static Cue? Relative(Cue? cue, string folder) => cue is { Kind: CueKind.Image or CueKind.Video } ? cue with { Source = Path.GetRelativePath(folder, cue.Source) } : cue;
    private static Cue? Absolute(Cue? cue, string folder) => cue is { Kind: CueKind.Image or CueKind.Video } ? cue with { Source = Path.GetFullPath(cue.Source, folder) } : cue;
}
