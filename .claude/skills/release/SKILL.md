---
name: release
description: Bringt eine neue NX-Present-Version heraus. Überarbeitet README und Doku, schreibt den Changelog-Abschnitt, hebt die Versionsnummer an, packt Setup, Update-Pakete und Update-Liste mit Velopack und veröffentlicht eine GitHub-Veröffentlichung, damit sich installierte Kopien beim Start selbst aktualisieren. Unbedingt verwenden, wenn es bei NX-Present um Release, neue Version, veröffentlichen, rausbringen, Update ausrollen, Changelog schreiben, README überarbeiten oder Installer neu bauen geht, auch wenn nur ein Teil davon gemeint ist.
---

# NX-Present veröffentlichen

Eine Veröffentlichung auf GitHub ist hier mehr als ein Download: NX-Present sucht beim Start nach der neuesten Veröffentlichung (Velopack liest deren Update-Liste `releases.win.json`) und installiert sie bei allen, die die Funktion eingeschaltet haben, von selbst und ohne Rückfrage, solange nichts live ist. Wer das Update schon hat, bekommt es nicht zurückgenommen. Deshalb sind die Schritte 1 bis 5 lokal und gefahrlos, ab Schritt 6 geht etwas nach außen, und der letzte Schritt braucht die ausdrückliche Zustimmung des Nutzers in diesem Gespräch. Einen Tag-Workflow, der von allein veröffentlicht, gibt es mit Absicht nicht; die CI (`.github/workflows/ci.yml`) baut und packt nur zur Kontrolle.

Wünscht der Nutzer nur einen Teil (nur README, nur Changelog, nur Setup bauen), erledige genau diesen Teil und höre dort auf.

## 0. Ausgangslage klären

- Git, .NET und `vpk` liegen auf dem PC des Nutzers oft nicht im PATH der Shell. Suche sie, bevor du sie für fehlend hältst: `C:\Program Files\Git\cmd\git.exe`, `C:\Program Files\dotnet\dotnet.exe`, `%USERPROFILE%\.dotnet\tools\vpk.exe`. Was wirklich fehlt, darfst du nur nach ausdrücklichem Ja des Nutzers installieren (`winget install --id Git.Git`, `winget install --id GitHub.cli`, `dotnet tool install -g vpk --version <Version des Pakets Velopack in NX-Present.csproj>`).
- `vpk` muss genau die Version des NuGet-Pakets `Velopack` in `src/NX-Present/NX-Present.csproj` haben; `scripts/Build.ps1` und die CI prüfen das.
- `git status`, Branch und `git log <letzter Tag>..HEAD`: Was ist seit der letzten Veröffentlichung passiert? Das ist die Grundlage für den Changelog.
- Im Ordner können andere Claude-Sitzungen arbeiten (die Sitzungsliste zeigt laufende Sitzungen mit demselben Ordner). Fremde, halbfertige Änderungen gehören nicht in dein Release: Prüfe vor dem Committen, dass es kompiliert und alle Prüfungen bestehen, und committe nicht blind alles.
- Das Repository steht in `UpdateService.Repository` (`src/NX-Present/UpdateService.cs`) und in `scripts/Build.ps1`. Nimm es von dort, nicht aus dem Gedächtnis; es ist schon einmal umgezogen. Links in der Doku müssen zu diesem Wert passen.
- **PackId `NXPresent`** (`scripts/Build.ps1`) darf nie geändert werden, sobald eine Velopack-Veröffentlichung existiert: Installierte Kopien finden Updates über diesen Namen.

## 1. Version festlegen

Frage den Nutzer nur, wenn es nicht klar ist. Neue Funktionen heben die mittlere Zahl an, reine Korrekturen die letzte. Der Updater bietet nur Versionen der Form `X.Y.Z` an, die größer sind als die laufende, also kein `-beta` im Tag.

Die Nummer steht nur an einer Stelle: `<Version>` in `src/NX-Present/NX-Present.csproj`. Daraus bauen `Build.ps1` und die Pakete. Suche mit `Select-String` nach der alten Nummer, um Treffer in README, Changelog und `THIRD-PARTY.md` zu prüfen. Ersetze nicht blind: Aussagen wie „ab 0.4.0 aktualisiert sich NX-Present selbst“ nennen eine feste Version.

## 2. Doku überarbeiten

Alles auf Deutsch, im Stil der vorhandenen Texte. Lies die Dateien vor dem Ändern, denn sie sind oft nach dem letzten Release weitergewachsen.

