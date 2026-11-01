// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using GorillaTag.Cosmetics;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Sodium
{
    [HarmonyPatch(typeof(EnvironmentProximityReactor), "Update")]
    internal static class ProximityPatch
    {
        private sealed class UpdateState
        {
            internal float NextUpdateTime;
        }

        private static readonly ConditionalWeakTable<EnvironmentProximityReactor, UpdateState> States =
            new ConditionalWeakTable<EnvironmentProximityReactor, UpdateState>();

        private static bool Prefix(EnvironmentProximityReactor __instance)
        {
            float rate = Plugin.Instance?.EnvironmentProximityRate ?? 0f;
            if (rate <= 0f)
                return true;

            UpdateState state = States.GetOrCreateValue(__instance);
            float now = Time.unscaledTime;
            if (now < state.NextUpdateTime)
                return false;

            state.NextUpdateTime = now + 1f / Mathf.Clamp(rate, 1f, 120f);
            return true;
        }
    }
}
