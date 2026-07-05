using TruckOrganizer.Core;
using UnityEngine;

namespace TruckOrganizer.Behaviours
{
    /// <summary>
    /// Attached to the chest and the terminal. Shows an interaction hint when
    /// the local player is close and looking roughly at the object, and opens
    /// the storage menu on key press.
    /// </summary>
    public class StorageContainer : MonoBehaviour
    {
        public string label = "Lager";

        private const float LookDotThreshold = 0.45f;

        public bool LocalPlayerCanInteract()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;

            Vector3 focus = transform.position + Vector3.up * 0.4f;
            Vector3 toContainer = focus - cam.transform.position;
            if (toContainer.magnitude > Plugin.InteractRange.Value) return false;

            return Vector3.Dot(cam.transform.forward, toContainer.normalized) > LookDotThreshold;
        }

        /// <summary>Distance-only check used while the menu is open, so the
        /// menu does not close just because the view direction changes.</summary>
        public bool LocalPlayerWithinRange()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector3 focus = transform.position + Vector3.up * 0.4f;
            return (focus - cam.transform.position).magnitude <= Plugin.InteractRange.Value + 0.8f;
        }

        private void Update()
        {
            if (StorageMenu.IsOpen) return;
            if (!LocalPlayerCanInteract())
            {
                if (StorageMenu.HintSource == this) StorageMenu.HintSource = null;
                return;
            }

            StorageMenu.HintSource = this;

            if (Input.GetKeyDown(Plugin.InteractKey.Value))
            {
                StorageMenu.Open(this);
            }
        }

        private void OnDestroy()
        {
            if (StorageMenu.HintSource == this) StorageMenu.HintSource = null;
            StorageMenu.CloseIfSource(this);
        }
    }
}
