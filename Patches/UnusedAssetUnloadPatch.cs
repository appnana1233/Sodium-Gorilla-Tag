// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using HarmonyLib;

namespace Sodium
{
    [HarmonyPatch(typeof(SimpleUnloadUnusedAssets), "OnEnable")]
    internal static class UnusedAssetUnloadPatch
    {
        private static bool Prefix()
        {
            return Plugin.Instance == null || !Plugin.Instance.SkipTimedUnusedAssetUnloads;
        }
    }
}
