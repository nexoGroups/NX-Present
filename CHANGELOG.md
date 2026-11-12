# Änderungen

## 0.7.0-beta.2 – Skalierbares Layout und Industrielles Design

- **Dynamische Größenanpassung der Container (Resizable Layout):** Alle Hauptmodule lassen sich flexibel in ihrer Größe anpassen.
  - Das AUX-Modul lässt sich über einen horizontalen Studio-Anfasser von 32 px bis 180 px frei in der Höhe skalieren.
  - Das Optionen-Panel (Inspector) verfügt über einen vertikalen Splitter-Anfasser zur stufenlosen Breitenanpassung (200 px bis 500 px).
  - Ein Klick in den Leerbereich des Mischers oder auf den Schalter **📐 Layout** aktiviert den Bearbeitungsmodus mit optischen cyanfarbenen Begrenzungsrahmen.
  - Voreinstellungen für die AUX-Höhe (S · 42px, M · 70px, L · 100px) und ein Schalter zum Zurücksetzen auf den Standard erlauben schnelles Umschalten.
  - Das angepasste Layout wird lokal auf dem Gerät gespeichert und bleibt beim nächsten Programmstart erhalten.
- **Dezenter technischer Eckenradius:** Weiche Rundungen wurden durch präzise 2 px bis 3 px Radien ersetzt (Karten, Eingaben, Buttons, Tally-Lampen und T-Bar-Faderknauf).
- **Industrieller Studio- und Broadcast-Look:**
  - Tiefere Anthrazittöne mit sichtbaren 1px-Präzisionsrahmen statt weicher Schatten.
  - Funktionale Farbkodierung für Bearbeitung (Cyan), Warnung (Amber), Program (Tally Rot) und Preview (Grün).
  - Monospace-Typografie (`Consolas` / `NxMono`) für AUX-Kanalbezeichnungen, Messwerte, Badges und Zeitcodes.
  - Der T-Bar-Überblender besitzt geriffelte vertikale Grifflinien für eine taktile Broadcast-Fader-Optik.

## 0.7.0-beta.1 – Szenen, Fensteraufnahme und Cue Mode

- Im Szeneneditor kombinierst du Bilder, Text, Uhr und Countdown auf einer 16:9-Fläche. Vorlagen für Pre-Show, Song-Anzeige und Bauchbinden dienen als Ausgangspunkt. Szenen lassen sich im Medienpool ablegen oder einem AUX-Kanal zuweisen.
- Mit **Anwendung erfassen** nimmst du ein geöffnetes Windows-Fenster als Bildquelle auf. Szenen können auch Titel und Cover aus der Windows-Mediensitzung sowie einen Audio-Visualizer anzeigen.
- Der Medienpool hat eine eigene Seite. Im überarbeiteten AUX-Patcher wählst du oben den Zielkanal und unten die Quelle. Die Kanäle zeigen eine Vorschau ihrer Zuweisung.
- Video-Cues unterstützen In- und Out-Punkte. Die Quelle **Cue-Show** zeigt in dieser Beta eine Platzhaltertafel.
- Die Beta wird über **NXPresent-beta-Setup.exe** bewusst installiert. Installierte Beta-Versionen suchen auf dem Beta-Kanal nach neueren Vorabversionen. Der Stable-Kanal liefert reguläre Versionen aus.

## 0.6.0 – ME1, ME2 und AUX-Patcher

- ME1 und ME2 stehen fest im Mischer. CUT, AUTO und der Überblender wechseln die Projektion zum jeweils anderen ME.
- Zehn AUX-Kacheln zeigen Standbilder. Ein Klick lädt die zugewiesene Quelle ins inaktive ME. Die Tasten 1 bis 9 und 0 wählen AUX 1 bis 10.
- Im AUX-Patcher stehen Medienpool, Schwarz und Windows-Aufnahmegeräte zur Auswahl. Kamera und HDMI-Capture liefern Bild. Die AUX-Belegung wird automatisch und in der Showdatei gespeichert. Frühere Showdateien lassen sich weiterhin öffnen.
- Bei 1180 × 740 bleiben alle zehn AUX-Kacheln auch mit geöffneten Optionen sichtbar. Ein Livebild von Kamera oder HDMI und die Ausgabe auf einem Beamer wurden noch nicht an Hardware geprüft.

