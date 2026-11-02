using UnityEngine;

namespace Sodium
{
    internal sealed class OwnedMaterial : MonoBehaviour
    {
        private Renderer owner;
        private Material instance;

        internal static Material Acquire(Renderer renderer)
        {
            if (renderer == null)
                return null;

            OwnedMaterial tracker = renderer.GetComponent<OwnedMaterial>();
            if (tracker != null && tracker.owner != renderer)
                return renderer.material;

            Material current = renderer.sharedMaterial;
            if (tracker != null && tracker.instance != null && current == tracker.instance)
                return current;

            Material material = renderer.material;
            if (material == null || material == current)
                return material;

            if (tracker == null)
                tracker = renderer.gameObject.AddComponent<OwnedMaterial>();

            tracker.Adopt(renderer, material);
            return material;
        }

        private void Adopt(Renderer renderer, Material material)
        {
            if (instance != null && instance != material)
                Destroy(instance);

            owner = renderer;
            instance = material;
        }

        private void OnDestroy()
        {
            if (instance != null)
                Destroy(instance);

            instance = null;
            owner = null;
        }
    }
}
