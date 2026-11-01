// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace Sodium
{
    [HarmonyPatch(typeof(GorillaScoreboardTotalUpdater), nameof(GorillaScoreboardTotalUpdater.UpdateActiveScoreboards))]
    internal static class ScoreboardRefreshPatch
    {
        private static readonly Queue<KeyValuePair<int, GorillaScoreBoard>> Pending =
            new Queue<KeyValuePair<int, GorillaScoreBoard>>();
        private static readonly HashSet<int> PendingIds = new HashSet<int>();

        private static bool Prefix()
        {
            if (!(Plugin.Instance?.StaggerScoreboardRefresh ?? false))
                return true;

            List<GorillaScoreBoard> scoreboards = GorillaScoreboardTotalUpdater.allScoreboards;
            for (int i = 0; i < scoreboards.Count; i++)
            {
                GorillaScoreBoard scoreboard = scoreboards[i];
                if (scoreboard == null)
                    continue;

                int id = scoreboard.GetInstanceID();
                if (PendingIds.Add(id))
                    Pending.Enqueue(new KeyValuePair<int, GorillaScoreBoard>(id, scoreboard));
            }

            return false;
        }

        internal static void RunNext()
        {
            while (Pending.Count > 0)
            {
                KeyValuePair<int, GorillaScoreBoard> entry = Pending.Dequeue();
                PendingIds.Remove(entry.Key);
                if (entry.Value == null)
                    continue;

                if (GorillaScoreboardTotalUpdater.instance != null)
                    GorillaScoreboardTotalUpdater.instance.UpdateScoreboard(entry.Value);
                break;
            }
        }

        internal static void Clear()
        {
            Pending.Clear();
            PendingIds.Clear();
        }
    }
}