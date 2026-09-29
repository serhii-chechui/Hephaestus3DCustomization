using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    internal sealed class RecordingStrategyAsset : OutfitAttachStrategyAsset
    {
        public int AttachCount { get; private set; }

        public int DetachCount { get; private set; }

        public override GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            AttachCount++;
            return Object.Instantiate(prefab, context.Root, false);
        }

        public override void Detach(GameObject instance)
        {
            DetachCount++;
            base.Detach(instance);
        }
    }
}
