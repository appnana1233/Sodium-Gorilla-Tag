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
    [HarmonyPatch(typeof(GorillaEyeExpressions), "UpdateEyeExpression")]
    static class EyeExpressionPatch
    {
        class EyeCache
        {
            public GameObject face;
            public Renderer r;
            public Material mat;
            public Vector2 lastUV = new Vector2(-999f, -999f);  // impossible default forces first write
        }

        static readonly ConditionalWeakTable<GorillaEyeExpressions, EyeCache> caches =
            new ConditionalWeakTable<GorillaEyeExpressions, EyeCache>();

        static readonly int baseMapStId = Shader.PropertyToID("_BaseMap_ST");

        [HarmonyPrefix]
        static bool Prefix(GorillaEyeExpressions __instance, GameObject ___targetFace, Vector2 ___overrideUV)
        {
            if (!(Plugin.Instance?.EyeExpressions ?? false))
                return true;

            if (___targetFace == null)
                return true;

            var c = caches.GetOrCreateValue(__instance);

            // rebuild the cache if the face object changed or the renderer is gone
            if (c.face != ___targetFace || c.r == null)
            {
                c.face = ___targetFace;
                c.r = ___targetFace.GetComponent<Renderer>();
                c.mat = null;
            }

            if (c.r == null)
                return true;

            if (c.mat == null)
            {
                c.mat = c.r.material;
                c.lastUV = new Vector2(-999f, -999f);
            }

            // only push to gpu if the uv actually changed
            if (c.lastUV == ___overrideUV)
                return false;

            c.lastUV = ___overrideUV;
            c.mat.SetVector(baseMapStId, new Vector4(0.5f, 1f, ___overrideUV.x, ___overrideUV.y));
            return false;
        }
    }
}