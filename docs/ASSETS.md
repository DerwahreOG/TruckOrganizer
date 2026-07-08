# Eigene 3D-Modelle (Truhe & Terminal) einbinden

Der Mod funktioniert ohne zusätzliche Dateien mit eingebauten Platzhalter-Modellen.
Sobald ein AssetBundle namens `truckorganizer` neben der `TruckOrganizer.dll` in
`BepInEx/plugins/` liegt, werden stattdessen die eigenen Modelle verwendet.

> **Stand:** Die Modelle liegen unter `assets/models/` im Repo:
> `REPO_Truhe.fbx` (Truhe) und `REPO_WallTerminal.fbx` (Terminal, mit
> `Screen_Emissive`-Material für das Display). Das AssetBundle selbst muss
> einmalig in Unity gebaut werden – siehe unten.

## Unity-Projekt aufsetzen

1. Unity **2022.3.21f1** installieren (gleiche Version wie R.E.P.O.), am
   einfachsten über Unity Hub.
2. Neues 3D-Projekt (Built-in Render Pipeline) anlegen.
3. `assets/models/REPO_Truhe.fbx` und `assets/models/REPO_WallTerminal.fbx`
   aus diesem Repo in den Unity-Ordner `Assets/Models/` ziehen.

## Prefabs vorbereiten

Es gelten folgende Namenskonventionen, die `AssetFactory` beim Laden erwartet:

### `ChestPrefab` (aus `REPO_Truhe.fbx`)

- FBX in die Szene ziehen, Skalierung prüfen (Zielgröße ca. 1,0 × 0,7 × 0,65 m,
  Pivot am Boden), dann als Prefab-Asset mit dem Namen **`ChestPrefab`** speichern.
- Ein `BoxCollider` auf der Wurzel (kein Trigger), damit Spieler nicht durchlaufen.

### `TerminalPrefab` (aus `REPO_WallTerminal.fbx`)

- FBX in die Szene ziehen, als Prefab-Asset **`TerminalPrefab`** speichern.
  Pivot in der Mitte der Rückwand; die Rückseite (+Z) liegt an der Truck-Wand an,
  das Display zeigt nach −Z.
- Das Display-Material **`Screen_Emissive`** aus dem FBX extrahieren
  (FBX auswählen → *Materials* → *Extract Materials*) und sicherstellen, dass es
  den Standard-Shader mit Emission-Slot nutzt. Der Mod findet es automatisch
  (Renderer- oder Materialname muss „Screen" enthalten) und animiert
  `_EmissionColor` beim Booten.
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
