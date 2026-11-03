# Entwicklung

NX-Present ist eine Windows-Desktop-Anwendung mit C#, WPF, .NET 10, LibVLCSharp und Microsoft WebView2. Die Projektlizenz erlaubt lokale nichtkommerzielle Anpassungen; die Veröffentlichung eigener Programmdateien benötigt eine gesonderte Erlaubnis. GitHub-Forks sind im beschriebenen Rahmen erlaubt.

## Voraussetzungen

- Windows x64
- .NET SDK 10.0.403; `global.json` erlaubt neuere Patches dieses SDK-Featurebands
- Netzwerkzugriff auf NuGet für den ersten Restore
- Optional: das Velopack-Werkzeug `vpk` für Setup und Update-Pakete (`dotnet tool install -g vpk --version 1.2.161`; die Version muss genau zum Paket `Velopack` in `src/NX-Present/NX-Present.csproj` passen, `Build.ps1` und die CI prüfen das)

Visual Studio ist nicht erforderlich. Die Werkzeuge zum Bauen gehören nicht in den Git-Quellbaum. Binärdateien, Browserprofile, Einstellungen und Zugangsdaten werden nicht eingecheckt.

## Bauen und prüfen

```powershell
.\scripts\Build.ps1
.\scripts\Build.ps1 -Media
.\scripts\Build.ps1 -Installer
```

`-Installer` packt mit Velopack nach `artifacts/release`: `NXPresent-win-Setup.exe`, `NXPresent-win-Portable.zip`, das volle Paket `NXPresent-X.Y.Z-full.nupkg` (bei früheren Velopack-Versionen kleine `-delta`-Pakete), die Update-Liste `releases.win.json` und `SHA256SUMS.txt`. Die Versionshinweise stammen aus dem Abschnitt `## X.Y.Z` der `CHANGELOG.md`. `-SkipValidation` lässt die Prüfungen aus (die CI führt sie in einem eigenen Schritt aus).

Alternativ direkt mit dem SDK:

```powershell
dotnet restore src/NX-Present/NX-Present.csproj --locked-mode
dotnet build src/NX-Present/NX-Present.csproj -c Release --no-restore
dotnet run --project tests/NX-Present.Validation/NX-Present.Validation.csproj -c Release
dotnet publish src/NX-Present/NX-Present.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish
```

`artifacts/` ist vollständig aus Git ausgeschlossen. Abhängigkeiten sind in `packages.lock.json` festgehalten. Ein Selbstbau bringt die .NET-Laufzeit mit. Die Bibliotheken werden dynamisch als getrennte Dateien eingebunden und nicht in einer einzelnen EXE versteckt.

## Prüfungen

`tests/NX-Present.Validation` ist ein Konsolenprogramm ohne Testframework. Jede Prüfung schreibt `PASS …` oder bricht mit der ersten Abweichung ab.

Die Standardvalidierung braucht weder Video-Engine noch Browser und umfasst:

- URL- und Dateitypfilter, Showdateien, Einstellungen, Cue-Liste (Nummern, Tasten, Zeitangaben, Speichern, Kompatibilität älterer Shows)
- die Cue-Engine mit künstlicher Uhr: GO und Marke, Pre-Wait, Auto-Continue und Auto-Follow, gleichzeitige Cues, Warteschlange hinter laufenden Übergängen, Stopp, ausgeschaltete Cues, fehlgeschlagene und langsam ladende Quellen, Schwarz-, Lautstärke- und Video-Cues, Vorladen
- Testbilder (sichtbare Bewegung, feste Referenzlinien, Schutz-Schwarzbild) und Uhr
- die Mischer-Logik mit künstlicher Uhr: Vorschau, CUT, AUTO, Überblender, Fade to Black, Tausch von Programm und Vorschau, Audio-Pegel, abgebrochene und fehlgeschlagene Quellen
- den Compositor pixelgenau für jede Übergangsart (Mix, Dip, beide Wischer, Iris, Schieben) und für Schwarz
- Standbilder mit EXIF-Ausrichtung und Größenbegrenzung
- den Update-Dienst ohne Netzwerk und ohne Installation: Versionsnummern, Adressen, das Verhalten einer nicht installierten Kopie (kein Angebot, kein Download, kein Start des Updaters), Einstellungen. Velopacks Prüfung von Paketen und der eigentliche Update-Weg lassen sich nur in einer installierten Kopie ausprobieren (siehe „Veröffentlichen“)
- das Hauptfenster in einem unsichtbaren Fenster außerhalb aller Monitore: Medienpool, Tasten-Reihen, CUT und AUTO in Echtzeit, T-Bar, gesperrte Programm-Zeile, FTB, Mindestgröße, Speichern der Einstellungen. Dabei entstehen Screenshots der Oberfläche im Ordner `runs/` neben dem Testprogramm.

