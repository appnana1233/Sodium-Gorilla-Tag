using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace Sodium
{
    internal sealed class MemoryWatchdogStuff : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

        private enum Phase { Idle, LightRunning, HardRunning, Settling }
        private const ulong IncrementalBudgetNs = 1000000UL;
        private const float SettleSeconds = 3f;
        private const float MaxPhaseSeconds = 30f;
        private const float MaxBackoffSeconds = 600f;
        private const int MaxBackoffDoublings = 10;
        private const int MinimumDropPercent = 1;

        private Phase phase = Phase.Idle;
        private int level;
        private int baselineLoad;
        private int failedCycles;
        private float settleUntil;
        private float phaseDeadline;
        private AsyncOperation unloadOp;
        private float nextCheckTime;
        private float nextAllowedCollectTime;
        private bool unsupported;

        private void Update()
        {
            if (unsupported || Plugin.Instance == null || !Plugin.Instance.MemoryWatchdog)
                return;

            switch (phase)
            {
                case Phase.Idle:
                    CheckThreshold();
                    break;

                case Phase.LightRunning:
                    if (Time.unscaledTime >= phaseDeadline)
                    {
                        Plugin.Log?.LogWarning("Memory watchdog: incremental collection did not finish in time, evaluating what it freed so far.");
                        BeginSettle();
                        break;
                    }

                    bool stillRunning;
                    try { stillRunning = GarbageCollector.CollectIncremental(IncrementalBudgetNs); }
                    catch (Exception e) { Abort(e); return; }
                    if (!stillRunning)
                        BeginSettle();
                    break;

                case Phase.HardRunning:
                    if (unloadOp == null || unloadOp.isDone)
                        BeginSettle();
                    else if (Time.unscaledTime >= phaseDeadline)
                    {
                        failedCycles++;
                        Plugin.Log?.LogWarning("Memory watchdog: unused-asset unload did not finish in time, giving up on this cleanup.");
                        FinishCycle();
                    }
                    break;

                case Phase.Settling:
                    if (Time.unscaledTime >= settleUntil)
                        Evaluate();
                    break;
            }
        }

        private void CheckThreshold()
        {
            float now = Time.unscaledTime;
            if (now < nextCheckTime)
                return;

            nextCheckTime = now + Mathf.Max(1f, Plugin.Instance.MemoryWatchdogCheckInterval);

            if (unloadOp != null)
            {
                if (!unloadOp.isDone)
                    return;
                unloadOp = null;
            }

            if (!TryGetMemoryLoad(out int load))
                return;

            int threshold = Mathf.Clamp(Plugin.Instance.MemoryWatchdogThreshold, 50, 99);
            if (load < threshold)
            {
                failedCycles = 0;
                return;
            }

            if (now < nextAllowedCollectTime)
                return;

            baselineLoad = load;
            Plugin.Log?.LogInfo($"Memory watchdog: system RAM at {load}% (threshold {threshold}%), running light cleanup...");
            StartLight();
        }

        private void StartLight()
        {
            level = 1;
            try
            {
                if (GarbageCollector.isIncremental)
                {
                    phaseDeadline = Time.unscaledTime + MaxPhaseSeconds;
                    phase = Phase.LightRunning;
                }
                else
                {
                    GC.Collect();
                    BeginSettle();
                }
            }
            catch (Exception e)
            {
                Abort(e);
            }
        }

        private void StartHard()
        {
            level = 2;
            try
            {
                unloadOp = Resources.UnloadUnusedAssets();
                phaseDeadline = Time.unscaledTime + MaxPhaseSeconds;
                phase = Phase.HardRunning;
            }
            catch (Exception e)
            {
                Abort(e);
            }
        }

        private void BeginSettle()
        {
            settleUntil = Time.unscaledTime + SettleSeconds;
            phase = Phase.Settling;
        }

        private void Evaluate()
        {
            if (!TryGetMemoryLoad(out int load))
            {
                FinishCycle();
                return;
            }

            int threshold = Mathf.Clamp(Plugin.Instance.MemoryWatchdogThreshold, 50, 99);
            bool dropped = load <= baselineLoad - MinimumDropPercent || load < threshold;

            if (dropped)
            {
                Plugin.Log?.LogInfo($"Memory watchdog: {(level == 1 ? "light" : "hard")} cleanup worked, RAM {baselineLoad}% -> {load}%.");
                failedCycles = 0;
                FinishCycle();
                return;
            }

            if (level == 1)
            {
                Plugin.Log?.LogInfo($"Memory watchdog: light cleanup did not lower RAM ({baselineLoad}% -> {load}%), running a harder cleanup.");
                baselineLoad = load;
                StartHard();
                return;
            }

            failedCycles++;
            Plugin.Log?.LogWarning($"Memory watchdog: hard cleanup did not lower RAM either ({baselineLoad}% -> {load}%). The memory may be held by other applications or by native memory Unity cannot free. Cleanups will be retried no sooner than {GetCooldownSeconds():0}s from now.");
            FinishCycle();
        }

        private float GetCooldownSeconds()
        {
            float cooldown = Mathf.Max(5f, Plugin.Instance.MemoryWatchdogCooldown);
            float backedOff = cooldown * Mathf.Pow(2f, Mathf.Min(failedCycles, MaxBackoffDoublings));
            return Mathf.Max(cooldown, Mathf.Min(backedOff, MaxBackoffSeconds));
        }

        private void FinishCycle()
        {
            phase = Phase.Idle;
            nextAllowedCollectTime = Time.unscaledTime + GetCooldownSeconds();
        }

        private void Abort(Exception e)
        {
            failedCycles++;
            Plugin.Log?.LogWarning($"Memory watchdog: cleanup failed: {e.Message}");
            FinishCycle();
        }

        private bool TryGetMemoryLoad(out int load)
        {
            load = 0;
            try
            {
                MEMORYSTATUSEX status = new MEMORYSTATUSEX();
                status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (!GlobalMemoryStatusEx(ref status))
                    return false;

                load = (int)status.dwMemoryLoad;
                return true;
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
            {
                unsupported = true;
                Plugin.Log?.LogWarning("Memory watchdog disabled: system memory query is not available on this platform.");
                return false;
            }
        }
    }
}
