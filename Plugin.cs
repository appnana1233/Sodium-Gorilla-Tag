using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace Sodium
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }
        internal bool DeferSharedBlocksStartup { get; private set; }
        internal bool ReduceDistantCosmetics { get; private set; }
        internal bool NoAA { get; private set; }
        internal float CosmeticFullDetailDistance { get; private set; }
        internal float CosmeticMaximumDistance { get; private set; }
        internal int CosmeticFullDetailCount { get; private set; }
        internal bool NoReflections { get; private set; }
        internal int CosmeticMaximumCount { get; private set; }
        internal bool SkipForcedLeaveGarbageCollection { get; private set; }
        internal bool SkipTimedUnusedAssetUnloads { get; private set; }
        internal bool EyeExpressions { get; private set; }
        internal bool StaggerScoreboardCreation { get; private set; }
        internal float LODBias { get; private set; }
        internal float TextureTransitionRate { get; private set; }
        internal bool OptimizeCustomMapSanitizer { get; private set; }
        internal float VoiceReactiveVisualRate { get; private set; }
        internal float EnvironmentProximityRate { get; private set; }
        internal bool NoShadows { get; private set; }
        internal bool StaggerRemoteRigPoolStartup { get; private set; }
        internal bool MouthFlap { get; private set; }
        internal int InitialRemoteRigPoolSize { get; private set; }
        internal bool SkipInactiveEntityLeaveScans { get; private set; }
        internal bool StaggerScoreboardRefresh { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Instance = this;
            DeferSharedBlocksStartup = Config.Bind("Startup", "Defer Shared Blocks startup", true,
                "Do not initialise Shared Blocks until the player enters its zone.").Value;
            StaggerRemoteRigPoolStartup = Config.Bind("Startup", "Stagger remote rig pool startup", true,
                "Creates the remote-player rig pool over several frames instead of constructing all 19 rigs in one startup frame.").Value;
            InitialRemoteRigPoolSize = Config.Bind("Startup", "Initial remote rig pool size", 4,
                "Number of remote-player rigs created immediately. Remaining rigs are warmed one per frame and are also created on demand.").Value;
            LODBias = Config.Bind("Quality", "LOD bias", 0.34f,
    "Lower values switch to low-detail models sooner for more FPS. Set to 0 to leave the game's value unchanged.").Value;
            NoShadows = Config.Bind("Quality", "Disable shadows", true,
    "Turns off realtime shadows for more FPS. Objects will no longer cast shadows.").Value;
            NoAA = Config.Bind("Quality", "Disable anti-aliasing", false,
    "Turns off anti-aliasing for more FPS. Edges will look more jagged, especially in VR.").Value;
            NoReflections = Config.Bind("Quality", "Disable realtime reflection probes", true,
    "Turns off realtime reflection probes. Mainly useful for custom maps that use them. Reflective surfaces may look flatter.").Value;
            ReduceDistantCosmetics = Config.Bind("Rendering", "Reduce distant cosmetic detail", true,
                "Locally culls distant remote-player cosmetics. Does not modify networking or other players.").Value;
            CosmeticFullDetailDistance = Config.Bind("Rendering", "Full-detail cosmetic distance", 4.5f).Value;
            CosmeticMaximumDistance = Config.Bind("Rendering", "Maximum cosmetic distance", 8f).Value;
            CosmeticFullDetailCount = Config.Bind("Rendering", "Full-detail player count", 5).Value;
            CosmeticMaximumCount = Config.Bind("Rendering", "Maximum detailed player count", 10).Value;
            TextureTransitionRate = Config.Bind("Rendering", "Texture transitions per second", 30f,
                "Limits purely visual texture-transition updates. Set to 0 to leave the game rate unchanged.").Value;
            MouthFlap = Config.Bind("Rendering", "Cache mouth flap updates", true,
    "Caches the face material and skips redundant texture offset updates for mouth animations.").Value;
            EyeExpressions = Config.Bind("Rendering", "Cache eye expressions", true,
    "Caches the face renderer and skips redundant eye UV updates.").Value;
            VoiceReactiveVisualRate = Config.Bind("Rendering", "Voice-reactive visuals per second", 30f,
                "Limits cosmetic voice-reactive visual updates when they do not use attack or decay smoothing. Set to 0 to leave the game rate unchanged.").Value;
            EnvironmentProximityRate = Config.Bind("Rendering", "Environment proximity checks per second", 30f,
                "Limits cosmetic-to-environment proximity checks. Network state changes remain intact but can be delayed by one update interval. Set to 0 to leave the game rate unchanged.").Value;
            OptimizeCustomMapSanitizer = Config.Bind("Maps", "Optimize custom-map sanitizer", true,
                "Uses cached constant-time allow-list lookups while preserving the game's component validation rules.").Value;
            SkipForcedLeaveGarbageCollection = Config.Bind("Stutter", "Skip forced collection on room leave", true,
                "Lets the managed runtime choose when to collect instead of forcing a collection during every room leave.").Value;
            SkipTimedUnusedAssetUnloads = Config.Bind("Stutter", "Skip timed unused-asset unloads", true,
                "Prevents scene components from forcing a synchronous unused-asset sweep during gameplay. May use more memory.").Value;
            StaggerScoreboardCreation = Config.Bind("Stutter", "Stagger scoreboard creation on room join", true,
                "Creates active local scoreboards over successive frames instead of all in the room-join frame.").Value;
            StaggerScoreboardRefresh = Config.Bind("Stutter", "Stagger scoreboard refreshes", true,
                "Refreshes multiple scoreboards over successive frames when another player joins or leaves.").Value;
            SkipInactiveEntityLeaveScans = Config.Bind("Stutter", "Skip inactive-zone entity leave scans", true,
                "Skips unused full entity scans performed by every inactive zone when a remote player leaves.").Value;

            bool patched = HarmonyPatches.ApplyHarmonyPatches();
            if (patched)
            {
                gameObject.AddComponent<MonkeBlocksStuff>();
                gameObject.AddComponent<RoomJoinStuff>();
            }
            gameObject.AddComponent<LODBiasStuff>();
            gameObject.AddComponent<NoShadowsStuff>();
            gameObject.AddComponent<NoAAStuff>();
            gameObject.AddComponent<NoReflectionsStuff>();

            if (patched)
                Logger.LogInfo($"Sodium {PluginInfo.Version} loaded successfully!");
            else
                Logger.LogWarning("Sodium's patches failed to apply and were rolled back. Quality settings still apply. See the error above.");
        }

        private void OnDestroy()
        {
            HarmonyPatches.RemoveHarmonyPatches();
            Instance = null;
        }
    }
}