Mit `--media` (oder `Build.ps1 -Media`) kommen die Prüfungen mit der echten Video-Engine und der WebView2-Laufzeit hinzu: ein selbst erzeugtes Testvideo (Bildfarben, Cue, Wiedergabe, Pause, Suchen, Schleife, erneutes Bereitstellen, Freigabe, defekte Datei), eine lokale animierte Webseite (Bilder, Bewegung, Freigabe des Browserfensters, nicht erreichbare Adresse) und ein Mischer mit echten Quellen. `--video <Dateipfad>` ergänzt einen Test mit einer eigenen Videodatei. `--media-only` führt nur die Mischer- und Medienprüfungen aus.

Ändert sich der Besitzer oder Name des Repositorys, müssen `UpdateService.Repository` und die Adresse in `scripts/Build.ps1` mitgezogen werden; die alte Adresse wird bewusst nicht weiter benutzt, weil ein frei gewordener Name von jemand anderem belegt werden könnte.

Der zusätzliche Parameter `--audio` ist ein bewusster Integrationstest, der Windows-Standardgeräte umstellt und wiederherstellt; er darf nicht parallel zu einer laufenden NX-Present-Sitzung ausgeführt werden.

## Aufbau

| Bereich | Aufgabe |
| --- | --- |
| `Mixer/Switcher` | Logik des Mischers: Programm, Vorschau, CUT, AUTO, Überblender, FTB, Audio-Pegel. Kennt keine Oberfläche; die Zeit kommt von außen |
| `Mixer/SourceFeed` und Ableitungen | Eine Quelle liefert Bilder und erzeugt für jeden Monitor eine eigene Ansicht derselben Bilder: `StillFeed`, `VectorFeed`, `VideoFeed`, `WebFeed` |
| `Mixer/VideoFeed` | LibVLC über die Speicherausgabe: jedes Bild landet in einer `WriteableBitmap`. Der Player wird nur über eine serielle Warteschlange bedient, nie vom UI-Thread |
| `Mixer/WebFeed` | Webseite in einem Browserfenster außerhalb aller Monitore; die Bilder kommen per Screencast des Browsers |
| `Mixer/MixerSurface` | Compositor: Programm, einlaufende Quelle, Übergang und Schwarz. Ausgabefenster und Monitore benutzen je einen |
| `Mixer/MonitorView`, `TBar`, `BusKey` | Monitor mit Tally und Wiedergabeleiste, Überblender, Taste für Zeilen und Pool |
| `MainWindow*` | Oberfläche des Mischers, Pool, Einstellungen, Cue-Anbindung und Fensterrahmen |
| `CueRundown` | Die Cue-Liste als Daten: Cues mit Nummer, Zeiten, Art und Weiter-Modus, Marke (`Next`), Live-Cue, Nummern und Tasten. Kennt weder Oberfläche noch Mischer |
| `CueEngine` | Fährt die Liste wie QLab: GO, Pre-Wait, Auto-Continue und Auto-Follow, mehrere Cues zugleich, Warteschlange der Übergänge, Stopp, Vorladen. Die Zeit kommt von außen |
| `CueModeView`, `CueFormat` | Die Cue-Seite (Liste, Inspektor, Monitore) und ihre Helfer: Zeiten lesen und anzeigen, Farben, Tasten, Konverter |
| `DisplaySafety` | Beamer-Fenster (`OutputWindow`), Mausschutz, Anzeigenliste |
| `PatternView` / `ClockView` / `CueVisual` | Testsignale, Uhr, Texttafeln und deren Vorschaubilder |
| `AudioRouting` | Gemeinsames Windows-Standardgerät und Wiederherstellung |
| `SettingsStore` / `ShowFile` | Einstellungen und `.nxshow`-Dateien |
| `Program`, `UpdateService`, `MainWindow.Update` | Velopack-Einstieg (`VelopackApp` zuerst in `Main`), Suche, Download und Start des Updaters; Setup-Karte, Statusleiste und Prüfung beim Start |
| `installer/cache` | Der erzeugte VLC-Plugin-Katalog samt Manifest, den `scripts/Prepare-PluginCache.ps1` vor dem Packen in die Anwendung legt |

