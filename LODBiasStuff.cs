// Code By Gorilla-Optimizer, made by sevvy-wevvy https://github.com/sevvy-wevvy/Gorilla-Optimizer/

using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx.Configuration;

namespace Sodium
{
    internal sealed class LODBiasStuff : MonoBehaviour
    {
        private const float LowBias = 0.34f;
        private float originalLodBias;

        private void Awake()
        {
            originalLodBias = QualitySettings.lodBias;
        }

        private void Start()
        {
            Apply();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            QualitySettings.lodBias = originalLodBias;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private void Apply()
        {
            if (Plugin.Instance == null)
                return;

            float bias = Plugin.Instance.LODBias;
            if (bias > 0f)
                QualitySettings.lodBias = Mathf.Clamp(bias, 0.1f, 4f);
        }
    }
}
