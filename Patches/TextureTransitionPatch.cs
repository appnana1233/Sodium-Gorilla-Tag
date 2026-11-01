// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using System.Runtime.CompilerServices;
using GorillaTag;
using HarmonyLib;
using UnityEngine;

namespace Sodium
{
    [HarmonyPatch(typeof(TextureTransitionerManager), "LateUpdate")]
    internal static class TextureTransitionPatch
    {
        private sealed class State
        {
            internal float Next;
        }

        private static readonly ConditionalWeakTable<TextureTransitionerManager, State> States =
            new ConditionalWeakTable<TextureTransitionerManager, State>();

        private static bool Prefix(TextureTransitionerManager __instance)
        {
            float rate = Plugin.Instance?.TextureTransitionRate ?? 0f;
            if (rate <= 0f)
                return true;

            State s = States.GetOrCreateValue(__instance);
            float now = Time.unscaledTime;
            if (now < s.Next)
                return false;

            s.Next = now + 1f / Mathf.Clamp(rate, 1f, 120f);
            return true;
        }
    }
}