### Der Signalweg

Jede Quelle ist ein `SourceFeed`. Alle Monitore und das Ausgabefenster bauen mit `CreateView` eine eigene Ansicht, die dieselbe Bitmap oder Zeichnung zeigt. Es gibt deshalb keine Bildschirmaufnahme, keine Überlagerungsfenster und keine eingefrorene Vorschau: Der Programm-Monitor ist ein zweiter Compositor über denselben Bildern wie die Ausgabe.

`Switcher` hält die Quelle auf Programm und die Vorschau. Ein Übergang arbeitet mit einem Fortschritt von 0 bis 1; `MixerSurface` setzt daraus Deckkraft, Masken oder Verschiebung. Nach dem Abschluss tauschen Programm und Vorschau die Plätze, und die bisherige Quelle wird mit `Arm()` zurückgestellt (ein Video wartet wieder auf seinem ersten Bild).

### Video

`VideoFeed` setzt LibVLC auf die Speicherausgabe (`RV32`). Das Bild wird in der Größe des Videos nach Seitenverhältnis (einschließlich Pixelformat) auf höchstens 1920 × 1080 festgelegt. VLC schreibt in drei native Puffer; der UI-Thread kopiert das jeweils letzte fertige Bild in die `WriteableBitmap`. Ein angehaltenes Bild zeichnet VLC immer wieder neu; es wird nur wenige Male kopiert. Alle Befehle (Abspielen, Anhalten, Suchen, Lautstärke) laufen über eine serielle Warteschlange. Zeit, Länge und Zustand liest die Oberfläche aus zwischengespeicherten Werten. Beim Freigeben werden Player und Puffer erst nach dem letzten Rückruf freigegeben; beim Beenden wartet `VideoFeed.WaitForShutdown`.

### Webseiten

`WebFeed` startet ein `WebView2` in einem Fenster bei Position −12000, −12000. Chromium würde ein solches Fenster als verdeckt behandeln und das Zeichnen einstellen, deshalb setzt `WebHost` die Startparameter `--disable-features=CalculateNativeWinOcclusion`, `--disable-backgrounding-occluded-windows`, `--disable-renderer-backgrounding` und `--disable-background-timer-throttling`. Die Seite wird mit `Emulation.setDeviceMetricsOverride` auf 1920 × 1080 festgelegt, die Bilder liefert `Page.startScreencast` als JPEG. Bleibt der Screencast stumm, nimmt `CapturePreviewAsync` einzelne Bilder auf.

### Updates

`UpdateService` benutzt Velopack mit einer `GithubSource` auf `https://github.com/nexoGroups/NX-Present` (ohne Anmeldung, keine Vorabversionen). Velopack liest aus der neuesten Veröffentlichung die Update-Liste `releases.win.json`, lädt das volle Paket `NXPresent-X.Y.Z-full.nupkg` (oder kleine `-delta`-Pakete) und prüft es gegen Größe und SHA-256 der Liste. `UpdateService` bietet nur an, wenn die Version ein reines `X.Y.Z` ohne Zusatz wie `-beta` ist und größer als die laufende (`UpdateService.CurrentVersion`, aus `<Version>` in der `.csproj`). Die Prüfsumme stammt aus derselben Veröffentlichung; sie erkennt beschädigte Downloads, keine manipulierte Veröffentlichung. Ein signiertes Setup wäre der nächste Schritt, sobald ein Zertifikat vorliegt.

`Program.Main` startet als Erstes `VelopackApp.Build().Run()`: Setup und Updater rufen die EXE mit besonderen Argumenten auf, die dort behandelt werden, bevor WPF startet (`App.xaml` ist dafür in der `.csproj` als `Page` statt `ApplicationDefinition` eingetragen). Updates gibt es nur in einer Kopie, die das Setup installiert hat (`UpdateService.IsInstalled`); ein Selbstbau meldet das und wird nie ersetzt.

