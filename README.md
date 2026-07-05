# TruckOrganizer

Ein R.E.P.O.-Mod, der den Shop-Einkauf in ein geteiltes, persistentes Lager verwandelt:

- **Vorratstruhe am Extraction Point** – spawnt erst, wenn der Extraction Point erfolgreich
  abgegeben wurde. Die Umgebung ist prozedural, daher sucht der Mod bei jedem Level
  dynamisch eine freie, nicht verdeckte Stelle rund um den Extraction Point.
- **Wand-Terminal im Schiff (Truck)** – hängt neben dem Truck-Bildschirm und bietet Zugriff
  auf denselben Lagerinhalt. Das Display bootet mit CRT-Flacker-Animation.
- **Gemeinsamer Inhalt** – alles, was im Shop gekauft wird, landet im Lager statt im Truck.
  Truhe und Terminal zeigen immer denselben, synchronisierten Bestand.
- **Items entnehmen** – entnommene Items werden gespawnt und automatisch in einen freien
  Inventar-Slot gelegt (falls einer frei ist).
- **Upgrades direkt verwenden** – Spieler-Upgrades können aus dem Menü heraus verbraucht
  werden und wirken sofort auf den anfragenden Spieler.
- **Persistenz beim Host** – der Lagerinhalt wird beim Host pro Spielstand gespeichert
  (`BepInEx/config/TruckOrganizer/<Spielstand>.txt`) und beim nächsten Laden wiederhergestellt.
- **Single- und Multiplayer** – der Host ist autoritativ; Clients senden Anfragen und
  erhalten Snapshots über Photon-Events.

## Installation

1. [BepInEx 5](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) installieren.
2. `TruckOrganizer.dll` nach `BepInEx/plugins/` kopieren.
3. Optional: AssetBundle `truckorganizer` (eigene 3D-Modelle, siehe `docs/ASSETS.md`)
   neben die DLL legen. Ohne Bundle nutzt der Mod eingebaute Platzhalter-Modelle.

Im Multiplayer sollte mindestens der Host den Mod installiert haben; für Truhe,
Terminal und Menü brauchen ihn alle Spieler.

## Bedienung

- Truhe/Terminal ansehen und `E` drücken (Taste konfigurierbar) → Lager-Menü öffnet sich.
- **Nehmen** (`Enter` oder Button): Item wird gespawnt und nach Möglichkeit in einen freien Slot gelegt.
- **Benutzen** (`U` oder Button, nur Spieler-Upgrades): Upgrade wird verbraucht und sofort angewendet.
- Auswahl mit `↑`/`↓`; `E`, `Tab` oder `Esc` schließt das Menü.
- **Terminal versetzen**: Auf eine Wand zielen und `F9` drücken – die Position wird
  dauerhaft gespeichert und in jeder Szene wiederverwendet.

## Konfiguration (`BepInEx/config/truckorganizer.storage.cfg`)

| Option | Standard | Beschreibung |
| --- | --- | --- |
| `ChestEnabled` | `true` | Truhe am abgeschlossenen Extraction Point spawnen |
| `TerminalEnabled` | `true` | Terminal im Truck spawnen |
| `TerminalInLevels` | `true` | Terminal auch während Levels im Truck spawnen |
| `PurchasesGoToStorage` | `true` | Shop-Käufe wandern ins Lager statt in den Truck |
| `InteractKey` | `E` | Taste zum Öffnen von Truhe/Terminal |
| `InteractRange` | `2.6` | Maximale Interaktionsdistanz |
| `Terminal.OffsetX/Y/Z` | `1.35 / 0 / 0` | Terminal-Position relativ zum Truck-Bildschirm |
| `Terminal.RotationY` | `0` | Zusätzliche Terminal-Rotation um die Y-Achse (Grad) |

## Testen

1. **Mod bauen** (Windows, .NET SDK 8): `dotnet build TruckOrganizer/TruckOrganizer.csproj -c Release`
   → DLL liegt unter `TruckOrganizer/bin/Release/netstandard2.1/TruckOrganizer.dll`.
2. **BepInEx installieren** – am einfachsten über r2modman/Thunderstore Mod Manager
   (Profil für R.E.P.O. mit `BepInExPack`), alternativ manuell BepInEx 5 (x64) ins Spielverzeichnis.
