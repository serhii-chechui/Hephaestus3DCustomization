using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Attaches material items: puts the materials of the prefab's <see cref="OutfitMaterialSet"/>
    /// on the character's body part renderers. Items layer over each other per renderer: a
    /// renderer shows the materials of the last item still worn, or its own materials when none
    /// is left, whatever order items are put on and taken off in. The returned instance is an
    /// empty marker object under the character.
    /// </summary>
    public class MaterialOutfitAttachStrategy : IOutfitAttachStrategy
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        private readonly Dictionary<Renderer, RendererMaterialStack> _stacks = new Dictionary<Renderer, RendererMaterialStack>();
        private readonly Dictionary<GameObject, List<RendererMaterialStack>> _byInstance = new Dictionary<GameObject, List<RendererMaterialStack>>();

        public OutfitAttachMode Mode => OutfitAttachMode.Material;

        public GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            var set = prefab.GetComponent<OutfitMaterialSet>();

            if (set == null)
            {
                Debug.LogWarning($"{LogTag} '{prefab.name}' has no {nameof(OutfitMaterialSet)}; material item '{context.Item.name}' isn't attached.", prefab);
                return null;
            }

            var instance = new GameObject(prefab.name);
            instance.transform.SetParent(context.Root, false);

            var stacks = new List<RendererMaterialStack>();

            foreach (var entry in set.Overrides)
            {
                if (entry == null || entry.bodyPart == null || entry.materials == null || entry.materials.Length == 0) continue;

                foreach (var renderer in context.Skeleton.GetBodyPartRenderers(entry.bodyPart))
                {
                    if (renderer == null) continue;

                    if (!_stacks.TryGetValue(renderer, out var stack))
                    {
                        stack = new RendererMaterialStack(renderer);
                        _stacks.Add(renderer, stack);
                    }

                    stack.Push(instance, entry.materials);
                    stacks.Add(stack);
                }
            }

            _byInstance.Add(instance, stacks);

            return instance;
        }

        public void Detach(GameObject instance)
        {
            if (_byInstance.TryGetValue(instance, out var stacks))
            {
                _byInstance.Remove(instance);

                foreach (var stack in stacks)
                {
                    stack.Remove(instance);

                    if (stack.IsEmpty) _stacks.Remove(stack.Renderer);
                }
            }

            OutfitObjectUtility.Destroy(instance);
        }
    }
}