## 0.5.2 – Video-Vorschau und NX-UI-System

- Videos in der Vorschau werden erst als bereit angezeigt, wenn sie auf ihrem ersten Bild angehalten sind. Beim erneuten Vorladen startet der Decoder wieder am Anfang.
- Diese Version enthält den kompakten Mischer, das dunkle NX-UI-System und die überarbeiteten Abstände aus 0.5.0 und 0.5.1.
- Wer noch 0.3.1 nutzt, installiert das neue Windows-Setup einmal von Hand. Danach kann NX-Present Updates über Velopack beziehen.

## 0.5.1 – Mehr Platz in den Quellenzeilen

- In den Programm- und Vorschauzeilen haben Beschriftungen, Schloss und Quellentasten mehr Abstand. Die ersten Quellentasten stehen bündig untereinander.
- Das Setup enthält auch den kompakten Mischer und das NX-UI-System aus 0.5.0.

## 0.5.0 – NX-UI-System

- Bildmischer, Cue Mode und Setup verwenden das dunkle NX-UI-System mit Inter, blauen Bedienelementen und einheitlichen Karten, Menüs und Eingabefeldern.
- Im Mischer stehen die Monitore im Vordergrund. Übergangsart, Dauer, Überblender und Live-Tasten liegen in einer Leiste. Der Medienpool scrollt waagerecht. Audio, Bild und Details zur Vorschau findest du unter **Optionen**.
- Im Cue Mode lässt die flachere Startleiste mehr Platz für den Ablauf.
- Der Überblender verwendet den NX-Griff und zeigt den Fortschritt in Blau an. Rot kennzeichnet weiterhin das Programm, Grün die Vorschau.
- Gegenüber 0.3.1 kommen der Bildmischer und die Cue-Liste hinzu. Das Windows-Setup wechselt zu Velopack und unterstützt automatische Updates. Einzelheiten stehen unter 0.4.0.

Wer 0.3.1 installiert hat, startet das neue Setup einmal von Hand. Die Einstellungen werden beim Wechsel übernommen.

## 0.4.0 – Bildmischer

Die Bedienung ist neu aufgebaut wie ein Videomischer.

