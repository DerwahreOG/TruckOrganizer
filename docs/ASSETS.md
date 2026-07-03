# Eigene 3D-Modelle (Truhe & Terminal) einbinden

Der Mod funktioniert ohne zusätzliche Dateien mit eingebauten Platzhalter-Modellen.
Sobald ein AssetBundle namens `truckorganizer` neben der `TruckOrganizer.dll` in
`BepInEx/plugins/` liegt, werden stattdessen die eigenen Modelle verwendet.

> **Hinweis:** Die FBX-/Blend-Dateien aus der Aufgabenbeschreibung sind nicht im
> Repository angekommen. Sie müssen direkt ins Repo gelegt werden (z. B. unter
> `assets/models/`), damit sie hier versioniert werden können. Das AssetBundle
> selbst muss in Unity gebaut werden – das geht nicht automatisiert ohne
> Unity-Installation.

## Unity-Projekt aufsetzen

1. Unity **2022.3.21f1** installieren (gleiche Version wie R.E.P.O.).
2. Neues 3D-Projekt (Built-in Render Pipeline) anlegen.
3. Die FBX-Dateien (`chest.fbx`, `terminal.fbx`) in `Assets/Models/` importieren.

## Prefabs vorbereiten

Es gelten folgende Namenskonventionen, die `AssetFactory` beim Laden erwartet:

### `ChestPrefab`

- Wurzelobjekt heißt beliebig, das Prefab-Asset heißt **`ChestPrefab`**.
- Realistische Größe: ca. 1,0 × 0,7 × 0,65 m, Pivot am Boden.
- Ein `BoxCollider` auf der Wurzel (kein Trigger), damit Spieler nicht durchlaufen.

### `TerminalPrefab`

- Prefab-Asset heißt **`TerminalPrefab`**, Pivot in der Mitte der Rückwand
  (die Rückseite liegt an der Truck-Wand an).
- Der Bildschirm-Renderer muss **`Screen`** im Namen tragen und ein Material mit
  Emission-Slot verwenden (Standard-Shader). Die Boot-Animation setzt
  `_EmissionColor` zur Laufzeit.
- Optional: ein deaktiviertes `Point Light` als Kind (beliebiger Name), leicht vor
  dem Display platziert. Es wird von der Boot-Animation eingeschaltet.
- Das `TerminalScreen`-Skript **nicht** im Unity-Projekt anlegen – der Mod fügt
  seine eigene Komponente zur Laufzeit hinzu und verdrahtet Material und Licht
  über die obigen Konventionen.

## AssetBundle bauen

1. Beide Prefabs markieren und ihnen unten im Inspector das AssetBundle
   **`truckorganizer`** zuweisen.
2. Build-Skript `Assets/Editor/BuildBundles.cs`:

```csharp
using UnityEditor;

public static class BuildBundles
{
    [MenuItem("Tools/Build AssetBundles")]
    public static void Build()
    {
        BuildPipeline.BuildAssetBundles(
            "AssetBundles",
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64);
    }
}
```

3. `Tools → Build AssetBundles` ausführen.
4. Die erzeugte Datei `AssetBundles/truckorganizer` (ohne Endung) neben die
   `TruckOrganizer.dll` nach `BepInEx/plugins/` kopieren.

## Blender-Export-Tipps (Blend → FBX)

- Skalierung anwenden (`Ctrl+A → Scale`), 1 Blender-Einheit = 1 Meter.
- Export mit `Apply Transform` aktiviert, `-Z Forward`, `Y Up`.
- Materialien einfach halten; Texturen mit exportieren oder in Unity neu zuweisen.
