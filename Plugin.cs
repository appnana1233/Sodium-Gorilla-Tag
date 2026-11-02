using BepInEx;
using BepInEx.Configuration;
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
        internal bool NoAA { get; private set; }
        internal bool NoReflections { get; private set; }
        internal bool LowestShadowResolution { get; private set; }
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
        internal bool MemoryWatchdog { get; private set; }
        internal int MemoryWatchdogThreshold { get; private set; }
        internal float MemoryWatchdogCheckInterval { get; private set; }
        internal float MemoryWatchdogCooldown { get; private set; }
        private ConfigEntry<bool> disableCosmeticParticlesEntry;
        private ConfigEntry<float> particleScanIntervalEntry;
        internal bool DisableCosmeticParticles => disableCosmeticParticlesEntry != null && disableCosmeticParticlesEntry.Value;
        internal float ParticleScanInterval => particleScanIntervalEntry != null ? particleScanIntervalEntry.Value : 2f;

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
            LowestShadowResolution = Config.Bind("Quality", "Lowest shadow resolution", true,
                "Sets the shadow map resolution to the lowest option for more FPS. Shadows will look blurrier. Only matters while shadows are enabled.").Value;
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
            MemoryWatchdog = Config.Bind("Memory", "Enable memory watchdog", true,
                "Runs a garbage collection when system RAM usage reaches the threshold, then an unused-asset unload if that does not help. Useful since skipped forced collections and asset unloads can let memory climb. Windows only.").Value;
            MemoryWatchdogThreshold = Config.Bind("Memory", "Memory watchdog threshold (%)", 90,
                "System RAM usage percentage at or above which a garbage collection is triggered. Clamped to 50-99.").Value;
            MemoryWatchdogCheckInterval = Config.Bind("Memory", "Memory watchdog check interval (seconds)", 5f,
                "How often RAM usage is checked. Minimum 1 second.").Value;
            MemoryWatchdogCooldown = Config.Bind("Memory", "Memory watchdog cooldown (seconds)", 60f,
                "Minimum time between triggered cleanups, so a system that stays above the threshold does not clean up repeatedly. The wait doubles after each cleanup that fails to lower RAM, up to 10 minutes, and resets once RAM drops below the threshold or a cleanup works. Minimum 5 seconds.").Value;
            disableCosmeticParticlesEntry = Config.Bind("Particles", "Disable cosmetic particles", false,
                "Turns off particle effects on player cosmetics for more FPS. Can be changed while the game is running, and the effects come back when this is turned off.");
            particleScanIntervalEntry = Config.Bind("Particles", "Particle scan interval (seconds)", 2f,
                "How often newly spawned particle systems are found and switched off. Lower values catch new effects sooner but cost more CPU. Minimum 0.5 seconds.");

            if (SkipTimedUnusedAssetUnloads && !MemoryWatchdog)
                Logger.LogWarning("Timed unused-asset unloads are skipped and the memory watchdog is off, so nothing will unload unused assets between scene changes. Memory use can keep growing.");

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
            gameObject.AddComponent<ShadowResolutionStuff>();
            gameObject.AddComponent<MemoryWatchdogStuff>();
            gameObject.AddComponent<ParticleStuff>();
            if (patched)
                Logger.LogInfo($"Sodium {PluginInfo.Version} was loaded successfully! Have fun!");
            else
                Logger.LogWarning("Sodium's patches failed to apply and were rolled back. Quality settings still apply. See the error above and report it to the discord.");
        }

        private void OnDestroy()
        {
            HarmonyPatches.RemoveHarmonyPatches();
            Instance = null;
        }
    }
}