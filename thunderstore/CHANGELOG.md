# Changelog

## 0.1.8

- Item-Symbole im Menü: dieselben Icons, die das Spiel in den
  Inventar-Slots zeigt (aus den Item-Prefabs geladen, gecacht).
- Maus-Relock nach Menü-Schließen deutlich beharrlicher (bis 10s
  erzwungen, Spiel-Menüs wie das Esc-Menü werden respektiert).
- Klick-Diagnose: Mausklicks bei offenem Menü werden ins Log
  geschrieben, um Eingabeprobleme nachvollziehen zu können.

## 0.1.7

- AssetBundle-Loader verbessert: sucht die Bundle-Datei auch im
  Plugins-Wurzelordner und in Unterordnern, matcht Prefab-Namen unscharf
  (z. B. "REPO_Truhe", "WallTerminal") und loggt beim Start genau,
  welches Bundle geladen wurde, welche Prefabs es enthält und wie groß
  die Modelle sind. Ohne Bundle wird klar geloggt, dass Platzhalter
  aktiv sind.

## 0.1.6

- Harmony-Patches werden jetzt pro Hook isoliert angewendet: ein durch ein
  Spiel-Update umbenannter Methodenname deaktiviert nur noch diesen einen
  Hook statt aller folgenden (sehr wahrscheinlich die Ursache für die
  fehlende Truhe am Extraction Point).
- Zusätzlicher patch-unabhängiger Überwacher: Truhe spawnt auch dann, wenn
  alle Extraction-Hooks fehlschlagen (Abfrage des Abschluss-Zählers).
- Menü zeigt jetzt eine Statuszeile (z. B. "'Gun' entnommen" oder
  Fehlerhinweise); Nehmen/Benutzen loggt jeden Schritt.
- Power-Kristalle bleiben Vanilla (nicht mehr im Lager); bereits
  eingelagerte Kristalle/Carts werden beim Laden zurückverschoben.
- Maus-Relock nach Menü-Schließen deutlich verstärkt.
- F9 erneut auf das Terminal gedrückt dreht es um 180° (für Modelle, die
  andersherum ausgerichtet sind) – Ausrichtung wird mitgespeichert.
- Item-Spawn: klare Fehlermeldungen, Fallback-Spawnposition vor der Kamera.

## 0.1.5

- Maus wird nach dem Schließen des Menüs wieder korrekt gesperrt
  (kurzes erzwungenes Re-Locking nach dem Schließen).
- Terminal montiert sich jetzt automatisch an der nächsten Wand rund um
  den Truck-Bildschirm (Raycast), bis einmal per F9 eine eigene Position
  gespeichert wurde – kein unsichtbares/versetztes Terminal mehr.
- UI neu gestaltet im Spiel-Look: dunkles Panel mit abgerundeten Ecken,
  orangefarbene Akzente, klare weiße Schrift.

## 0.1.4

- Komplett neues UI für Truhe und Terminal: dunkles Panel im CRT-Grün-Look
  mit Kopfzeile, Abschnitten (Upgrades/Items), Zeilen-Hervorhebung,
  Hover-Auswahl und gestylten Buttons.
- Terminal-Anker auf das Truck-Wurzelobjekt umgestellt: die per F9
  festgelegte Position gilt damit identisch in Lobby und Levels.
  (Nach dem Update das Terminal bitte einmal neu per F9 platzieren.)

## 0.1.3

- Menü-Bedienung repariert: Cursor wird jetzt zuverlässig entsperrt
  (LateUpdate-Override), Menü komplett per Tastatur bedienbar
  (↑/↓ wählen, Enter = Nehmen, U = Upgrade benutzen).
- Menü schließt mit E/Tab statt nur Esc (Esc öffnet das Spielmenü);
  Blickrichtung schließt das Menü nicht mehr, nur Entfernung.
- Terminal-Platzierung per Taste (Standard F9): auf eine Wand zielen,
  F9 drücken – Terminal sitzt dort und die Position wird dauerhaft
  gespeichert (auch für kommende Sessions/Level).
- Truhen-Spawn zusätzlich an StateSet, StateSetRPC und
  RoundDirector.ExtractionCompleted gekoppelt, falls StateComplete
  nicht durchlaufen wird.

## 0.1.2

- Terminal wird nicht mehr unter den Truck-Bildschirm geparentet
  (skalierte Displays machten es unsichtbar klein); Platzierung jetzt in
  Welt-Koordinaten vor der Wand.
- Truhen-/Terminal-Erzeugung komplett abgesichert und mit Logging versehen.
- Neue Debug-Taste (Standard `F8`): schreibt einen Diagnose-Report ins
  BepInEx-Log und spawnt eine Debug-Truhe direkt vor dem Spieler.

## 0.1.1

- Alle Harmony-Hooks abgesichert: Exceptions können Vanilla-Abläufe
  (Speichern/Laden, Extraction-Point-Status) nicht mehr abbrechen.
- SemiFunc-Aufrufe mit Photon-Fallbacks abgesichert (Hauptmenü/Ladephasen).
- Käufe-Abgriff greift nur noch im Shop; Carts/Fahrzeuge bleiben Vanilla
  (Start-Cart landet nicht mehr im Lager).
- Terminal-Suche findet jetzt auch inaktive Truck-Bildschirme.
- Terminal-Offset-Config auf einzelne Float-Werte umgestellt (OffsetX/Y/Z,
  RotationY) für BepInEx-Kompatibilität.
- Deutlich mehr Diagnose-Logging (Szenen, Spawns, übersprungene Käufe).

## 0.1.0

- Erste Version.
- Vorratstruhe spawnt am abgegebenen Extraction Point (dynamische Platzsuche).
- Wand-Terminal im Truck mit CRT-Boot-Animation.
- Shop-Käufe wandern in ein geteiltes Lager (Host-autoritativ, Photon-Sync).
- Items entnehmen mit Auto-Equip, Spieler-Upgrades direkt verwenden.
- Lagerinhalt wird beim Host pro Spielstand gespeichert.
