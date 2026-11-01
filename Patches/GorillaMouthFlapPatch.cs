// Code By Gorilla-Optimizer, made by sevvy-wevvy https://github.com/sevvy-wevvy/Gorilla-Optimizer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using GorillaTag;
using GorillaTag.Cosmetics;
using HarmonyLib;
using UnityEngine;

namespace Sodium
{
    [HarmonyPatch(typeof(GorillaMouthFlap), "UpdateMouthFlapFlipbook")]
    static class MouthFlapPatch
    {
        class Cache
        {
            public Renderer renderer;
            public Material mat;
            public int lastIdx = -1;
            public Vector2[] lastFaces;  // compare by ref since its always same array instance per level
        }

        static readonly ConditionalWeakTable<GorillaMouthFlap, Cache> caches =
            new ConditionalWeakTable<GorillaMouthFlap, Cache>();

        static readonly int mouthMapId = Shader.PropertyToID("_MouthMap");

        [HarmonyPrefix]
        static bool Prefix(GorillaMouthFlap __instance, MouthFlapLevel mouthFlap,
            ref float ___activeFlipbookPlayTime,
            float ___deltaTime,
            Renderer ___targetFaceRenderer)
        {
            // toggle and safety checks first, before touching any state
            if (!(Plugin.Instance?.MouthFlap ?? false)) return true;
            if (___targetFaceRenderer == null || mouthFlap.faces == null
                || mouthFlap.faces.Length == 0 || mouthFlap.cycleDuration <= 0f)
                return true;

            // advance time same as original
            ___activeFlipbookPlayTime += ___deltaTime;
            ___activeFlipbookPlayTime %= mouthFlap.cycleDuration;

            int len = mouthFlap.faces.Length;
            int idx = Mathf.FloorToInt(___activeFlipbookPlayTime * len / mouthFlap.cycleDuration);
            if (idx >= len) idx = len - 1;
            if (idx < 0) idx = 0;

            var c = caches.GetOrCreateValue(__instance);

            // rebuild the cache if the material is gone or the renderer changed
            if (c.mat == null || c.renderer != ___targetFaceRenderer)
            {
                c.renderer = ___targetFaceRenderer;
                c.mat = ___targetFaceRenderer.material;
                c.lastIdx = -1;
                c.lastFaces = null;
            }

            // skip the gpu call if frame didnt change
            if (idx == c.lastIdx && mouthFlap.faces == c.lastFaces)
                return false;

            c.lastIdx = idx;
            c.lastFaces = mouthFlap.faces;
            c.mat.SetTextureOffset(mouthMapId, mouthFlap.faces[idx]);

            return false;  // we handled it
        }
    }
}