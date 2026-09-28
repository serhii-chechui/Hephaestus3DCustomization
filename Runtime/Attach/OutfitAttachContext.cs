using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>What an <see cref="IOutfitAttachStrategy"/> needs to put an item on a character.</summary>
    public readonly struct OutfitAttachContext
    {
        public OutfitAttachContext(OutfitItem item, OutfitSkeleton skeleton, Transform root)
        {
            Item = item;
            Skeleton = skeleton;
            Root = root;
        }

        /// <summary>The item being attached.</summary>
        public OutfitItem Item { get; }

        /// <summary>The character's bone and socket index.</summary>
        public OutfitSkeleton Skeleton { get; }

        /// <summary>The character's root; skinned items are parented to it.</summary>
        public Transform Root { get; }
    }
}
