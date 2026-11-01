// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using GorillaTagScripts.Builder;
using HarmonyLib;

namespace Sodium
{
    [HarmonyPatch(typeof(SharedBlocksManager), "Start")]
    internal static class BlocksPatch
    {
        private static SharedBlocksManager deferredManager;
        private static bool allowOriginalStart;

        internal static bool HasDeferredManager => deferredManager != null;

        internal static SharedBlocksManager TakeDeferredManager()
        {
            SharedBlocksManager manager = deferredManager;
            deferredManager = null;
            allowOriginalStart = true;
            return manager;
        }

        private static bool Prefix(SharedBlocksManager __instance)
        {
            if (allowOriginalStart)
            {
                allowOriginalStart = false;
                return true;
            }

            if (Plugin.Instance == null || !Plugin.Instance.DeferSharedBlocksStartup ||
                (ZoneManagement.instance != null && ZoneManagement.instance.IsZoneActive(GTZone.monkeBlocksShared)))
                return true;

            deferredManager = __instance;
            return false;
        }
    }
}