- **CHANGELOG.md**: Abschnitt der neuen Version in der Form der vorhandenen (`## X.Y.Z – Kurztitel`), neue Funktionen, Änderungen, behobene Fehler, Hinweise. Der Abschnitt wird von `Build.ps1` als Versionshinweise in die Update-Liste übernommen, die Überschrift muss also mit `## X.Y.Z` beginnen. Schreibe aus Sicht der Nutzer, nicht als Commit-Liste. Einen Fehler nennst du nur „behoben“, wenn du die Ursache im Code gesehen oder die Korrektur geprüft hast. Offene Grenzen gehören in „Hinweise“, auch was nicht an echter Hardware geprüft wurde.
- **README.md**: Funktionen, Tabellen, Tastenkürzel, Installation und Systemangaben gegen den Code abgleichen. Der Kasten „Stand dieser Beschreibung“ am Anfang gilt nur, solange das Setup der neuen Version noch fehlt; nach der Veröffentlichung entfällt er. Keine Zusagen, die nicht belegt sind (zum Beispiel konstante 60 FPS).
- **docs/BEDIENUNG.md, docs/Hilfe.html, docs/ENTWICKLUNG.md**: bei geänderter Bedienung oder Technik anpassen. `Hilfe.html` kommt mit dem Setup zu den Nutzern.
- **THIRD-PARTY.md** und `licenses/`, falls Abhängigkeiten gewechselt haben (auch die Version von Velopack).
- **docs/RELEASE-PRUEFUNG.md**: Nach dem Bauen mit dem füllen, was wirklich geprüft wurde, und mit ausdrücklichen Zeilen für alles, was nicht geprüft wurde (echter Beamer, Update von der Vorversion, Smart App Control, fehlende Signatur).

## 3. Bauen und prüfen

```powershell
.\scripts\Build.ps1 -Installer -Media -DotNet 'C:\Program Files\dotnet\dotnet.exe'
```

Das stellt Pakete her, führt die Prüfungen mit echtem Video und Browser aus, veröffentlicht die Anwendung und packt mit Velopack nach `artifacts/release`: `NXPresent-win-Setup.exe`, `NXPresent-win-Portable.zip`, `NXPresent-X.Y.Z-full.nupkg` (bei früheren Velopack-Versionen auch `-delta`-Pakete), `releases.win.json`, `SHA256SUMS.txt`. Die Versionshinweise landen in `artifacts/release-notes.md`. Starte es im Hintergrund mit Ausgabe in eine Datei und leite die Ausgabe nicht in einen anderen Befehl: Die langlebigen Browser- und VLC-Kindprozesse lassen eine Pipe sonst hängen. Das Skript braucht Netz (NuGet, frühere Veröffentlichungen für Delta-Pakete).

Bricht eine Prüfung ab, ist das ein Befund: Ursache suchen, nicht wiederholen, bis es grün wird. Verweigert Windows das Ausführen eines frisch gebauten Programms oder von `vpk` (Smart App Control, Fehler 0x800711C7, „Eine Anwendungssteuerungsrichtlinie hat diese Datei blockiert“), melde das dem Nutzer und höre auf. Umgehen oder abschalten darfst du es nicht; das ist seine Entscheidung. Die CI-Aufgabe `pack` kann das Packen dann stellvertretend auf einem GitHub-Rechner prüfen.

**Das Update beweisen** (siehe docs/ENTWICKLUNG.md, „Ausprobieren vor der Veröffentlichung“): zwei Versionen in einen eigenen Feed-Ordner packen, die ältere in einen neuen Ordner installieren, mit `NXPRESENT_UPDATE_SOURCE=<Feed-Ordner>` starten und prüfen, dass Setup → Updates die neuere findet und einspielt. Das Setup einer Veröffentlichung kannst du nur gefahrlos starten, wenn du einen frischen Ordner mit `--installto` benutzt; ein Setup ohne diesen Schalter würde die Kopie des Nutzers (`%LOCALAPPDATA%\NXPresent`) ersetzen. Räume alles wieder weg (Installationsordner mit `Update.exe --silent uninstall`, Verknüpfungen, Eintrag unter Apps, `artifacts/`). Kannst du das nicht selbst fahren, schreibe es als nicht geprüft in die Release-Prüfung und bitte den Nutzer, das Setup einmal über die Vorversion zu probieren (Start, Setup → Updates zeigt die neue Version, „Nach Updates suchen“ meldet „aktuell“).

## 4. Commit und Push

Eigene Dateien gezielt hinzufügen, Commit-Text englisch im Stil der Historie (`git log --oneline`). Gepusht wird auf `main`, aber erst nach dem Ja des Nutzers. Wird ein Push oder das Umstellen der Remote-Adresse von der Berechtigungsprüfung verweigert, frage den Nutzer, statt einen Umweg zu suchen. Die Authentifizierung bei GitHub macht der Nutzer selbst; Zugangsdaten oder Tokens fragst du nie ab und gibst sie nie ein.

Der Release-Tag zeigt später auf den Commit, den du als `--target` angibst. Er muss also vorher gepusht sein, und die CI auf diesem Commit sollte grün sein.

## 5. Dateien für die Veröffentlichung

Installierte Kopien finden eine Veröffentlichung nur, wenn die Update-Liste und die Pakete dabei sind. Lade deshalb **alle** Dateien aus `artifacts/release` hoch (Namen nicht ändern):

- `NXPresent-win-Setup.exe` und `NXPresent-win-Portable.zip`
- `NXPresent-X.Y.Z-full.nupkg` und eventuelle `NXPresent-X.Y.Z-delta.nupkg`
- `releases.win.json` (und die weiteren Listen, die `vpk` daneben schreibt, zum Beispiel `RELEASES`)
- `SHA256SUMS.txt`

