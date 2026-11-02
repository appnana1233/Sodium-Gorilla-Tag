using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sodium
{
    internal sealed class ParticleStuff : MonoBehaviour
    {
        private sealed class State
        {
            internal bool WasPlaying;
            internal bool EmissionWasEnabled;
            internal bool RendererWasEnabled;
        }

        private readonly Dictionary<ParticleSystem, State> tracked = new Dictionary<ParticleSystem, State>();
        private readonly List<ParticleSystem> buffer = new List<ParticleSystem>();
        private readonly List<ParticleSystem> scratch = new List<ParticleSystem>();
        private bool applied;
        private float nextScanTime;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreAll();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            nextScanTime = 0f;
        }

        private void Update()
        {
            Plugin plugin = Plugin.Instance;
            bool wanted = plugin != null && plugin.DisableCosmeticParticles;

            if (wanted != applied)
            {
                if (!wanted)
                    RestoreAll();

                applied = wanted;
                nextScanTime = 0f;
            }

            if (!wanted)
                return;

            float now = Time.unscaledTime;
            if (now < nextScanTime)
                return;

            nextScanTime = now + Mathf.Max(0.5f, plugin.ParticleScanInterval);
            Scan();
        }

        private void Scan()
        {
            PurgeDestroyed();

            VRRig[] rigs = FindObjectsByType<VRRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < rigs.Length; i++)
            {
                buffer.Clear();
                rigs[i].GetComponentsInChildren(true, buffer);
                for (int j = 0; j < buffer.Count; j++)
                    Suppress(buffer[j]);
            }
            buffer.Clear();
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