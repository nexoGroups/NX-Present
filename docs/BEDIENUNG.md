# Bedienung

## Anschließen und einrichten

Verbinde Hauptmonitor und Beamer. Windows muss die Anzeigen **erweitern**, damit die Bedienung und die Leinwand getrennte Flächen erhalten. Wähle beim ersten Start unter **Setup → Leinwand und Ausgabeschutz** den tatsächlichen Beamer. Die Nummer allein ist nicht auf jedem PC dieselbe.

Mit **Ausgabe starten** beginnt die Projektion schwarz. **Maus und andere Fenster von der Leinwand fernhalten** aktiviert den Ausgabeschutz. Der globale Ausstieg **Strg + Alt + F10** bleibt erreichbar.

Der Mischer arbeitet auch ohne Ausgabe. Das für die Projektion gewählte ME ist rot markiert. So lässt sich ein Ablauf an einem einzelnen Monitor proben.

## Der Mischer

**ME1** links und **ME2** rechts zeigen ihre Quellen an festen Plätzen. Rot markiert das für die Projektion gewählte ME. Ein Video im inaktiven ME wartet auf seinem ersten Bild und lässt sich dort mit der Wiedergabeleiste prüfen.

- **AUX 1–10:** Jeder Kanal zeigt ein Vorschaubild seiner Quelle. Ein Klick lädt sie ins inaktive ME. Die Tasten **1** bis **9** wählen AUX 1 bis 9, **0** wählt AUX 10.
- **AUX-Patcher:** Orientiert sich an Blackmagic ATEM Software Control mit getrennten Bereichen: Oben werden sämtliche AUX-Ausgänge als Kacheln mit Live-Vorschau dargestellt; ein Klick wählt den AUX-Kanal für das Routing aus. Unten befinden sich alle regulären Signalquellen (Kameras, Capture-Eingänge, Medien, Testbilder, Schwarz); ein Klick weist die Quelle unmittelbar dem ausgewählten AUX-Kanal zu. Mit **Aufnahmegeräte neu erkennen** werden HDMI- und USB-Aufnahmegeräte aktualisiert.
- **Medienpool:** Im normalen Modus vollständig ausgeblendet, um die Live-Bedienoberfläche übersichtlich und fokussiert zu halten. Über den separaten Modus **MEDIENPOOL** in der Seitenleiste/Navigationsleiste erreichbar. Dort lassen sich alle Quellen verwalten, durchsuchen, filtern und neue Inhalte hinzufügen.

### Szenen und Anwendungsfenster

- Unter **AUX-Patcher → OBS-Szeneneditor** öffnest du den Szeneneditor für den ausgewählten Kanal. Wähle eine Vorlage und passe Quellen, Ebenen und Positionen an. Beim Speichern kannst du die Szene im Medienpool ablegen oder einem AUX-Kanal zuweisen.
- Unter **Medienpool → Hinzufügen → Anwendung / Fenster erfassen** öffnest du die Liste der verfügbaren Windows-Fenster. Das ausgewählte Fenster erscheint als Bildquelle im Medienpool.
- Für Video-Cues stellst du im Inspektor In- und Out-Punkte ein. Die Quelle **Cue-Show** im AUX-Patcher zeigt in dieser Beta eine Platzhaltertafel.

### CUT, AUTO und Überblender

- **CUT** (Eingabetaste) schaltet die Projektion sofort auf das andere ME.
- **AUTO** (Leertaste, Strg + Enter) blendet mit dem gewählten Übergang und der eingestellten **Dauer** über. Ein Video startet erst mit der Blende, sein Ton blendet mit. Der Knopf leuchtet rot und zeigt den Fortschritt.
- **Überblender:** Ziehe ihn von links nach rechts, um von Hand zu mischen. Am rechten Ende übernimmt das andere ME die Projektion. Zurück am Anfang brichst du den Übergang ab. Drückst du AUTO bei halb gezogenem Überblender, läuft der Rest der Blende automatisch.
- **Übergangsart:** Wähle in der Leiste unter den Monitoren zwischen Überblenden, Über Schwarz, Wischen, Iris und Schieben. Daneben stellst du die Dauer ein.
- **CUT während einer Blende** beendet sie sofort.

Nach jedem Übergang wechselt die rote Markierung zum anderen ME. Die Quellen bleiben in ihren MEs. Ein Video im vorherigen ME steht wieder auf seinem ersten Bild. Änderungen im AUX-Patcher wirken beim nächsten Klick auf die Kachel.

