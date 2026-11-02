// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using System.Collections.Generic;
using HarmonyLib;

namespace Sodium
{
    [HarmonyPatch(typeof(GorillaScoreboardSpawner), nameof(GorillaScoreboardSpawner.OnJoinedRoom))]
    internal static class RoomJoinPatch
    {
        private static readonly Queue<KeyValuePair<int, GorillaScoreboardSpawner>> Pending =
            new Queue<KeyValuePair<int, GorillaScoreboardSpawner>>();
        private static readonly HashSet<int> PendingIds = new HashSet<int>();
        private static readonly HashSet<int> AllowedIds = new HashSet<int>();

        private static bool Prefix(GorillaScoreboardSpawner __instance)
        {
            if (Plugin.Instance == null || !Plugin.Instance.StaggerScoreboardCreation)
                return true;

            int id = __instance.GetInstanceID();
            if (AllowedIds.Remove(id))
                return true;

            if (!__instance.gameObject.activeInHierarchy)
                return true;

            if (PendingIds.Add(id))
                Pending.Enqueue(new KeyValuePair<int, GorillaScoreboardSpawner>(id, __instance));
            return false;
        }

        internal static void RunNext()
        {
            if (Pending.Count == 0 || NetworkSystem.Instance == null || !NetworkSystem.Instance.InRoom)
                return;

            KeyValuePair<int, GorillaScoreboardSpawner> entry = Pending.Dequeue();
            PendingIds.Remove(entry.Key);
            if (entry.Value == null)
                return;

            AllowedIds.Add(entry.Key);
            try
            {
                entry.Value.OnJoinedRoom();
            }
            finally
            {
                AllowedIds.Remove(entry.Key);
            }
        }

        internal static void Clear()
        {
            Pending.Clear();
            PendingIds.Clear();
            AllowedIds.Clear();
        }
    }
}
