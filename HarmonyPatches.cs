using HarmonyLib;
using System;
using System.Reflection;

namespace Sodium
{
    /// <summary>
    /// This class handles applying harmony patches to the game.
    /// You should not need to modify this class.
    /// </summary>
    public class HarmonyPatches
    {
        private static Harmony instance;

        public static bool IsPatched { get; private set; }
        public const string InstanceId = PluginInfo.GUID;

        internal static bool ApplyHarmonyPatches()
        {
            if (IsPatched) return true;
            if (instance == null) instance = new Harmony(InstanceId);
            try
            {
                instance.PatchAll(Assembly.GetExecutingAssembly());
                IsPatched = true;
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogError($"Sodium patching failed, rolling back: {e}");
                try { instance.UnpatchSelf(); } catch (Exception r) { Plugin.Log?.LogError(r.ToString()); }
                IsPatched = false;
                return false;
            }
        }

        internal static void RemoveHarmonyPatches()
        {
            if (instance == null) return;
            instance.UnpatchSelf();   // no IsPatched check
            IsPatched = false;
        }
    }
}
