using System.IO;
using NXPresent;
using Velopack;

/// <summary>
/// The update service and the update settings. Nothing is downloaded or installed here: Velopack itself only works in a copy
/// that Setup installed, so the full update path is proven with a local feed (docs/ENTWICKLUNG.md), not by this program.
/// </summary>
static class UpdateChecks
{
    public static async Task Run(Action<bool, string> check, string folder)
    {
        // The real program does this first thing in Main (Program.cs); without it Velopack has no locator for UpdateManager.
        Velopack.VelopackApp.Build().Run();

        // ---- version numbers ----
        check(UpdateService.TryParseVersion("0.4.1", out var v) && v == SemanticVersion.Parse("0.4.1"), "Update: release version is read");
        check(UpdateService.TryParseVersion("v0.5.0", out v) && v == SemanticVersion.Parse("0.5.0"), "Update: release version with v is read");
        foreach (var bad in new[] { "0.4.1-beta", "0.4.1+build5", "1.2", "latest", "", "0.4.1.2", "v-1.0.0" })
            check(!UpdateService.TryParseVersion(bad, out _), "Update: version \"" + bad + "\" is not offered");
        check(!UpdateService.TryParseVersion(null, out _), "Update: missing version is not offered");
        var stable = SemanticVersion.Parse("0.6.0");
        var nextStable = SemanticVersion.Parse("0.7.0");
        var beta = SemanticVersion.Parse("0.7.0-beta.1");
        var nextBeta = SemanticVersion.Parse("0.7.0-beta.2");
        check(UpdateService.TryParseVersion("v0.7.0-beta.1", out v) && v == beta, "Update: beta version preserves its prerelease suffix");
        check(UpdateService.CanOffer(stable, nextStable) && !UpdateService.CanOffer(stable, beta), "Update: stable only offers newer stable versions");
        check(UpdateService.CanOffer(beta, nextBeta) && !UpdateService.CanOffer(nextBeta, beta) && !UpdateService.CanOffer(beta, nextStable), "Update: beta only offers newer prereleases");
        check(UpdateService.CurrentVersion.IsPrerelease == (UpdateService.UpdateChannel == "beta"), "Update: running version selects its release channel");

        // ---- addresses ----
        check(UpdateService.RepositoryUrl == "https://github.com/nexoGroups/NX-Present" && UpdateService.Repository == "nexoGroups/NX-Present", "Update: the official repository is nexoGroups/NX-Present");
        check(UpdateService.ReleasePageFor(beta).AbsoluteUri == "https://github.com/nexoGroups/NX-Present/releases/tag/v0.7.0-beta.1", "Update: release notes page includes the beta suffix");
        check(UpdateService.SourceOverrideVariable == "NXPRESENT_UPDATE_SOURCE", "Update: the test feed variable has its documented name");

        // ---- a copy that is not installed is never updated ----
        string feed = Path.Combine(folder, "update-feed");
        Directory.CreateDirectory(feed);
        var service = new UpdateService(feed);
        check(service.UsesOverride, "Update: a given feed folder replaces the official release page");
        check(!service.IsInstalled, "Update: a build from the source tree is not an installed copy");
        var result = await service.CheckAsync();
        check(result.State == UpdateState.NotInstalled && result.Offer == null, "Update: a copy that is not installed gets no offer");
        var offerless = new UpdateOffer(beta, null!, UpdateService.ReleasePageFor(beta));
        check(await Thrown(() => service.DownloadAsync(offerless, null, CancellationToken.None)) is InvalidOperationException, "Update: a copy that is not installed downloads nothing");
        check(Thrown(() => Task.Run(() => service.ApplyAfterExit(offerless))).Result is InvalidOperationException, "Update: a copy that is not installed never starts the updater");

        var official = new UpdateService();
        check(official.UsesOverride == !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(UpdateService.SourceOverrideVariable)), "Update: without the test variable the official release page is used");

        // ---- settings ----
        var defaults = new AppSettings();
        check(defaults.CheckUpdatesAtStart && defaults.AutoInstallUpdates && defaults.UpdateAttempt == null, "Update: searching and installing at start are on by default");
        string oldFile = Path.Combine(folder, "settings-before-updates.json");
        File.WriteAllText(oldFile, "{\"Version\":1,\"Volume\":55}");
        var loaded = SettingsStore.Load(oldFile);
        check(loaded.Volume == 55 && loaded.CheckUpdatesAtStart && loaded.AutoInstallUpdates, "Update: settings files from before the updater keep working and get the defaults");
        string newFile = Path.Combine(folder, "settings-with-updates.json");
        SettingsStore.Save(new AppSettings { CheckUpdatesAtStart = false, AutoInstallUpdates = false, UpdateAttempt = "0.4.1" }, newFile);
        var roundtrip = SettingsStore.Load(newFile);
        check(!roundtrip.CheckUpdatesAtStart && !roundtrip.AutoInstallUpdates && roundtrip.UpdateAttempt == "0.4.1", "Update: update preferences and the last attempt survive a restart");
    }

    private static async Task<Exception?> Thrown(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception ex) { return ex; }
    }
}