### Schwarz

- **FTB** (F) blendet das Programm weich nach Schwarz, auch der Ton. Ein zweiter Druck blendet zurück. Die Dauer stellst du unter Setup ein.
- **SCHWARZ + STUMM** (Strg + B) wirkt sofort und schaltet den Ton ab. Videos laufen im Hintergrund weiter.

Beide ME-Monitore zeigen weiterhin ihre Quellen.

### Ton und Bild

Öffne **Optionen** in der Titelleiste. Unter **Audio** stellst du die Hauptlautstärke ein, schaltest **Stumm** (M) und wählst, ob Videos wiederholt werden. Unter **Bild** legst du fest, wie Bilder und Videos auf die Fläche passen (Einpassen, Füllen, Strecken) und ob Testbilder animiert sind.

## Quellen hinzufügen

**+ Hinzufügen** im Medienpool bietet Bilder und Videos, Webseiten, Text und die Uhr. Dateien lassen sich auch in das Fenster ziehen. Mit dem Mausrad blätterst du waagerecht durch die Quellen. Fotos aus Kameras und Telefonen stehen aufrecht, auch wenn sie im Hochformat aufgenommen wurden.

Eine Webseite läuft in einem unsichtbaren Browser und erscheint wie jede andere Quelle. **Webseite neu laden** findest du unter **Optionen → Quelle im inaktiven ME**. Der Ton einer Webseite schaltet bei einer Blende um.

## Cue Mode: eine Show fahren

Der Cue Mode ist eine Cue-Liste nach dem Vorbild von QLab. Du legst die Reihenfolge deiner Show fest, gibst jedem Cue seine Zeiten und lässt ihn auf **GO** starten, von Hand oder mit **Auto-Continue** und **Auto-Follow** von selbst. Öffne **Cues** in der Seitenleiste.

- **Oben:** **GO**, **STOPP** und **ZURÜCK**, daneben die **Marke** (der nächste Cue), das Feld **Gehe zu Cue** und **Show öffnen / speichern**.
- **Links:** Quellen aus dem Medienpool. Ein Doppelklick oder **+ Zum Ablauf** fügt die gewählte Quelle als Cue hinter der Marke ein. Eine Quelle darf mehrfach vorkommen.
- **Mitte:** die Cue-Liste mit Nummer (Q), Name, Pre-Wait, Dauer, Post-Wait und Weiter-Symbol. Fortschrittsbalken unter den Zeiten zeigen, wie weit ein laufender Cue ist.
- **Rechts:** Vorschau und Programm als Echtzeit-Monitore. Die Vorschau zeigt die geladene Quelle des nächsten Quell-Cues.
- **Unten:** der **Inspektor** des gewählten Cues mit den Reitern **Grundlagen**, **Zeit und Weiter**, **Aktion** und **Taste**.

### Die Marke und GO

Der gewählte Cue ist die **Marke**. **GO** (Leertaste, Strg + Enter) startet den Cue an der Marke und setzt die Marke sofort auf den nächsten. Du kannst also schon wieder GO drücken, während der erste Cue noch wartet oder blendet. Ein Quell-Cue wird erst **LIVE**, wenn sein Übergang fertig ist. Die Quelle an der Marke lädt NX-Present schon vorher in die Vorschau, damit GO ohne Verzögerung startet. Mit den Pfeiltasten **↑ ↓** oder einem Klick bewegst du die Marke, ohne etwas zu starten. Nach dem letzten Cue springt die Marke nicht an den Anfang; **ZURÜCK** setzt sie zurück, ohne das Programm zu ändern.

### Cue-Arten

**+ Cue** legt weitere Arten an. Quellen kommen aus dem Medienpool.

| Art | Wirkung |
| --- | --- |
| Quelle | Bringt Bild, Video, Webseite, Text, Uhr oder Testbild mit dem Übergang des Cues auf Programm |
| Schwarz | Blendet weich nach Schwarz oder zurück, samt Ton, mit eigener Dauer |
| Lautstärke | Blendet die Hauptlautstärke in der Dauer auf einen Zielwert |
| Video steuern | Pausiert, setzt fort oder startet das Video im Programm von vorn |
| Warten | Eine Pause im Ablauf; sinnvoll zusammen mit Auto-Follow |
| Notiz | Ein Merker ohne Wirkung, zum Beispiel „Moderation beginnt“ |

