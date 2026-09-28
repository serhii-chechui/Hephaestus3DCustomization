using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WTFGames.Hephaestus.AddressablesSystem;

namespace WTFGames.Hephaestus.Customization3D.Samples.Addressables
{
    /// <summary>
    /// Loads outfit prefabs through Hephaestus Addressables, using <see cref="OutfitItem.AssetKey"/>
    /// as the address. Several wearers can use the same item: each address is loaded once and
    /// released when the last wearer lets it go, because <see cref="IAddressablesManager"/>
    /// tracks one handle per asset.
    /// </summary>
    public sealed class AddressablesOutfitAssetProvider : IOutfitAssetProvider
    {
        private readonly IAddressablesManager _addressables;
        private readonly Dictionary<string, OutfitAssetHandle> _handles = new Dictionary<string, OutfitAssetHandle>();

        public AddressablesOutfitAssetProvider(IAddressablesManager addressables)
        {
            _addressables = addressables;
        }

        public async Task<GameObject> LoadAsync(OutfitItem item, CancellationToken cancellationToken)
        {
            var key = item.AssetKey;

            if (!_handles.TryGetValue(key, out var handle))
            {
                handle = new OutfitAssetHandle(_addressables.LoadAssetAsync<GameObject>(key));
                _handles.Add(key, handle);
            }

            handle.Users++;

            GameObject prefab;

            try
            {
                prefab = await handle.Load;
            }
            catch
            {
                RemoveUser(key, handle, null);
                throw;
            }

            if (prefab == null) RemoveUser(key, handle, null);

            // Addressables loads can't be cancelled; the wearer releases a prefab it no longer needs.
            return prefab;
        }

        public void Release(OutfitItem item, GameObject prefab)
        {
            if (_handles.TryGetValue(item.AssetKey, out var handle)) RemoveUser(item.AssetKey, handle, prefab);
        }

        private void RemoveUser(string key, OutfitAssetHandle handle, GameObject prefab)
        {
            handle.Users--;

            if (handle.Users > 0) return;

            _handles.Remove(key);

            if (prefab != null) _addressables.Release(prefab);
        }
    }
}
