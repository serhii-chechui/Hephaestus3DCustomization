using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Samples.DressUp
{
    /// <summary>
    /// Resolves items through an <see cref="OutfitPrefabLibrary"/>. The prefabs are referenced
    /// directly, so there is nothing to release. A simulated latency shows how the wearer handles
    /// requests that overtake each other, as they do with real downloads.
    /// </summary>
    public sealed class PrefabOutfitAssetProvider : IOutfitAssetProvider
    {
        private readonly OutfitPrefabLibrary _library;
        private readonly float _latencySeconds;

        public PrefabOutfitAssetProvider(OutfitPrefabLibrary library, float latencySeconds = 0f)
        {
            _library = library;
            _latencySeconds = latencySeconds;
        }

        public async Task<GameObject> LoadAsync(OutfitItem item, CancellationToken cancellationToken)
        {
            if (_latencySeconds > 0f)
            {
                await Task.Delay(TimeSpan.FromSeconds(_latencySeconds), cancellationToken);
            }

            return _library.TryGetPrefab(item.AssetKey, out var prefab) ? prefab : null;
        }

        public void Release(OutfitItem item, GameObject prefab)
        {
        }
    }
}
