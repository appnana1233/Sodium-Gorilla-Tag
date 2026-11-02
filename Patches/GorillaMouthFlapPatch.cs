// Code By Gorilla-Optimizer, made by sevvy-wevvy https://github.com/sevvy-wevvy/Gorilla-Optimizer/

using System.Runtime.CompilerServices;
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

            var c = caches.GetOrCreateValue(__instance);

            // rebuild the cache if the material is gone, the renderer changed,
            // or the game swapped the renderer's material out from under us
            if (c.mat == null || c.renderer != ___targetFaceRenderer
                || c.renderer.sharedMaterial != c.mat)
            {
                c.renderer = ___targetFaceRenderer;
                c.mat = OwnedMaterial.Acquire(___targetFaceRenderer);
                c.lastIdx = -1;
                c.lastFaces = null;
                if (c.mat == null) return true;
            }

            // advance time same as original
            ___activeFlipbookPlayTime += ___deltaTime;
            ___activeFlipbookPlayTime %= mouthFlap.cycleDuration;

            int len = mouthFlap.faces.Length;
            int idx = Mathf.FloorToInt(___activeFlipbookPlayTime * len / mouthFlap.cycleDuration);
            if (idx >= len) idx = len - 1;
            if (idx < 0) idx = 0;

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