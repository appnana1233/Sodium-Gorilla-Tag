// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using System.Reflection;
using GorillaTagScripts.Builder;
using UnityEngine;

namespace Sodium
{
    internal sealed class MonkeBlocksStuff : MonoBehaviour
    {
        private static readonly MethodInfo SharedBlocksStart = typeof(SharedBlocksManager)
            .GetMethod("Start", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private float nextCheckTime;

        private void Update()
        {
            if (Time.unscaledTime < nextCheckTime)
                return;

            nextCheckTime = Time.unscaledTime + 0.25f;
            if (!BlocksPatch.HasDeferredManager || ZoneManagement.instance == null ||
                !ZoneManagement.instance.IsZoneActive(GTZone.monkeBlocksShared))
                return;

            SharedBlocksManager manager = BlocksPatch.TakeDeferredManager();
            if (manager != null && SharedBlocksStart != null)
                SharedBlocksStart.Invoke(manager, null);
        }
    }
}
