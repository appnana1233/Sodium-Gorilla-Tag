// Code By Gorilla-Optimizer, made by sevvy-wevvy https://github.com/sevvy-wevvy/Gorilla-Optimizer/

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sodium
{
    internal sealed class NoAAStuff : MonoBehaviour
    {
        private int originalAntiAliasing;

        private void Awake()
        {
            originalAntiAliasing = QualitySettings.antiAliasing;
        }

        private void Start()
        {
            Apply();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            QualitySettings.antiAliasing = originalAntiAliasing;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private void Apply()
        {
            if (Plugin.Instance != null && Plugin.Instance.NoAA)
                QualitySettings.antiAliasing = 0;
        }
    }
}
