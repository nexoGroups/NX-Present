using System.Collections.Generic;
using System.IO;

namespace NXPresent;

internal static class AuxRouting
{
    public const int ChannelCount = 10;

    public static void Validate(IReadOnlyList<Cue>? sources)
    {
        if (sources == null) return;
        if (sources.Count > ChannelCount) throw new InvalidDataException("Die Show enthält mehr als zehn AUX-Kanäle.");
        foreach (var source in sources) Cue.Validate(source);
    }
}