- **Echter Viewer:** Der Programm-Monitor zeigt in Echtzeit das Bild der Ausgabe, auch bei laufenden Videos, Webseiten, Uhr und animierten Testbildern. Der Vorschau-Monitor zeigt die nächste Quelle live; ein Video wartet auf seinem ersten Bild.
- **CUT, AUTO und Überblender:** Sofortschnitt, automatischer Übergang mit einstellbarer Dauer und ein Überblender für die Handblende. AUTO aus halb gezogenem Überblender läuft den Rest. Nach jedem Übergang tauschen Programm und Vorschau die Plätze.
- **Neue Übergänge:** Wischen waagerecht und senkrecht, Iris und Schieben, zusätzlich zu Überblenden und Über Schwarz. Der Ton von Videos blendet mit.
- **FTB** (weich nach Schwarz, mit Ton) und **Schwarz + stumm** (sofort).
- **Programm- und Vorschau-Zeile** mit einer Taste je Quelle, Ziffern 1 bis 9 und 0, Tally in Rot und Grün. Die Programm-Zeile ist gesperrt, bis man sie freigibt.
- **Medienpool** mit Vorschaubildern, Suche, Filtern und Kontextmenü; Webseiten und Texte werden in einem Dialog hinzugefügt.
- **Neue Oberfläche** nach dem Vorbild von Mischer- und Grading-Software: dunkle neutrale Flächen, rechts ein Bereich für Ausgabe, Audio, Bild und Quelle, die Seitenwahl unten statt links.
- **Cue Mode als Cue-Liste nach dem Vorbild von QLab:** Eine Marke zeigt den nächsten Cue; GO startet ihn und setzt die Marke sofort weiter. Jeder Cue hat Nummer, Pre-Wait, Dauer und Post-Wait sowie Auto-Continue oder Auto-Follow, sodass eine Kette mit einem GO läuft. Cue-Arten: Quelle, Schwarz, Lautstärke, Video steuern, Warten und Notiz. Dazu kommen Ausschalten, Farbmarken, Tasten für den Direktabruf, Gehe zu Cue, Nummerierung, ein Inspektor mit Reitern, Fortschrittsbalken in der Liste, Vorladen der nächsten Quelle und STOPP (Esc). Weil es einen Programmausgang gibt, warten Quell-Cues in einer Warteschlange auf das Ende des laufenden Übergangs. GO funktioniert auch ohne Ausgabefenster. Showdateien mit Cue-Liste haben Format 3; ältere Dateien bleiben lesbar und werden von oben nummeriert.
- **Beispiel-Show:** `examples/Demo.nxshow` enthält jetzt eine kleine Cue-Liste mit Notiz, Auto-Follow-Kette, Taste und Schwarzblende.
- **Ohne Beamer nutzbar:** Der Mischer arbeitet vollständig ohne Ausgabefenster.
- **Automatische Updates:** Unter Setup zeigt eine neue Karte die installierte Version und bietet „Nach Updates suchen“ und „Jetzt aktualisieren“. Beim Start sucht NX-Present außerdem im Hintergrund auf der offiziellen GitHub-Veröffentlichungsseite nach einer neueren Version und installiert sie, solange nichts live ist; läuft schon Inhalt, wartet das Update auf einen Klick, und ein Hinweis „UPDATE x.y.z“ erscheint in der Statusleiste. Beides lässt sich unter Setup ausschalten. Die Updates kommen von Velopack und werden nur von `github.com/nexoGroups/NX-Present` geladen; jedes Paket wird gegen die Prüfsumme der Update-Liste derselben Veröffentlichung geprüft. Ein fehlgeschlagener Versuch wird nicht bei jedem Start wiederholt, und eine selbst gebaute Kopie wird nicht überschrieben.
- **Neues Setup (Velopack statt Inno Setup):** Das Setup heißt jetzt `NXPresent-win-Setup.exe` und installiert ohne Rückfragen für das Benutzerkonto nach `%LOCALAPPDATA%\NXPresent`; ein Update wartet auf das Ende der laufenden Anwendung, spielt die neue Version ein und startet NX-Present danach neu. Es gibt keine Lizenzseite im Setup mehr, die Nutzungsbedingungen liegen als `LICENSE-NX-Present.txt` im Programmordner. Zusätzlich gibt es eine tragbare Fassung (`NXPresent-win-Portable.zip`). Die Plugin-Dateien der Video-Engine werden mit jeder Version komplett ersetzt. Wer 0.3.1 mit dem alten Setup installiert hat, installiert das neue Setup einmal von Hand und deinstalliert die alte Kopie danach unter Windows-Einstellungen → Apps; die Einstellungen bleiben erhalten.

Behobene Fehler der Vorgängerfassung (nach Durchsicht des Codes):