### Zeiten und Weiter

Im Reiter **Zeit und Weiter** (und direkt in der Liste) hat jeder Cue drei Zeiten. Du schreibst sie wie gewohnt: `2,5`, `1:30` oder `500ms`.

- **Pre-Wait:** Wartezeit zwischen GO und dem Start des Cues.
- **Dauer:** wie lange der Cue arbeitet: der Übergang einer Quelle (0,1 bis 5 Sekunden), die Schwarzblende, die Lautstärkeblende oder die Pause.
- **Post-Wait:** Wartezeit bis zum automatischen Start des nächsten Cues.

**Danach** legt fest, wie es weitergeht:

- **Nicht fortfahren:** Der Ablauf bleibt stehen. Der nächste Cue startet mit GO.
- **Auto-Continue (↓):** Der nächste Cue startet, wenn die Post-Wait-Zeit **seit dem Start** dieses Cues abgelaufen ist. Beide laufen dann gleichzeitig.
- **Auto-Follow (⇓):** Der nächste Cue startet, wenn dieser Cue **fertig** ist und die Post-Wait-Zeit abgelaufen ist.

Ein Beispiel: Ein Quell-Cue mit zwei Sekunden Überblendung und Auto-Follow mit fünf Sekunden Post-Wait zeigt sein Bild und startet sieben Sekunden nach GO den nächsten Cue. Eine Kette aus Cues läuft so mit einem einzigen GO.

NX-Present hat **einen** Programmausgang und kann nur **einen Übergang zugleich** fahren. Wird ein Quell-Cue fällig, während ein anderer Übergang läuft, wartet er in der Warteschlange (Status „wartet“) und startet in dem Moment, in dem der Übergang endet. Cues anderer Art laufen daneben weiter.

### Stoppen

**STOPP** (**Esc**) beendet alle wartenden und laufenden Cues und bricht ab, was automatisch folgen würde. Ein Übergang, der schon läuft, wird nicht zurückgenommen und endet von selbst. Esc gilt auf der Cue-Seite überall, auch in einem Textfeld. Ein Cue, der nicht starten kann (zum Beispiel eine fehlende Datei), zeigt ein Warnzeichen und den Grund; die Kette hinter ihm startet dann nicht.

### Nummern, Farben, Tasten

- **Nummer:** Jeder Cue hat eine Nummer wie `12` oder `12.5`, die es nur einmal gibt. Im Feld **Gehe zu Cue** springt die Marke nach Eingabe der Nummer und der Eingabetaste dorthin. **Nummern** nummeriert die Liste von oben neu.
- **Aktiv:** Ein ausgeschalteter Cue wird beim Weiterschalten der Marke und in Ketten übersprungen. Nützlich, um einen Teil der Show zu proben.
- **Farbe:** Ein Farbstreifen am Anfang der Zeile markiert zusammengehörige Cues.
- **Taste:** Im Reiter **Taste** gibst du einem Cue einen Buchstaben, eine Ziffer oder F1 bis F12 (nicht M). Die Taste startet den Cue sofort, ohne die Marke zu bewegen. Sie gilt auf der Cue-Seite, solange kein Textfeld aktiv ist.

Ein laufender oder wartender Cue lässt sich nicht bearbeiten, verschieben oder entfernen. Stoppe ihn zuerst.

### Speichern

Mit **Show speichern** sicherst du Pool und Cue-Liste gemeinsam als `.nxshow`. Dateien mit Cue-Liste verwenden Format 3; ältere Showdateien lassen sich weiter öffnen und werden von oben nummeriert. Änderungen werden zusätzlich mit der Sitzung gespeichert. Nach einem Neustart wird kein Cue automatisch gestartet. `examples/Demo.nxshow` enthält eine kleine Cue-Liste mit Notiz, Auto-Follow-Kette, Taste und Schwarzblende zum Ausprobieren.

## Ton

Unter **Setup → Gemeinsames Audiogerät** wählst du das Ausgabegerät und bestätigst mit **Ton anwenden**. Das verändert das Windows-Standardgerät für alle Programme; laufende Videos und Webseiten folgen ihm. Die vorherige Windows-Auswahl wird beim normalen Beenden wiederhergestellt.

