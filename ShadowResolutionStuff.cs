using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Sodium
{
    internal sealed class ShadowResolutionStuff : MonoBehaviour
    {
        private const int LowestPipelineResolution = 256;
        private const string MainLightProperty = "mainLightShadowmapResolution";
        private const string AdditionalLightProperty = "additionalLightsShadowmapResolution";

        private ShadowResolution originalResolution;
        private RenderPipelineAsset pipelineAsset;
        private int? originalMainLight;
        private int? originalAdditionalLight;
        private bool warned;

        private void Awake()
        {
            originalResolution = QualitySettings.shadowResolution;
        }

        private void Start()
        {
            Apply();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            QualitySettings.shadowResolution = originalResolution;
            RestorePipeline();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private void Apply()
        {
            if (Plugin.Instance == null || !Plugin.Instance.LowestShadowResolution)
                return;

            QualitySettings.shadowResolution = ShadowResolution.Low;
            ApplyToPipeline();
        }

        private void ApplyToPipeline()
        {
            RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null)
                return;

            if (asset != pipelineAsset)
            {
                RestorePipeline();
                pipelineAsset = asset;
            }

            bool changedMain = SetLowest(MainLightProperty, ref originalMainLight);
            bool changedAdditional = SetLowest(AdditionalLightProperty, ref originalAdditionalLight);

            if (!changedMain && !changedAdditional && originalMainLight == null && originalAdditionalLight == null && !warned)
            {
                warned = true;
                Plugin.Log?.LogWarning("Shadow resolution: could not find the shadow map resolution settings on the active render pipeline, only the quality setting was changed.");
            }
        }

        private bool SetLowest(string name, ref int? original)
        {
            PropertyInfo property = GetProperty(name);
            if (property == null)
                return false;

            int current = (int)property.GetValue(pipelineAsset);
            if (original == null)
                original = current;

            if (current == LowestPipelineResolution)
                return false;

            property.SetValue(pipelineAsset, LowestPipelineResolution);
            return true;
        }

        private void RestorePipeline()
        {
            if (pipelineAsset != null)
            {
                Restore(MainLightProperty, ref originalMainLight);
                Restore(AdditionalLightProperty, ref originalAdditionalLight);
            }

            originalMainLight = null;
            originalAdditionalLight = null;
            pipelineAsset = null;
        }

        private void Restore(string name, ref int? original)
        {
            if (original == null)
                return;

            PropertyInfo property = GetProperty(name);
            if (property != null)
                property.SetValue(pipelineAsset, original.Value);
        }

        private PropertyInfo GetProperty(string name)
        {
            PropertyInfo property = pipelineAsset.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (property == null || property.PropertyType != typeof(int) || !property.CanRead || !property.CanWrite)
                return null;

            return property;
        }
    }
}
