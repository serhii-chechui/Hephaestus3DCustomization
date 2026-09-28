using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    internal static class OutfitObjectUtility
    {
        /// <summary>Destroys in Play Mode and immediately in Edit Mode, where Destroy isn't allowed.</summary>
        public static void Destroy(Object target)
        {
            if (target == null) return;

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        public static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;

            var transform = root.transform;

            for (var i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }
    }
}
