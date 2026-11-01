// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Sodium
{
    [HarmonyPatch(typeof(VoiceLoudnessReactor), "Update")]
    internal static class VoiceLoudnessReactorPatch
    {
        private sealed class UpdateState
        {
            internal float NextUpdateTime;
        }

        private static readonly AccessTools.FieldRef<VoiceLoudnessReactor, float> Attack =
            AccessTools.FieldRefAccess<VoiceLoudnessReactor, float>("attack");
        private static readonly AccessTools.FieldRef<VoiceLoudnessReactor, float> Decay =
            AccessTools.FieldRefAccess<VoiceLoudnessReactor, float>("decay");
        private static readonly ConditionalWeakTable<VoiceLoudnessReactor, UpdateState> States =
            new ConditionalWeakTable<VoiceLoudnessReactor, UpdateState>();

        private static bool Prefix(VoiceLoudnessReactor __instance)
        {
            float rate = Plugin.Instance?.VoiceReactiveVisualRate ?? 0f;
            if (rate <= 0f)
                return true;

            if (Attack(__instance) > 0f || Decay(__instance) > 0f)
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
