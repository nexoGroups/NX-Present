[CmdletBinding()]
param(
    # Pack the release with Velopack: Setup, update packages and update feed in artifacts/release.
    [switch]$Installer,
    # Adds the validation checks that need the real video engine and the WebView2 runtime.
    [switch]$Media,
    # For CI jobs that run the validation on their own.
    [switch]$SkipValidation,
    [string]$DotNet = 'dotnet',
    # Set a separate directory to keep assets from an earlier build out of this release.
    [string]$ReleaseDirectory
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$packId = 'NXPresent'   # Never change: installed copies find their updates through this name.
$mainExe = 'NX-Present.exe'
$repoUrl = 'https://github.com/nexoGroups/NX-Present'
Push-Location $root
try {
    function Invoke-DotNet([string[]]$Arguments) {
        & $DotNet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed (exit $LASTEXITCODE): $Arguments" }
    }
    [xml]$project = Get-Content -LiteralPath 'src/NX-Present/NX-Present.csproj'
    $version = [string]$project.Project.PropertyGroup.Version
    if ($version -cnotmatch '^\d{1,4}\.\d{1,4}\.\d{1,4}(?:-(?:alpha|beta|rc)\.[1-9][0-9]*)?$') {
        throw "Unsupported release version: $version"
    }
    $prerelease = $version.Contains('-')
    $channel = if ($prerelease) { 'beta' } else { 'win' }
    $packageVersion = if ($prerelease) { "$version-$channel" } else { $version }
    if (-not $ReleaseDirectory) {
        $ReleaseDirectory = if ($prerelease) { "artifacts/release-$version" } else { 'artifacts/release' }
    }
    $buildId = Get-Date -Format 'yyyyMMdd-HHmmss'
    $appDir = Join-Path $root "artifacts/publish/$version/$buildId"
    $releaseDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ReleaseDirectory)
    $otherChannel = if ($prerelease) { 'win' } else { 'beta' }
    if ($Installer -and (Test-Path -LiteralPath (Join-Path $releaseDir "releases.$otherChannel.json"))) {
        throw "Choose a separate output directory for channel '$channel'."
    }
    New-Item -ItemType Directory -Path $appDir,$releaseDir -Force | Out-Null
    Invoke-DotNet -Arguments @('restore','src/NX-Present/NX-Present.csproj','--locked-mode')
    Invoke-DotNet -Arguments @('build','src/NX-Present/NX-Present.csproj','-c','Release','--no-restore')
    if (-not $SkipValidation) {
        $validation = @('run','--project','tests/NX-Present.Validation/NX-Present.Validation.csproj','-c','Release')
        if ($Media) { $validation += @('--','--media') }
        Invoke-DotNet -Arguments $validation
    }
    Invoke-DotNet -Arguments @('publish','src/NX-Present/NX-Present.csproj','-c','Release','-r','win-x64','--self-contained','true','-o',$appDir,'-p:DebugType=None','-p:DebugSymbols=false')
    & (Join-Path $PSScriptRoot 'Prepare-PluginCache.ps1') -AppDirectory $appDir
    Copy-Item -LiteralPath 'LICENSE.txt' -Destination (Join-Path $appDir 'LICENSE-NX-Present.txt')
    Copy-Item -LiteralPath 'THIRD-PARTY.md' -Destination $appDir
    Copy-Item -LiteralPath 'licenses' -Destination $appDir -Recurse
    Copy-Item -LiteralPath 'docs/Hilfe.html' -Destination $appDir
    New-Item -ItemType Directory -Path (Join-Path $appDir 'examples') -Force | Out-Null
    Copy-Item -LiteralPath 'examples/Demo.nxshow' -Destination (Join-Path $appDir 'examples')
    if ($Installer) {
        if ((Get-Content -LiteralPath 'LICENSE.txt' -Raw).Contains('{{')) { throw 'Unresolved placeholders in the project license.' }

        # The vpk tool must have exactly the version of the Velopack package in the app project.
        $package = @($project.Project.ItemGroup.PackageReference) | Where-Object { $_.Include -eq 'Velopack' } | Select-Object -First 1
        if (-not $package) { throw 'The Velopack package is missing in NX-Present.csproj.' }
        $vpkLine = & $DotNet tool list -g | Where-Object { $_ -match '^vpk\s+(\S+)' } | Select-Object -First 1
        if (-not $vpkLine) { throw "vpk is not installed: dotnet tool install -g vpk --version $($package.Version)" }
        $vpkVersion = ($vpkLine -split '\s+')[1]
        if ($vpkVersion -ne $package.Version) { throw "vpk $vpkVersion does not match the Velopack package $($package.Version): dotnet tool update -g vpk --version $($package.Version)" }

        # The release notes are the section of this version in CHANGELOG.md (also shown in the update feed).
        $changelog = [IO.File]::ReadAllText((Join-Path $root 'CHANGELOG.md'), [Text.Encoding]::UTF8)
        $section = [regex]::Match($changelog, '(?ms)^## ' + [regex]::Escape($version) + '(?=[ \t\r\n])[^\r\n]*\r?\n(.*?)(?=^## |\z)')
        if (-not $section.Success) { throw "CHANGELOG.md has no section '## $version'." }
        $notes = Join-Path $root 'artifacts/release-notes.md'
        [IO.File]::WriteAllText($notes, $section.Groups[1].Value.Trim() + "`n", (New-Object System.Text.UTF8Encoding($false)))

        # Earlier releases let Velopack build small delta packages. The first Velopack release has none, so failing here is fine.
        $downloadArguments = @('download', 'github', '--repoUrl', $repoUrl, '--channel', $channel, '--outputDir', $releaseDir)
        if ($prerelease) { $downloadArguments += '--pre' }
        & vpk @downloadArguments
        if ($LASTEXITCODE -ne 0) { Write-Host 'No earlier Velopack release found; building full packages only.' }
        $global:LASTEXITCODE = 0
        $older = @(Get-ChildItem -LiteralPath $releaseDir -Filter '*.nupkg' -File)
        Start-Sleep -Seconds 2
        $started = Get-Date

        $packArguments = @(
            'pack',
            '--packId', $packId,
            '--packVersion', $version,
            '--channel', $channel,
            '--runtime', 'win-x64',
            '--packDir', $appDir,
            '--mainExe', $mainExe,
            '--packTitle', 'NX-Present',
            '--packAuthors', 'NX-Present',
            '--icon', (Join-Path $root 'src/NX-Present/Assets/nx-present.ico'),
            '--releaseNotes', $notes,
            '--framework', 'webview2',
            '--outputDir', $releaseDir
        )
        & vpk @packArguments
        if ($LASTEXITCODE -ne 0) { throw "vpk pack failed (exit $LASTEXITCODE)" }

        # Downloaded older packages only served the delta calculation. They are not part of the new release.
        foreach ($file in $older) { if ($file.LastWriteTime -lt $started) { Remove-Item -LiteralPath $file.FullName -Force } }

        $setup = Join-Path $releaseDir "$packId-$channel-Setup.exe"
        $portable = Join-Path $releaseDir "$packId-$channel-Portable.zip"
        $feed = Join-Path $releaseDir "releases.$channel.json"
        foreach ($required in @($setup, $portable, (Join-Path $releaseDir "$packId-$packageVersion-full.nupkg"), $feed)) {
            if (-not (Test-Path -LiteralPath $required)) { throw "vpk did not produce $required" }
        }
        $sums = foreach ($file in @($setup, $portable)) { (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($file) }
        [IO.File]::WriteAllText((Join-Path $releaseDir 'SHA256SUMS.txt'), (($sums -join "`n") + "`n"), (New-Object System.Text.ASCIIEncoding))
        Write-Host "Release files in ${releaseDir}:"
        Get-ChildItem -LiteralPath $releaseDir -File | ForEach-Object { Write-Host ('  {0}  {1:N1} MB' -f $_.Name, ($_.Length / 1MB)) }
    }
    Write-Host "Anwendung: $appDir"
} finally { Pop-Location }
