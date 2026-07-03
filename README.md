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
- **Nehmen**: Item wird gespawnt und nach Möglichkeit direkt in einen freien Slot gelegt.
- **Benutzen** (nur Spieler-Upgrades): Upgrade wird verbraucht und sofort angewendet.
- `Esc` schließt das Menü.

## Konfiguration (`BepInEx/config/truckorganizer.storage.cfg`)

| Option | Standard | Beschreibung |
| --- | --- | --- |
| `ChestEnabled` | `true` | Truhe am abgeschlossenen Extraction Point spawnen |
| `TerminalEnabled` | `true` | Terminal im Truck spawnen |
| `TerminalInLevels` | `true` | Terminal auch während Levels im Truck spawnen |
| `PurchasesGoToStorage` | `true` | Shop-Käufe wandern ins Lager statt in den Truck |
| `InteractKey` | `E` | Taste zum Öffnen von Truhe/Terminal |
| `InteractRange` | `2.6` | Maximale Interaktionsdistanz |
| `Terminal.PositionOffset` | `(1.35, 0, 0)` | Terminal-Position relativ zum Truck-Bildschirm |
| `Terminal.RotationOffset` | `(0, 0, 0)` | Zusätzliche Terminal-Rotation (Euler) |

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
