using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Marks the root of an equipped outfit. <see cref="OutfitSkeleton"/> skips marked
    /// hierarchies, so an item's transforms never shadow the character's own bones.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OutfitInstance : MonoBehaviour
    {
        public OutfitItem Item { get; internal set; }
    }
}
