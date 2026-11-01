// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using HarmonyLib;
using System.Reflection;

namespace Sodium
{
    [HarmonyPatch]
    internal static class InactiveEntityLeavePatch
    {
        private static readonly PropertyInfo ActiveManagerProperty =
            AccessTools.Property(AccessTools.TypeByName("GameEntityManager"), "activeManager");

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("GameEntityManager"), "OnRigDeactivated");
        }

        private static bool Prefix(object __instance)
        {
            if (!(Plugin.Instance?.SkipInactiveEntityLeaveScans ?? false))
                return true;

            // The original inactive-manager branch scans every entity, increments a
            // local counter that is never read, and immediately returns.
            return ReferenceEquals(__instance, ActiveManagerProperty.GetValue(null));
        }
    }
}
