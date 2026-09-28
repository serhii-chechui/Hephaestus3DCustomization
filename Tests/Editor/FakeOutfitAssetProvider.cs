using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    /// <summary>Returns registered prefabs. Keys marked as deferred stay pending until <see cref="Complete"/>.</summary>
    internal sealed class FakeOutfitAssetProvider : IOutfitAssetProvider
    {
        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
        private readonly HashSet<string> _deferredKeys = new HashSet<string>();
        private readonly Dictionary<string, TaskCompletionSource<GameObject>> _pending = new Dictionary<string, TaskCompletionSource<GameObject>>();

        public int LoadCount { get; private set; }

        public List<GameObject> Released { get; } = new List<GameObject>();

        public void Register(string key, GameObject prefab, bool deferred = false)
        {
            _prefabs[key] = prefab;
            if (deferred) _deferredKeys.Add(key);
        }

        public void Complete(string key)
        {
            var pending = _pending[key];
            _pending.Remove(key);
            pending.SetResult(_prefabs[key]);
        }

        public Task<GameObject> LoadAsync(OutfitItem item, CancellationToken cancellationToken)
        {
            LoadCount++;

            if (!_deferredKeys.Contains(item.AssetKey))
            {
                _prefabs.TryGetValue(item.AssetKey, out var prefab);
                return Task.FromResult(prefab);
            }

            var completion = new TaskCompletionSource<GameObject>();
            _pending[item.AssetKey] = completion;
            return completion.Task;
        }

        public void Release(OutfitItem item, GameObject prefab)
        {
            Released.Add(prefab);
        }
    }
}
