using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sodium
{
    internal sealed class ParticleStuff : MonoBehaviour
    {
        private const int RigsPerFrame = 2;

        private sealed class State
        {
            internal bool WasPlaying;
            internal bool EmissionWasEnabled;
            internal bool RendererWasEnabled;
        }

        private readonly Dictionary<ParticleSystem, State> tracked = new Dictionary<ParticleSystem, State>();
        private readonly List<ParticleSystem> buffer = new List<ParticleSystem>();
        private readonly List<ParticleSystem> scratch = new List<ParticleSystem>();
        private VRRig[] pendingRigs;
        private int pendingIndex;
        private bool applied;
        private float nextScanTime;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            pendingRigs = null;
            RestoreAll();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            pendingRigs = null;
            nextScanTime = 0f;
        }

        private void Update()
        {
            Plugin plugin = Plugin.Instance;
            bool wanted = plugin != null && plugin.DisableCosmeticParticles;

            if (wanted != applied)
            {
                if (!wanted)
                {
                    pendingRigs = null;
                    RestoreAll();
                }

                applied = wanted;
                nextScanTime = 0f;
            }

            if (!wanted)
                return;

            if (pendingRigs != null)
            {
                ScanChunk();
                return;
            }

            float now = Time.unscaledTime;
            if (now < nextScanTime)
                return;

            nextScanTime = now + Mathf.Max(0.5f, plugin.ParticleScanInterval);
            BeginScan();
        }

        private void BeginScan()
        {
            PurgeDestroyed();

            VRRig[] rigs = FindObjectsByType<VRRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (rigs.Length == 0)
                return;

            pendingRigs = rigs;
            pendingIndex = 0;
            ScanChunk();
        }

        private void ScanChunk()
        {
            int end = Mathf.Min(pendingIndex + RigsPerFrame, pendingRigs.Length);
            while (pendingIndex < end)
            {
                VRRig rig = pendingRigs[pendingIndex++];
                if (rig == null)
                    continue;

                buffer.Clear();
                rig.GetComponentsInChildren(true, buffer);
                for (int i = 0; i < buffer.Count; i++)
                    Suppress(buffer[i]);
            }

            buffer.Clear();
            if (pendingIndex >= pendingRigs.Length)
                pendingRigs = null;
        }

        private void PurgeDestroyed()
        {
            scratch.Clear();
            foreach (ParticleSystem system in tracked.Keys)
            {
                if (system == null)
                    scratch.Add(system);
            }

            for (int i = 0; i < scratch.Count; i++)
                tracked.Remove(scratch[i]);

            scratch.Clear();
        }

        private void Suppress(ParticleSystem system)
        {
            if (system == null || tracked.ContainsKey(system))
                return;

            State state = new State { WasPlaying = system.isPlaying };

            ParticleSystem.EmissionModule emission = system.emission;
            state.EmissionWasEnabled = emission.enabled;

            ParticleSystemRenderer systemRenderer = system.GetComponent<ParticleSystemRenderer>();
            state.RendererWasEnabled = systemRenderer != null && systemRenderer.enabled;

            emission.enabled = false;
            if (systemRenderer != null)
                systemRenderer.enabled = false;
            if (state.WasPlaying)
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            tracked[system] = state;
        }

        private void RestoreAll()
        {
            scratch.Clear();
            foreach (ParticleSystem system in tracked.Keys)
                scratch.Add(system);

            for (int i = 0; i < scratch.Count; i++)
                Restore(scratch[i]);

            scratch.Clear();
            tracked.Clear();
        }

        private void Restore(ParticleSystem system)
        {
            if (system == null || !tracked.TryGetValue(system, out State state))
                return;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = state.EmissionWasEnabled;

            ParticleSystemRenderer systemRenderer = system.GetComponent<ParticleSystemRenderer>();
            if (systemRenderer != null)
                systemRenderer.enabled = state.RendererWasEnabled;

            if (state.WasPlaying && system.gameObject.activeInHierarchy)
                system.Play(false);
        }
    }
}
