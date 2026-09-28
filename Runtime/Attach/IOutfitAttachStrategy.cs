using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Puts an item's prefab on a character. <see cref="OutfitWearer"/> picks the strategy
    /// by <see cref="OutfitItem.AttachMode"/>; replace one with
    /// <see cref="OutfitWearer.SetStrategy"/> to change how that mode works.
    /// </summary>
    public interface IOutfitAttachStrategy
    {
        OutfitAttachMode Mode { get; }

        /// <summary>
        /// Instantiates <paramref name="prefab"/> on the character and returns the instance
        /// root, or null when the item can't be attached. The prefab must not be modified.
        /// </summary>
        GameObject Attach(GameObject prefab, OutfitAttachContext context);

        /// <summary>Removes an instance created by <see cref="Attach"/>.</summary>
        void Detach(GameObject instance);
    }
}
