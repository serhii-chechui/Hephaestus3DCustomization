using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    internal sealed class RecordingAttachStrategy : IOutfitAttachStrategy
    {
        public int AttachCount { get; private set; }

        public OutfitAttachMode Mode => OutfitAttachMode.Skinned;

        public GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            AttachCount++;
            return Object.Instantiate(prefab, context.Root, false);
        }

        public void Detach(GameObject instance)
        {
            Object.DestroyImmediate(instance);
        }
    }
}
