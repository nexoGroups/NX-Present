# Releaseprüfung

## 0.5.1

- Release-Build ohne Warnungen oder Fehler. Alle 373 Prüfungen der Standardsuite bestanden.
- Die Quellenzeilen wurden bei 1180 × 740 geprüft. Der Abstand zwischen Programm-Label und Schloss beträgt 8 DIPs. Zwischen Schloss und erster Quellentaste liegen 12 DIPs. Die erste Vorschautaste steht bündig darunter, mit 8 DIPs zwischen den Zeilen.
- Außerhalb der Sandbox bestanden die WebView2-Tests. Die erweiterte Mediensuite bricht bei der Prüfung ab, ob ein für die Vorschau gewählter Clip auf seinem ersten Bild wartet. Bei 0.5.0 scheiterte auch das erneute Vorladen eines laufenden Clips im unveränderten Ausgangsstand.

## 0.5.0

- Release-Build ohne Warnungen oder Fehler. Alle 371 Prüfungen der Standardsuite bestanden.
- Bildmischer, Cue Mode und Setup anhand von gerenderten Fenstern geprüft. Bei 1180 × 740 bleiben die Monitore und die Übergangsleiste auch mit geöffneten Optionen bedienbar.
- Die erweiterte Medienprüfung bricht beim erneuten Vorladen eines laufenden Videos ab. Derselbe Test scheitert im unveränderten Ausgangsstand `1d1adcc`.
- Ein Installationswechsel von 0.3.1, ein Update über GitHub, Smart App Control und die Ausgabe auf einem Beamer wurden nicht geprüft.

## 0.4.0

Stand: 31.10.2026, gebaut mit `scripts/Build.ps1 -Installer -Media`.

## Geprüft

- Build: erfolgreich, 0 Warnungen und 0 Fehler.
- Validierung: 446 Prüfungen erfolgreich (Logik, Übergänge pixelgenau, Oberfläche, Cue-Liste, Updater; mit `-Media` auch echtes Video und echte Webseiten).
- Installer: `NX-Present-Setup-0.4.0-x64.exe` kompiliert (Inno Setup, 83 MB), Prüfsumme in `SHA256SUMS.txt`.
- Quellarchiv der Fremdkomponenten: `NX-Present-ThirdParty-Sources-0.4.0.zip`. Die Abhängigkeiten sind seit 0.3.1 unverändert (LibVLCSharp 3.10.2, VideoLAN.LibVLC.Windows 3.0.24, WebView2 SDK 1.0.4314.35), das Archiv von 0.3.1 wurde unter neuem Namen übernommen und hat dieselbe Prüfsumme.

## Nicht geprüft

- Das Setup wurde nicht ausgeführt, weil es die installierte Kopie ersetzen würde. Update von der Vorversion (Start, Setup → Updates zeigt die neue Version, „Nach Updates suchen“ meldet „aktuell“) steht aus.
- Selbstaktualisierung auf einem echten PC über eine veröffentlichte Version.
- Verhalten unter Smart App Control (Windows kann ein nicht signiertes Setup blockieren).
- Echter Beamer und Bildrate bei hoher Last.
- Tonausgabe bei laufendem Wechsel des Windows-Geräts.
- Eigene Code-Signatur: nicht vorhanden.
