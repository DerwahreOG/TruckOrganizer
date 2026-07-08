using System.Collections;
using UnityEngine;

namespace TruckOrganizer.Behaviours
{
    /// <summary>
    /// CRT style boot animation for the terminal display. Based on the
    /// user's Unity script; references are wired at runtime by AssetFactory
    /// (placeholder) or resolved from the asset bundle prefab.
    /// </summary>
    public class TerminalScreen : MonoBehaviour
    {
        public Material screenMat;
        public Light screenLight;

        private static readonly Color ScreenGreen = new Color(0.1f, 1f, 0.35f);

        public void PowerOn()
        {
            if (screenMat == null) return;
            StartCoroutine(BootFlicker());
        }

        private IEnumerator BootFlicker()
        {
            screenMat.EnableKeyword("_EMISSION");
            if (screenLight != null) screenLight.enabled = true;

            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float v = Mathf.Clamp01(t / 0.6f);
                if (Random.value < 0.15f) v *= 0.2f; // CRT flicker
                screenMat.SetColor("_EmissionColor", ScreenGreen * v * 2f);
                if (screenLight != null) screenLight.intensity = v * 1.5f;
                yield return null;
            }

            screenMat.SetColor("_EmissionColor", ScreenGreen * 2f);
            if (screenLight != null) screenLight.intensity = 1.5f;
        }
    }
}