Dazu gehört bei jedem Release das Quellarchiv der Fremdkomponenten `NX-Present-ThirdParty-Sources-X.Y.Z.zip` (LGPL: wer Binärdateien weitergibt, bietet die Quellen an). Erzeuge es mit `scripts/Get-ThirdPartySources.ps1` und `Compress-Archive` aus `artifacts/third-party-sources` (rund 600 MB, dauert). Ist der Satz unverändert, genügt die Datei der Vorversion unter neuem Namen. Lege das Archiv nach `artifacts/release`, dann nimmt Schritt 6 es mit.

`scripts/Prepare-Assets.ps1` in diesem Skill prüft, dass alle Pflichtdateien da sind, schreibt `SHA256SUMS.txt` mit Setup, tragbarer Fassung und Quellarchiv neu und zeigt Größen und Prüfsummen an:

```powershell
.\.claude\skills\release\scripts\Prepare-Assets.ps1 -Version X.Y.Z
```

Schreibe die Versionshinweise für GitHub nach `artifacts/github-release-notes.md`: der Inhalt von `artifacts/release-notes.md`, am Ende der Satz, dass das Setup nicht signiert ist und die Prüfsumme in `SHA256SUMS.txt` steht.

## 6. Entwurf auf GitHub anlegen

Brauchst du `gh` und es fehlt, frage nach der Installation (`winget install --id GitHub.cli`). Der Nutzer meldet sich selbst an (`gh auth login`, Browser). Prüfe mit `gh auth status`.

Lege die Veröffentlichung zuerst als **Entwurf** an. Ein Entwurf wird von der Update-Suche nicht gesehen, kein installiertes NX-Present findet ihn, und die Dateien lassen sich vorher kontrollieren:

```powershell
$files = @(Get-ChildItem artifacts/release -File | ForEach-Object FullName)   # enthält auch das Quellarchiv, wenn es in artifacts/release liegt
gh release create vX.Y.Z --repo <UpdateService.Repository> --draft --target <Commit-SHA> --title "NX-Present X.Y.Z" --notes-file artifacts/github-release-notes.md @files
```

Das Hochladen dauert bei 700 MB: im Hintergrund starten, Ausgabe in eine Datei. Prüfe danach mit `gh release view vX.Y.Z --json isDraft,isPrerelease,assets,url`, dass alle Dateien da sind (vor allem `releases.win.json` und das volle Paket), und vergleiche die Größe der Setup-Datei mit der lokalen.

## 7. Zustimmung einholen, dann veröffentlichen

Bevor du veröffentlichst, zeige dem Nutzer: Tag, Titel, die Dateien mit Größe und SHA-256, die Versionshinweise und den Link zum Entwurf, und sage klar, dass danach alle Installationen mit Startsuche das Update selbst einspielen. Frage ausdrücklich nach. Eine frühere Zustimmung (zum Pushen, zum Bauen) gilt dafür nicht.

Erst nach einem klaren Ja:

```powershell
gh release edit vX.Y.Z --repo <UpdateService.Repository> --draft=false --latest
```

Keine Vorabversion (`--prerelease` nie setzen), sonst wird sie nicht ausgeliefert.

## 8. Danach prüfen und berichten

- `gh release view vX.Y.Z` zeigt `isDraft: false`; die Tag-Adresse `releases/tag/vX.Y.Z` existiert.
- Die Update-Liste ist öffentlich lesbar und nennt die neue Version: `releases.win.json` aus der Veröffentlichung herunterladen (`gh release download vX.Y.Z --pattern releases.win.json --dir <Temp-Ordner>`) und prüfen, dass das volle Paket `NXPresent-X.Y.Z-full.nupkg` darin steht.
- Nach der zweiten Velopack-Veröffentlichung: das Setup der vorherigen laden (`gh release download`), in einen frischen Ordner installieren (`--installto`), starten und prüfen, dass es die neue Veröffentlichung auf GitHub findet und einspielt; danach alles wieder entfernen.
- Entferne den Kasten „Stand dieser Beschreibung“ aus der README und committe das (wieder erst nach Ja zum Push).
- Berichte ehrlich, was geprüft ist (Build, Prüfungen, Dateien, Update-Liste) und was nicht (Update auf einem echten PC, Verhalten unter Smart App Control, Beamer).

## Zurücknehmen

War die Veröffentlichung fehlerhaft: `gh release edit vX.Y.Z --draft=true` nimmt sie aus der Auslieferung, bevor weitere Installationen sie holen. Wer das Update schon hat, bekommt es nicht zurück; der Updater bietet nie ein Downgrade an. Die Abhilfe ist eine korrigierte, höhere Version.

## Was dieser Skill nicht tut

Er entfernt keine Windows-Schutzfunktionen, signiert nichts mit selbst erzeugten Zertifikaten, speichert keine Zugangsdaten, erzwingt keine Pushes und löscht keine fremden Veröffentlichungen. Stößt etwas an eine Sperre, bleibt es beim Bericht an den Nutzer.
