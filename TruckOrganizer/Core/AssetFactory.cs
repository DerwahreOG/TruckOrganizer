using System.IO;
using System.Reflection;
using TruckOrganizer.Behaviours;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Creates the chest and terminal objects. If a "truckorganizer" asset
    /// bundle (built from the FBX models) lies next to the plugin DLL its
    /// prefabs are used, otherwise simple placeholder meshes are built in
    /// code so the mod works without any extra files.
    /// </summary>
    public static class AssetFactory
    {
        private const string BundleFileName = "truckorganizer";
        private const string ChestPrefabName = "ChestPrefab";
        private const string TerminalPrefabName = "TerminalPrefab";

        private static AssetBundle _bundle;
        private static Material _baseMaterial;

        public static void TryLoadBundle()
        {
            string path = FindBundleFile();
            if (path == null)
            {
                Plugin.Log.LogInfo(
                    "No 'truckorganizer' asset bundle found next to the plugin DLL - using built-in placeholder models. " +
                    "Eigene Modelle: Bundle-Datei 'truckorganizer' (ohne Endung) neben die TruckOrganizer.dll legen.");
                return;
            }

            _bundle = AssetBundle.LoadFromFile(path);
            if (_bundle == null)
            {
                Plugin.Log.LogError($"Asset bundle at '{path}' could not be loaded " +
                    "(wrong Unity version or not built for StandaloneWindows64?).");
                return;
            }

            Plugin.Log.LogInfo($"Loaded TruckOrganizer asset bundle from '{path}'. " +
                $"Contains: {string.Join(", ", _bundle.GetAllAssetNames())}");
        }

        private static string FindBundleFile()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Directly next to the DLL, then one level up (plugins root).
            foreach (string dir in new[] { assemblyDir, assemblyDir != null ? Path.GetDirectoryName(assemblyDir) : null })
            {
                if (dir == null) continue;
                string candidate = Path.Combine(dir, BundleFileName);
                if (File.Exists(candidate)) return candidate;
            }

            // Last resort: search the whole BepInEx plugin folder.
            try
            {
                string pluginRoot = BepInEx.Paths.PluginPath;
                if (Directory.Exists(pluginRoot))
                {
                    string[] hits = Directory.GetFiles(pluginRoot, BundleFileName, SearchOption.AllDirectories);
                    if (hits.Length > 0) return hits[0];
                }
            }
            catch { /* IO errors are non-fatal */ }

            return null;
        }

        // ------------------------------------------------------------------
        // Chest
        // ------------------------------------------------------------------

        public static GameObject CreateChest()
        {
            GameObject fromBundle = InstantiateFromBundle(ChestPrefabName, "chest", "truhe", "kiste");
            if (fromBundle != null) return fromBundle;

            var root = new GameObject("TruckOrganizer_Chest");

            GameObject body = CreatePart(root.transform, "Body", PrimitiveType.Cube,
                new Vector3(0f, 0.28f, 0f), new Vector3(1.0f, 0.56f, 0.62f), new Color(0.16f, 0.12f, 0.09f));
            GameObject lid = CreatePart(root.transform, "Lid", PrimitiveType.Cube,
                new Vector3(0f, 0.62f, 0f), new Vector3(1.04f, 0.16f, 0.66f), new Color(0.20f, 0.15f, 0.11f));
            CreatePart(root.transform, "Trim", PrimitiveType.Cube,
                new Vector3(0f, 0.44f, 0f), new Vector3(1.06f, 0.05f, 0.68f), new Color(0.75f, 0.6f, 0.2f));

            GameObject glow = CreatePart(root.transform, "Glow", PrimitiveType.Cube,
                new Vector3(0f, 0.545f, 0.315f), new Vector3(0.5f, 0.06f, 0.02f), Color.black);
            MakeEmissive(glow, new Color(0.1f, 1f, 0.35f), 1.6f);

            // One solid collider on the root so players cannot walk through it.
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.38f, 0f);
            collider.size = new Vector3(1.06f, 0.78f, 0.68f);

            RemovePartColliders(body, lid);
            return root;
        }

        // ------------------------------------------------------------------
        // Terminal
        // ------------------------------------------------------------------

        public static GameObject CreateTerminal()
        {
            GameObject fromBundle = InstantiateFromBundle(TerminalPrefabName, "terminal", "wall");
            if (fromBundle != null)
            {
                EnsureTerminalScreen(fromBundle);
                return fromBundle;
            }

            var root = new GameObject("TruckOrganizer_Terminal");

            CreatePart(root.transform, "Casing", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.05f), new Vector3(0.62f, 0.5f, 0.1f), new Color(0.13f, 0.13f, 0.15f));
            GameObject screen = CreatePart(root.transform, "Screen", PrimitiveType.Quad,
                new Vector3(0f, 0.03f, -0.006f), new Vector3(0.5f, 0.34f, 1f), Color.black);
            CreatePart(root.transform, "Keys", PrimitiveType.Cube,
                new Vector3(0f, -0.19f, -0.01f), new Vector3(0.5f, 0.06f, 0.03f), new Color(0.25f, 0.25f, 0.28f));

            Material screenMat = MakeEmissive(screen, new Color(0.1f, 1f, 0.35f), 0f);

            var light = new GameObject("ScreenLight").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.03f, -0.25f);
            light.type = LightType.Point;
            light.color = new Color(0.1f, 1f, 0.35f);
            light.range = 1.6f;
            light.intensity = 0f;
            light.enabled = false;

            TerminalScreen terminalScreen = root.AddComponent<TerminalScreen>();
            terminalScreen.screenMat = screenMat;
            terminalScreen.screenLight = light;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0f, 0.03f);
            collider.size = new Vector3(0.64f, 0.52f, 0.14f);

            return root;
        }

        private static void EnsureTerminalScreen(GameObject terminal)
        {
            if (terminal.GetComponentInChildren<TerminalScreen>() != null) return;

            TerminalScreen screen = terminal.AddComponent<TerminalScreen>();

            // Convention for bundle prefabs: the display material is found via
            // a renderer named "Screen" OR a material whose name contains
            // "Screen" (e.g. the "Screen_Emissive" material from the FBX).
            // An optional disabled light in front of the display is used for
            // the boot glow.
            foreach (Renderer renderer in terminal.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.Contains("Screen"))
                {
                    screen.screenMat = renderer.material;
                    break;
                }
                foreach (Material mat in renderer.materials)
                {
                    if (mat != null && mat.name.Contains("Screen"))
                    {
                        screen.screenMat = mat;
                        break;
                    }
                }
                if (screen.screenMat != null) break;
            }
            screen.screenLight = terminal.GetComponentInChildren<Light>(true);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static GameObject InstantiateFromBundle(string prefabName, params string[] nameKeywords)
        {
            if (_bundle == null) return null;

            GameObject prefab = _bundle.LoadAsset<GameObject>(prefabName);

            // Tolerant fallback: match any prefab in the bundle whose name
            // contains one of the keywords (case-insensitive).
            if (prefab == null)
            {
                foreach (GameObject candidate in _bundle.LoadAllAssets<GameObject>())
                {
                    string name = candidate.name.ToLowerInvariant();
                    foreach (string keyword in nameKeywords)
                    {
                        if (name.Contains(keyword))
                        {
                            prefab = candidate;
                            break;
                        }
                    }
                    if (prefab != null) break;
                }
            }

            if (prefab == null)
            {
                Plugin.Log.LogWarning($"Bundle loaded, but no prefab named '{prefabName}' " +
                    $"(or containing: {string.Join("/", nameKeywords)}) found - using placeholder. " +
                    $"Bundle contents: {string.Join(", ", _bundle.GetAllAssetNames())}");
                return null;
            }

            GameObject instance = Object.Instantiate(prefab);
            LogBounds(instance, prefab.name);
            return instance;
        }

        private static void LogBounds(GameObject instance, string prefabName)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Plugin.Log.LogWarning($"Bundle prefab '{prefabName}' has no renderers (invisible?).");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            Plugin.Log.LogInfo($"Bundle prefab '{prefabName}' size: {bounds.size} m. " +
                "(Zielgröße: Truhe ~1.0m breit, Terminal ~0.6m - sonst in Unity den Scale Factor anpassen.)");
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            var renderer = part.GetComponent<MeshRenderer>();
            renderer.material = new Material(BaseMaterial()) { color = color };
            return part;
        }

        private static void RemovePartColliders(params GameObject[] parts)
        {
            foreach (GameObject part in parts)
            {
                Collider c = part.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }
        }

        public static Material MakeEmissive(GameObject part, Color color, float intensity)
        {
            var renderer = part.GetComponent<MeshRenderer>();
            Material mat = renderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            return mat;
        }

        private static Material BaseMaterial()
        {
            if (_baseMaterial != null) return _baseMaterial;

            Shader shader = Shader.Find("Standard");
            if (shader != null)
            {
                _baseMaterial = new Material(shader);
                return _baseMaterial;
            }

            // Standard may be stripped from the build; borrow a material from
            // an arbitrary scene renderer instead.
            var anyRenderer = Object.FindObjectOfType<MeshRenderer>();
            _baseMaterial = anyRenderer != null
                ? new Material(anyRenderer.sharedMaterial)
                : new Material(Shader.Find("Sprites/Default"));
            return _baseMaterial;
        }
    }
}