Die Hauptlautstärke regelt Videodateien. Webseiten haben ihre eigene Lautstärke. **Stumm** gilt für beide. Die Vorschau ist stumm, bis die Blende beginnt.

## Speichern, Umziehen und Beenden

Die Konfiguration wird automatisch unter `%LOCALAPPDATA%\NX-Present` gespeichert. Dazu gehören Pool, AUX-Belegung, Gerätewahl, Übergang und Dauer, Lautstärke und Fenstergröße. Bei einem späteren Start kann die gespeicherte Beamer-Ausgabe automatisch mit Schwarzbild einschalten. Die zuletzt vorbereitete Quelle wird wieder ins inaktive ME geladen.

Eine `.nxshow`-Datei sichert Medienpool, Cue-Ablauf und AUX-Belegung. Sie enthält Verweise auf Medien. Beim Umziehen müssen diese Dateien ebenfalls mitgenommen werden. `examples/Demo.nxshow` zeigt Testbilder, Uhr und Text ohne zusätzliche Medien.

**Strg + Alt + F10** schließt nur die Ausgabe. **Programm beenden** unter **Setup** oder das X schließt die ganze Anwendung und gibt die Maus frei.

## Updates

Unter **Setup → Updates** steht die installierte Version. **Nach Updates suchen** fragt die offizielle GitHub-Veröffentlichungsseite ab; ist eine neuere Version da, erscheint **Jetzt aktualisieren**, und **Versionshinweise** öffnet die Seite der Version. NX-Present lädt das Setup, vergleicht es mit der dort veröffentlichten SHA-256-Prüfsumme, beendet sich, lässt das Setup die Dateien ersetzen und startet danach neu. Einstellungen, Pool und Cue-Liste bleiben erhalten.

Beim Start sucht NX-Present im Hintergrund nach Updates (**Beim Start nach Updates suchen**) und installiert ein gefundenes Update selbst (**Gefundene Updates beim Start automatisch installieren, solange nichts live ist**). Läuft schon Inhalt auf dem Programm oder blendet gerade ein Übergang, wartet das Update: Die Statusleiste zeigt **UPDATE x.y.z**, ein Klick darauf öffnet Setup. Während des Ladens läuft alles normal weiter, ein Klick auf die Anzeige bricht den Download ab. Hat ein Update-Versuch nicht geklappt, wiederholt NX-Present ihn nicht bei jedem Start; das Protokoll liegt im Ordner `%LOCALAPPDATA%\NX-Present\Updates`.

Das Setup ist nicht signiert. Auf einem PC mit Smart App Control kann Windows es blockieren; NX-Present meldet das und lässt die geladene Datei im Update-Ordner. Eine selbst gebaute Kopie (ohne Setup-Installation) wird nicht überschrieben: Sie meldet nur, dass es eine neue Version gibt.

Die Deinstallation entfernt die Anwendung; persönliche Einstellungen und Browserprofile bleiben erhalten.

## Wenn etwas nicht funktioniert

- **CUT und AUTO sind grau:** Das inaktive ME hat noch keine bereite Quelle. Der Monitor zeigt den Ladestatus oder den Fehler.
- **Ein Video bleibt schwarz oder ruckelt:** Prüfe, ob die Datei in einem anderen Player läuft. Sehr hohe Auflösungen werden auf Full HD verkleinert, belasten die CPU aber trotzdem.
- **Eine Webseite bleibt leer:** Prüfe Adresse, Internetverbindung und die Microsoft-Edge-WebView2-Laufzeit. Es werden nur HTTP- und HTTPS-Adressen unterstützt.
- **Falsche Anzeige:** Schließe die Ausgabe, wähle unter Setup den richtigen Bildschirm und starte sie erneut.
- **Maus bleibt begrenzt:** Nutze **Strg + Alt + F10**.
- **Kamera oder HDMI liefert kein Bild:** Prüfe das Gerät, das HDMI-Signal und den Kamerazugriff für Desktop-Apps in Windows. Aktualisiere danach die Geräteliste im AUX-Patcher.
- **Medien fehlen nach dem Umziehen:** Lege die verknüpften Dateien wieder am passenden Ort ab oder füge sie neu hinzu.
- **Bildrate schwankt:** Das Ziel garantiert keine durchgehend konstante Ausgabe. Auflösung, Grafiktreiber, Auslastung und Anzeigehardware beeinflussen das Ergebnis.
