// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using GorillaNetworking;
using HarmonyLib;
using UnityEngine;

namespace Sodium
{
    [HarmonyPatch(typeof(CosmeticsThrottler), "Awake")]
    internal static class CosmeticsThrottlerPatch
    {
        private static void Postfix(CosmeticsThrottler __instance)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null || !plugin.ReduceDistantCosmetics)
                return;

            __instance.DrawAllDistance = Mathf.Max(0f, plugin.CosmeticFullDetailDistance);
            __instance.MaxDrawDistance = Mathf.Max(__instance.DrawAllDistance, plugin.CosmeticMaximumDistance);
            __instance.DrawAllCount = Mathf.Max(0, plugin.CosmeticFullDetailCount);
            __instance.DrawMaxCount = Mathf.Max(__instance.DrawAllCount, plugin.CosmeticMaximumCount);
        }
    }
}
