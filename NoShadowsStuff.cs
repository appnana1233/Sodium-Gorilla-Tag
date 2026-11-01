// Code By Gorilla-Optimizer, made by sevvy-wevvy https://github.com/sevvy-wevvy/Gorilla-Optimizer/

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sodium
{
    internal sealed class NoShadowsStuff : MonoBehaviour
    {
        private ShadowQuality originalShadows;

        private void Awake()
        {
            originalShadows = QualitySettings.shadows;
        }

        private void Start()
        {
            Apply();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            QualitySettings.shadows = originalShadows;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private void Apply()
        {
            if (Plugin.Instance != null && Plugin.Instance.NoShadows)
                QualitySettings.shadows = ShadowQuality.Disable;
        }
    }
}