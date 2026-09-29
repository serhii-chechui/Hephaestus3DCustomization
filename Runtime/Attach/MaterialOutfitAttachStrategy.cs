using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Attaches material items: puts the materials of the prefab's <see cref="OutfitMaterialSet"/>
    /// on the character's body part renderers and restores the previous materials on detach.
    /// The returned instance is an empty marker object under the character. Give material items
    /// their own slot, so two of them never change the same body part at once.
    /// </summary>
    public class MaterialOutfitAttachStrategy : IOutfitAttachStrategy
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        private readonly Dictionary<GameObject, List<MaterialSnapshot>> _snapshots = new Dictionary<GameObject, List<MaterialSnapshot>>();

        public OutfitAttachMode Mode => OutfitAttachMode.Material;

        public GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            var set = prefab.GetComponent<OutfitMaterialSet>();

            if (set == null)
            {
                Debug.LogWarning($"{LogTag} '{prefab.name}' has no {nameof(OutfitMaterialSet)}; material item '{context.Item.name}' isn't attached.", prefab);
                return null;
            }

            var snapshots = new List<MaterialSnapshot>();

            foreach (var entry in set.Overrides)
            {
                if (entry == null || entry.bodyPart == null || entry.materials == null || entry.materials.Length == 0) continue;

                foreach (var renderer in context.Skeleton.GetBodyPartRenderers(entry.bodyPart))
                {
                    if (renderer == null) continue;

                    snapshots.Add(new MaterialSnapshot(renderer, renderer.sharedMaterials));
                    renderer.sharedMaterials = entry.materials;
                }
            }

            var instance = new GameObject(prefab.name);
            instance.transform.SetParent(context.Root, false);
            _snapshots.Add(instance, snapshots);

            return instance;
        }

        public void Detach(GameObject instance)
        {
            if (_snapshots.TryGetValue(instance, out var snapshots))
            {
                _snapshots.Remove(instance);

                // Restore in reverse order, so a renderer changed twice ends with its first materials.
                for (var i = snapshots.Count - 1; i >= 0; i--)
                {
                    if (snapshots[i].Renderer != null) snapshots[i].Renderer.sharedMaterials = snapshots[i].Materials;
                }
            }

            OutfitObjectUtility.Destroy(instance);
        }
    }
}
