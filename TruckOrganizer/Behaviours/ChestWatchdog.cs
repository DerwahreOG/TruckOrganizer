using TruckOrganizer.Core;
using UnityEngine;

namespace TruckOrganizer.Behaviours
{
    /// <summary>
    /// Safety net that does not depend on Harmony patches at all: polls the
    /// RoundDirector's completed-extraction counter and spawns the chest when
    /// it increases. Catches game updates that rename the patched methods.
    /// </summary>
    public class ChestWatchdog : MonoBehaviour
    {
        private int _lastCompleted;
        private float _nextPoll;

        public static void ResetOnSceneLoad(ChestWatchdog instance)
        {
            if (instance != null) instance._lastCompleted = -1;
        }

        private void Update()
        {
            if (Time.time < _nextPoll) return;
            _nextPoll = Time.time + 0.5f;

            try
            {
                RoundDirector director = RoundDirector.instance;
                if (director == null) return;

                int completed = director.extractionPointsCompleted;
                if (_lastCompleted < 0 || completed < _lastCompleted)
                {
                    // Fresh scene or counter reset; just adopt the value.
                    _lastCompleted = completed;
                    return;
                }

                if (completed > _lastCompleted)
                {
                    _lastCompleted = completed;
                    Plugin.Log.LogInfo($"Watchdog: extraction completed counter -> {completed}.");
                    ChestSpawner.OnExtractionCompleted(director.extractionPointCurrent);
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"Chest watchdog failed: {e.Message}");
            }
        }
    }
}
