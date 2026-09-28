using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Resolves an <see cref="OutfitItem"/> to its prefab. Implement it over Addressables,
    /// asset bundles, Resources or procedural content; the customization code only sees prefabs.
    /// </summary>
    public interface IOutfitAssetProvider
    {
        /// <summary>
        /// Loads the prefab for <paramref name="item"/>. The wearer instantiates the prefab and
        /// never modifies it. Returns null when the asset can't be loaded.
        /// </summary>
        Task<GameObject> LoadAsync(OutfitItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Called once for every prefab returned by <see cref="LoadAsync"/> when the wearer no
        /// longer needs it: the item was unequipped, replaced, or its request was superseded.
        /// </summary>
        void Release(OutfitItem item, GameObject prefab);
    }
}