- Der Überblender konnte beim Start eines Übergangs die Meldung „Die Breite und die Höhe dürfen nicht negativ sein“ auslösen. Stand der Griff weniger als einen Pixel von der Ausgangsstellung entfernt, rechnete die Zeichnung des gelben Füllbalkens mit einer negativen Breite, und WPF brach das Layout ab; die Meldung blockierte dann als Dialog die Bedienung. Das trat bei AUTO und bei GO im Cue Mode auf, wenn der erste Bildaufbau genau in diesen Bereich fiel. Der Balken wird jetzt erst ab einem Pixel gezeichnet, und eine Prüfung deckt die Griffstellungen und Breiten ab.
- Die AUTO-Taste wurde während einer laufenden Blende abgeblendet statt rot und voll sichtbar zu leuchten.
- Der Live-Monitor zeigte bei Videos und Webseiten ein Bildschirmfoto der Ausgabe, einmal pro Sekunde. Es gibt jetzt keine Bildschirmaufnahme mehr; beide Monitore zeigen die Bilder selbst in Echtzeit.
- Übergänge liefen über ein zusätzliches Fenster mit einem Bildschirmfoto als Zwischenbild. Ein abgebrochener Wechsel ließ die Ausgabe schwarz. Der Übergang wird jetzt im Compositor gerechnet und bricht nie nach Schwarz ab.
- Ein neuer Wechsel sperrte die Bedienung, bis der Inhalt geladen war, bei einer Webseite bis zu 15 Sekunden. Quellen laden jetzt in der Vorschau; AUTO ist erst freigegeben, wenn die Quelle bereit ist.
- Die Vorschau war nur ein Standbild und nicht das, was später lief. Sie zeigt jetzt die Quelle selbst.
- Videos und Webseiten steckten in eigenen Fenstern, die sich nicht mit anderen Bildern überblenden ließen. Alles wird jetzt im selben Bildaufbau gezeichnet.
- Ein Wechsel der Tonausgabe lud die laufenden Medien neu. Das ist nicht mehr nötig.
- Fotos aus Kameras und Telefonen konnten im Hochformat falsch herum stehen, weil die Ausrichtung der Aufnahme nicht beachtet wurde. Sehr große Bilder werden außerdem auf eine sinnvolle Größe verkleinert.
- In Einstellungen und Showdateien wurden abgeleitete Felder (Symbol, Kategorie, Detail) mitgeschrieben. Alte Dateien bleiben lesbar.

Technik: Videos laufen über die Speicherausgabe der Video-Engine, Webseiten über den Screencast eines unsichtbaren Browsers. Beides liefert Bilder an dieselben Monitore wie die Ausgabe. Die Prüfungen decken Logik, Übergänge pixelgenau, die Oberfläche und, mit `-Media`, echtes Video und echte Webseiten ab.

Hinweise: Das Setup einer Veröffentlichung ist nicht signiert. Die Prüfsumme erkennt beschädigte oder unvollständige Downloads, aber keine manipulierte Veröffentlichung; auf einem PC mit Smart App Control kann Windows das Setup eines Updates blockieren, dann meldet NX-Present das und lässt die Datei im Update-Ordner liegen. Videos werden softwareseitig dekodiert und auf höchstens Full HD verkleinert. Webseiten lassen sich nicht bedienen; ihr Ton schaltet bei Übergängen um, statt zu überblenden. Das Verhalten mit einem echten Beamer, die Tonausgabe bei laufendem Wechsel des Windows-Geräts und die Bildrate bei hoher Last wurden nicht an Hardware geprüft.

## 0.3.1 – Erste öffentliche Veröffentlichung

- Schwarz-neongelbe Bedienoberfläche mit Bibliothek, Vorbereitung und Leinwand.
- Eigenständige Seiten für Setup und Hilfe.
- Bilder, Videos, Webseiten, Texte, Uhr und animierte Testbilder.
- Live-Freigabe mit Sofort, Überblenden und Über Schwarz.
- Globale Freigabe der Ausgabe über Strg + Alt + F10.
- Gespeicherte Konfiguration und automatischer Schwarzstart für eine ausdrücklich gewählte Ausgabe.
- Erststart ohne automatische Bildschirm-Vermutung.
- Windows-Installer mit gebündelter .NET-Laufzeit und Video-Engine sowie bedarfsgerechter WebView2-Installation.

60 FPS ist das Animationsziel; durchgehend konstante 60 FPS sind nicht bestätigt.
