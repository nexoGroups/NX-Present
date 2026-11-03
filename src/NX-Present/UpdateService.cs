using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace NXPresent;

public enum UpdateState { UpToDate, Available, Failed, NotInstalled }

/// <summary>A newer release that Velopack found in the update feed of the official GitHub release page.</summary>
public sealed record UpdateOffer(Version Version, UpdateInfo Info, Uri ReleasePage);

public sealed record UpdateCheck(UpdateState State, string Message, UpdateOffer? Offer = null);

/// <summary>
/// Finds, downloads and installs new releases from GitHub through Velopack. Updating only works in a copy that was installed
/// with the Setup of a release; a build started from the source tree reports <see cref="IsInstalled"/> as false and is never replaced.
/// Velopack checks every downloaded package against the size and SHA-256 hash of the feed that belongs to the same release.
/// </summary>
public sealed class UpdateService
{
    public const string Repository = "nexoGroups/NX-Present";
    public const string RepositoryUrl = "https://github.com/" + Repository;

    /// <summary>Points the updater at another feed (a folder or an address) to try out a release before it is published.</summary>
    public const string SourceOverrideVariable = "NXPRESENT_UPDATE_SOURCE";

    private static readonly Regex VersionText = new(@"^[vV]?(\d{1,4})\.(\d{1,4})\.(\d{1,4})$", RegexOptions.CultureInvariant);

    private readonly UpdateManager manager;

    public static Version CurrentVersion { get; } = Normalize(typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0));

    /// <param name="sourceOverride">A feed folder or address. When null, the environment variable <see cref="SourceOverrideVariable"/> decides, and without it the official release page is used.</param>
    public UpdateService(string? sourceOverride = null)
    {
        string? source = string.IsNullOrWhiteSpace(sourceOverride) ? Environment.GetEnvironmentVariable(SourceOverrideVariable) : sourceOverride;
        UsesOverride = !string.IsNullOrWhiteSpace(source);
        manager = UsesOverride
            ? new UpdateManager(source!.Trim())
            : new UpdateManager(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
    }

    /// <summary>True when the feed does not come from the official release page.</summary>
    public bool UsesOverride { get; }

    /// <summary>Only copies that Setup installed can be updated. A build from source has no Velopack install around it.</summary>
    public bool IsInstalled => manager.IsInstalled;

    public static Version Normalize(Version version) => new(version.Major, version.Minor, Math.Max(version.Build, 0));

    /// <summary>Accepts plain release versions such as 0.4.1 or v0.4.1. Anything with a suffix (-beta, +build) is not offered.</summary>
    public static bool TryParseVersion(string? text, out Version version)
    {
        version = new Version(0, 0, 0);
        if (text == null) return false;
        var match = VersionText.Match(text.Trim());
        if (!match.Success) return false;
        version = new Version(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
        return true;
    }

    public static Uri ReleasePageFor(Version version) => new(RepositoryUrl + "/releases/tag/v" + version.ToString(3));

    public async Task<UpdateCheck> CheckAsync(Version current, CancellationToken cancellation = default)
    {
        if (!IsInstalled)
            return new(UpdateState.NotInstalled, "Diese Kopie wurde nicht über das Setup installiert und lässt sich nicht selbst ersetzen. Lade das Setup von der Veröffentlichungsseite.");
        try
        {
            var info = await manager.CheckForUpdatesAsync().WaitAsync(cancellation).ConfigureAwait(false);
            if (info == null) return new(UpdateState.UpToDate, "NX-Present " + current.ToString(3) + " ist aktuell.");
            string text = info.TargetFullRelease.Version.ToString();
            if (!TryParseVersion(text, out var version)) return new(UpdateState.Failed, "Die neueste Veröffentlichung hat keine lesbare Versionsnummer (" + text + ").");
            if (version <= Normalize(current)) return new(UpdateState.UpToDate, "NX-Present " + current.ToString(3) + " ist aktuell.");
            return new(UpdateState.Available, "Version " + version.ToString(3) + " ist verfügbar.", new UpdateOffer(version, info, ReleasePageFor(version)));
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { return new(UpdateState.Failed, "GitHub antwortet nicht (Zeitüberschreitung)."); }
        catch (HttpRequestException ex) { return new(UpdateState.Failed, "Keine Verbindung zu GitHub: " + ex.Message); }
        catch (Exception ex) when (ex is not OperationCanceledException) { return new(UpdateState.Failed, "Die Suche nach Updates ist fehlgeschlagen: " + ex.Message); }
    }

    /// <summary>Downloads the package of the offered version and checks it. Progress runs from 0 to 1.</summary>
    public Task DownloadAsync(UpdateOffer offer, IProgress<double>? progress, CancellationToken cancellation)
    {
        if (!IsInstalled) throw new InvalidOperationException("Diese Kopie wurde nicht über das Setup installiert.");
        Action<int>? report = progress == null ? null : percent => progress.Report(Math.Clamp(percent, 0, 100) / 100.0);
        return manager.DownloadUpdatesAsync(offer.Info, report, cancelToken: cancellation);
    }

    /// <summary>
    /// Hands over to Velopack's Update.exe: it waits until this process has ended, installs the downloaded version and starts it.
    /// This call does not end the process. The caller closes the window normally right afterwards, so the show shuts down in an orderly way.
    /// </summary>
    public void ApplyAfterExit(UpdateOffer offer)
    {
        if (!IsInstalled) throw new InvalidOperationException("Diese Kopie wurde nicht über das Setup installiert.");
        manager.WaitExitThenApplyUpdates(offer.Info.TargetFullRelease, silent: true, restart: true);
    }
}
