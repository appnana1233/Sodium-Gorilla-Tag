// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Sodium
{
    [HarmonyPatch(typeof(VRRigCache), nameof(VRRigCache.InitializeVRRigCache))]
    internal static class RigPoolPatch
    {
        private static readonly FieldInfo FreeRigsField = AccessTools.Field(typeof(VRRigCache), "freeRigs");
        private static readonly FieldInfo RigsInUseField = AccessTools.Field(typeof(VRRigCache), "rigsInUse");
        private static readonly FieldInfo PostSpawnRigEventField = AccessTools.Field(typeof(VRRigCache), "OnPostSpawnRig");
        private static readonly MethodInfo SpawnRigMethod = AccessTools.Method(typeof(VRRigCache), "SpawnRig");
        private static int failures;
        private static VRRigCache cache;
        private static int targetRemoteRigCount;
        private static bool warmupFailed;

        private static void Prefix(ref int ___rigAmount)
        {
            if (!(Plugin.Instance?.StaggerRemoteRigPoolStartup ?? false))
                return;

            targetRemoteRigCount = ___rigAmount;
            ___rigAmount = Mathf.Clamp(Plugin.Instance.InitialRemoteRigPoolSize, 1, targetRemoteRigCount);
        }

        private static void Postfix(VRRigCache __instance)
        {
            if (Plugin.Instance?.StaggerRemoteRigPoolStartup == true && VRRigCache.isInitialized)
                cache = __instance;
        }

        internal static void WarmNext()
        {
            if (warmupFailed || cache == null || !VRRigCache.isInitialized || targetRemoteRigCount <= 0)
                return;

            if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom)
                return;

            if (VRRigCache.AllRigContainers.Count - 1 >= targetRemoteRigCount)
            {
                cache = null;
                return;
            }

            SpawnOne(cache);
        }

        internal static void EnsureAvailable(VRRigCache instance, NetPlayer targetPlayer)
        {
            if (warmupFailed || !(Plugin.Instance?.StaggerRemoteRigPoolStartup ?? false) || targetPlayer == null || targetPlayer.IsLocal || !targetPlayer.InRoom)
                return;

            Queue<RigContainer> freeRigs = (Queue<RigContainer>)FreeRigsField.GetValue(null);
            Dictionary<NetPlayer, RigContainer> rigsInUse =
                (Dictionary<NetPlayer, RigContainer>)RigsInUseField.GetValue(null);
            if (freeRigs.Count == 0 && !rigsInUse.ContainsKey(targetPlayer))
                SpawnOne(instance);
        }

        private static void SpawnOne(VRRigCache instance)
        {
            RigContainer rc = null;
            try
            {
                rc = (RigContainer)SpawnRigMethod.Invoke(instance, null);
                if (rc == null) throw new InvalidOperationException("SpawnRig returned null");
                ((Queue<RigContainer>)FreeRigsField.GetValue(null)).Enqueue(rc);
                rc.Rig.BuildInitialize();
                rc.Rig.transform.parent = null;
                (PostSpawnRigEventField?.GetValue(null) as Action)?.Invoke();
                failures = 0;
            }
            catch (Exception e)
            {
                Discard(rc);
                if (++failures >= 3) warmupFailed = true;   // retry a few times before giving up
                Debug.LogError($"remote rig pool warmup failed ({failures}): {(e.InnerException ?? e)}");
            }
        }

        private static void Discard(RigContainer rc)
        {
            if (rc == null) return;
            var q = (Queue<RigContainer>)FreeRigsField.GetValue(null);
            RigContainer[] keep = q.Where(r => r != rc).ToArray();   // needs using System.Linq;
            q.Clear();
            foreach (RigContainer r in keep) q.Enqueue(r);
            if (rc.Rig != null) UnityEngine.Object.Destroy(rc.Rig.gameObject);
        }

        [HarmonyPatch]
        internal static class VRRigPoolOnDemandPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(VRRigCache), "TryGetVrrig",
                    new[] { typeof(NetPlayer), typeof(RigContainer).MakeByRefType() });
            }

            private static void Prefix(VRRigCache __instance, NetPlayer targetPlayer)
            {
                RigPoolPatch.EnsureAvailable(__instance, targetPlayer);
            }
        }
    }
}