Die **PackId `NXPresent`** (`scripts/Build.ps1`) bestimmt den Installationsordner `%LOCALAPPDATA%\NXPresent` (Programm unter `current`, Updater `Update.exe`, Pakete unter `packages`). Sie darf nach der ersten Veröffentlichung nie geändert werden, sonst finden installierte Kopien keine Updates mehr. Sie darf auch nicht mit dem Datenordner `%LOCALAPPDATA%\NX-Present` (`App.DataDirectory`: Einstellungen, Pool, Browserprofile, Protokolle) zusammenfallen: Velopack löscht beim Deinstallieren den ganzen Installationsordner.

Zum Installieren ruft `UpdateService.ApplyAfterExit` `WaitExitThenApplyUpdates` auf. Das startet Velopacks `Update.exe`, das auf das Ende des Programms wartet, die neue Version einspielt und neu startet; das Hauptfenster wird danach normal geschlossen, sodass die Ausgabe geordnet endet. Vor dem Start wird `UpdateAttempt` in den Einstellungen gesetzt; ist die laufende Version danach noch älter, gilt der Versuch als fehlgeschlagen und wird nicht automatisch wiederholt. Ein Protokoll schreibt Velopack nach `%LOCALAPPDATA%\NXPresent\velopack.log`.

Die automatische Installation beim Start gilt nur, solange das Programm schwarz ist und kein Übergang läuft (`UpdateIsSafe`); sie wird direkt vor dem Start des Updaters noch einmal geprüft.

**Ausprobieren vor der Veröffentlichung:** Die Umgebungsvariable `NXPRESENT_UPDATE_SOURCE` richtet die Suche auf einen Ordner oder eine Adresse mit einer Update-Liste statt auf GitHub. Zwei Versionen mit `Build.ps1 -Installer` in denselben Ordner `artifacts/release` packen (zuerst die ältere), die ältere in einen neuen leeren Ordner installieren (`NXPresent-win-Setup.exe --silent --installto <Ordner>`), die Variable setzen, starten und unter Setup → Updates nach der neueren suchen. Danach mit `Update.exe --silent uninstall` aus dem Installationsordner entfernen, den Ordner nicht wiederverwenden und `artifacts/` aufräumen.

## Veröffentlichen

Der Ablauf steht im Projekt-Skill `.claude/skills/release`. Kurz:

1. Version (`<Version>` in `src/NX-Present/NX-Present.csproj`) und Abschnitt `## X.Y.Z – Titel` der `CHANGELOG.md` aktualisieren; `Build.ps1` übernimmt ihn als Versionshinweise.
2. Projekt bauen, Validierung mit `-Media` ausführen und das Setup erzeugen: `.\scripts\Build.ps1 -Installer -Media`.
3. Installation, Start, Ausgabe mit echtem Beamer, CUT, AUTO, Video und Webseite, Schwarzbild, Notausstieg, Update von der vorherigen Version (Setup-Karte und Start-Prüfung, siehe „Ausprobieren vor der Veröffentlichung“) und Deinstallation prüfen.
4. Fremdlizenzen und zugehörige Quellpakete prüfen und beilegen.
5. Ausschließlich im offiziellen Repository veröffentlichen, als Veröffentlichung mit dem Tag `vX.Y.Z`, ohne Haken bei „Vorabversion“: **alle** Dateien aus `artifacts/release` (`NXPresent-win-Setup.exe`, `NXPresent-win-Portable.zip`, `NXPresent-X.Y.Z-full.nupkg`, eventuelle `-delta`-Pakete, `releases.win.json`, `RELEASES-win`, `SHA256SUMS.txt`, die Namen dürfen nicht geändert werden) und die Fremdquellen. Fehlt das volle Paket oder die Update-Liste, finden installierte Kopien die Version nicht.

Das Setup wird hier ohne eigenes Code-Signing-Zertifikat gebaut. Für eine später signierte Veröffentlichung muss der Herausgeber selbst ein passendes Zertifikat bzw. einen Signierdienst bereitstellen; Schlüssel gehören nicht in das Repository.
