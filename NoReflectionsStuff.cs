// Code By Gorilla-Optimizer, made by sevvy-wevvy https://github.com/sevvy-wevvy/Gorilla-Optimizer/

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sodium
{
    internal sealed class NoReflectionsStuff : MonoBehaviour
    {
        private bool originalReflectionProbes;

        private void Awake()
        {
            originalReflectionProbes = QualitySettings.realtimeReflectionProbes;
        }

        private void Start()
        {
            Apply();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            QualitySettings.realtimeReflectionProbes = originalReflectionProbes;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private void Apply()
        {
            if (Plugin.Instance != null && Plugin.Instance.NoReflections)
                QualitySettings.realtimeReflectionProbes = false;
        }
    }
}