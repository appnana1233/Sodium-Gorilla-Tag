// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Sodium
{
    [HarmonyPatch]
    internal static class RoomTransitionPatch
    {
        private static readonly MethodInfo CollectMethod = AccessTools.Method(typeof(GC), nameof(GC.Collect), new[] { typeof(int) });
        private static readonly MethodInfo MaybeCollectMethod = AccessTools.Method(typeof(RoomTransitionPatch), nameof(MaybeCollect));

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("RoomSystem"), "OnLeftRoom");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && Equals(instruction.operand, CollectMethod))
                    yield return new CodeInstruction(OpCodes.Call, MaybeCollectMethod).MoveLabelsFrom(instruction);
                else
                    yield return instruction;
            }
        }

        private static void Postfix()
        {
            RoomJoinPatch.Clear();
            ScoreboardRefreshPatch.Clear();
        }

        private static void MaybeCollect(int generation)
        {
            if (Plugin.Instance == null || !Plugin.Instance.SkipForcedLeaveGarbageCollection)
                GC.Collect(generation);
        }
    }
}
