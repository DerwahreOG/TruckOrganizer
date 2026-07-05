# Changelog

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
