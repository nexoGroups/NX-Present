[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$ReleaseDir
)
# Prueft die Dateien einer NX-Present-Veroeffentlichung und schreibt SHA256SUMS.txt.
# Installierte Kopien finden eine Version nur, wenn die Update-Liste (releases.win.json) und das volle Paket dabei sind.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
if (-not $ReleaseDir) { $ReleaseDir = Join-Path $root 'artifacts\release' }
if ($Version -notmatch '^\d{1,4}\.\d{1,4}\.\d{1,4}$') { throw "Version '$Version' ist nicht von der Form X.Y.Z." }

[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src\NX-Present\NX-Present.csproj')
$projectVersion = [string]$project.Project.PropertyGroup.Version
if ($projectVersion -ne $Version) { throw "Die csproj hat Version '$projectVersion', verlangt ist '$Version'. Erst die Versionsnummer anheben und neu bauen." }

$packId = 'NXPresent'
$required = @("$packId-win-Setup.exe", "$packId-win-Portable.zip", "$packId-$Version-full.nupkg", 'releases.win.json')
foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $ReleaseDir $name))) { throw "Es fehlt $name in $ReleaseDir. Erst mit scripts\Build.ps1 -Installer bauen." }
}
$feed = Get-Content -LiteralPath (Join-Path $ReleaseDir 'releases.win.json') -Raw
if ($feed -notmatch [regex]::Escape("$packId-$Version-full.nupkg")) { throw "releases.win.json nennt $packId-$Version-full.nupkg nicht." }

$sourcesName = "NX-Present-ThirdParty-Sources-$Version.zip"
$files = @((Join-Path $ReleaseDir "$packId-win-Setup.exe"), (Join-Path $ReleaseDir "$packId-win-Portable.zip"))
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