3. **DLL installieren**: nach `BepInEx/plugins/` kopieren. Bei r2modman liegt das Profil unter
   `%AppData%\r2modmanPlus-local\REPO\profiles\<Profil>\BepInEx\plugins\`.
4. **Konsole aktivieren**: in `BepInEx/config/BepInEx.cfg` unter `[Logging.Console]`
   `Enabled = true` setzen – dann siehst du alle Mod-Logs live.

Testablauf (erst Singleplayer, dann Multiplayer):

- Spiel starten → Log muss `TruckOrganizer v0.1.5 loaded.` zeigen.
- Im Truck: Terminal neben dem Bildschirm mit Boot-Flackern. Position passt nicht?
  → `Terminal.PositionOffset` / `RotationOffset` in der Config justieren.
- Im Shop einkaufen → Log zeigt `Moved purchase '...' into storage`, die Items
  dürfen **nicht** im Truck spawnen; im Terminal-Menü müssen sie auftauchen.
- Level spielen, Extraction Point abgeben → Truhe spawnt in der Nähe
  (Log: Spawn-Broadcast). Mit `E` öffnen.
- „Nehmen": Item spawnt und landet in einem freien Slot. „Benutzen" bei Upgrades:
  Effekt sofort spürbar (z. B. Stamina-Balken länger).
- Speichern, Spiel beenden, Spielstand neu laden → Lagerinhalt muss wieder da sein
  (Datei: `BepInEx/config/TruckOrganizer/<Spielstand>.txt`).
- Multiplayer mit einem zweiten Spieler (beide mit Mod): Client sieht Truhe/Terminal,
  gleicher Inhalt, Nehmen/Benutzen vom Client aus funktioniert und synchronisiert.

Bei Problemen: `BepInEx/LogOutput.log` prüfen – alle Fehler des Mods sind dort geloggt.
Zusätzlich gibt es eine Debug-Taste (Standard `F8`): Sie schreibt einen Diagnose-Report
ins Log (Szene, Spielstatus, gefundene Truck-Bildschirme, Lagerinhalt) und spawnt eine
Debug-Truhe direkt vor dem Spieler, über die das Lager immer erreichbar ist.

## Thunderstore-Release

Die Paketstruktur liegt unter `thunderstore/` (Manifest, Icon, Changelog).

```bash
./pack.sh        # Linux/macOS
# oder
powershell -ExecutionPolicy Bypass -File pack.ps1   # Windows
```

Das Skript baut die Release-DLL und erzeugt `dist/TruckOrganizer-<version>.zip` mit
`manifest.json`, `icon.png`, `README.md`, `CHANGELOG.md` und der DLL. Upload:

1. Auf [thunderstore.io](https://thunderstore.io/c/repo/) einloggen (GitHub/Discord/Overwolf).
2. Unter *Settings → Teams* ein Team anlegen – der Teamname wird der Namespace des Pakets.
3. *Upload* → Community **R.E.P.O.** wählen, Zip hochladen, fertig.

Für neue Versionen: `version_number` in `thunderstore/manifest.json` **und** `Version`
in der `.csproj` erhöhen, `CHANGELOG.md` ergänzen, neu packen, hochladen.

## Entwicklung

Voraussetzungen: .NET SDK 8.

```bash
dotnet build TruckOrganizer/TruckOrganizer.csproj
```

Die Spiel-Assemblies kommen als gestrippte, publizierte Stubs über das NuGet-Paket
`R.E.P.O.GameLibs.Steam`; eine lokale Spielinstallation ist zum Bauen nicht nötig.
Um die DLL automatisch ins Spiel zu deployen, eine Datei `Directory.Build.props`
mit folgendem Inhalt anlegen (nicht committen):

```xml
<Project>
  <PropertyGroup>
    <BepInExPluginDirectory>C:\Pfad\zu\REPO\BepInEx\plugins</BepInExPluginDirectory>
  </PropertyGroup>
</Project>
```

## Architektur

| Baustein | Aufgabe |
| --- | --- |
| `Core/StorageService` | Host-autoritatives Lager, Persistenz, Snapshots |
| `Core/NetworkEvents` | Photon-`RaiseEvent`-Transport, Singleplayer-Fallback |
| `Core/ChestSpawner` | Spawn-Trigger + dynamische Platzsuche um den Extraction Point |
| `Core/TerminalSpawner` | Terminal-Verankerung am Truck-Bildschirm |
| `Core/ItemSpawner` | Item-Spawn (Host) + Auto-Equip beim Anfragenden |
| `Core/UpgradeMap` | Upgrade-Item → `PunManager`-Upgrade-Aufruf |
| `Core/AssetFactory` | AssetBundle-Loader + Platzhalter-Meshes |
| `Behaviours/*` | Interaktion, IMGUI-Menü, Terminal-Bootanimation |
| `Patches/*` | Harmony-Hooks (`ExtractionPoint.StateComplete`, `StatsManager`) |
