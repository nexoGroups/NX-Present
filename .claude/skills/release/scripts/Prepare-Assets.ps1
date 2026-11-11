[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$ReleaseDir
)
# Prueft die Dateien einer NX-Present-Veroeffentlichung und schreibt SHA256SUMS.txt.
# Installierte Kopien brauchen die Update-Liste ihres Kanals und das volle Paket.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
if (-not $ReleaseDir) { $ReleaseDir = Join-Path $root 'artifacts\release' }
if ($Version -cnotmatch '^\d{1,4}\.\d{1,4}\.\d{1,4}(?:-(?:alpha|beta|rc)\.[1-9][0-9]*)?$') {
    throw "Ungueltige Release-Version '$Version'."
}

[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src\NX-Present\NX-Present.csproj')
$projectVersion = [string]$project.Project.PropertyGroup.Version
if ($projectVersion -ne $Version) { throw "Die csproj hat Version '$projectVersion', verlangt ist '$Version'. Erst die Versionsnummer anheben und neu bauen." }

$packId = 'NXPresent'
$prerelease = $Version.Contains('-')
$channel = if ($prerelease) { 'beta' } else { 'win' }
$packageVersion = if ($prerelease) { "$Version-$channel" } else { $Version }
$setupName = "$packId-$channel-Setup.exe"
$portableName = "$packId-$channel-Portable.zip"
$packageName = "$packId-$packageVersion-full.nupkg"
$feedName = "releases.$channel.json"
$required = @($setupName, $portableName, $packageName, $feedName)
foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $ReleaseDir $name))) { throw "Es fehlt $name in $ReleaseDir. Erst mit scripts\Build.ps1 -Installer bauen." }
}
$feed = Get-Content -LiteralPath (Join-Path $ReleaseDir $feedName) -Raw | ConvertFrom-Json
$full = $feed.Assets | Where-Object { $_.FileName -ceq $packageName -and $_.Version -ceq $Version }
if (-not $full) { throw "$feedName nennt $packageName nicht." }
if ($prerelease -and (Test-Path -LiteralPath (Join-Path $ReleaseDir 'releases.win.json'))) {
    throw "Im Beta-Ordner liegt eine Stable-Update-Liste."
}

$sourcesName = "NX-Present-ThirdParty-Sources-$Version.zip"
$files = @((Join-Path $ReleaseDir $setupName), (Join-Path $ReleaseDir $portableName))
$sources = Join-Path $ReleaseDir $sourcesName
if (Test-Path -LiteralPath $sources) { $files += $sources }
else { Write-Warning "$sourcesName fehlt. Jede Veroeffentlichung braucht das Quellarchiv der Fremdkomponenten (Get-ThirdPartySources.ps1 und Compress-Archive)." }

$lines = foreach ($file in $files) {
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($file))"
}
$sumsPath = Join-Path $ReleaseDir 'SHA256SUMS.txt'
[IO.File]::WriteAllText($sumsPath, (($lines -join "`n") + "`n"), (New-Object System.Text.ASCIIEncoding))

$notes = Join-Path $root 'artifacts\release-notes.md'
if (-not (Test-Path -LiteralPath $notes)) { Write-Warning "artifacts\release-notes.md fehlt (schreibt scripts\Build.ps1 -Installer aus der CHANGELOG.md)." }

Write-Host "Veroeffentlichung $Version in $ReleaseDir"
foreach ($file in (Get-ChildItem -LiteralPath $ReleaseDir -File)) { Write-Host ('  {0}  {1:N1} MB' -f $file.Name, ($file.Length / 1MB)) }
Get-Content -LiteralPath $sumsPath | ForEach-Object { Write-Host "  $_" }
Write-Host "Geschrieben: $sumsPath"
