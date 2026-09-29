using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    /// <summary>
    /// Returns registered prefabs. Loads of keys marked as deferred stay pending until
    /// <see cref="Complete"/> or <see cref="Fail"/>.
    /// </summary>
    internal sealed class FakeOutfitAssetProvider : IOutfitAssetProvider
    {
        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
        private readonly HashSet<string> _deferredKeys = new HashSet<string>();
        private readonly Dictionary<string, List<TaskCompletionSource<GameObject>>> _pending = new Dictionary<string, List<TaskCompletionSource<GameObject>>>();

        public int LoadCount { get; private set; }

        public List<GameObject> Released { get; } = new List<GameObject>();

        public void Register(string key, GameObject prefab, bool deferred = false)
        {
            _prefabs[key] = prefab;
            if (deferred) _deferredKeys.Add(key);
        }

        /// <summary>Completes every pending load of <paramref name="key"/>.</summary>
        public void Complete(string key)
        {
            foreach (var pending in Take(key))
            {
                pending.SetResult(_prefabs[key]);
            }
        }

        /// <summary>Fails every pending load of <paramref name="key"/>.</summary>
        public void Fail(string key, Exception exception)
        {
            foreach (var pending in Take(key))
            {
                pending.SetException(exception);
            }
        }

        public Task<GameObject> LoadAsync(OutfitItem item, CancellationToken cancellationToken)
        {
            LoadCount++;

            if (!_deferredKeys.Contains(item.AssetKey))
            {
                _prefabs.TryGetValue(item.AssetKey, out var prefab);
                return Task.FromResult(prefab);
            }

            if (!_pending.TryGetValue(item.AssetKey, out var loads))
            {
                loads = new List<TaskCompletionSource<GameObject>>();
                _pending.Add(item.AssetKey, loads);
            }

            var completion = new TaskCompletionSource<GameObject>();
            loads.Add(completion);
            return completion.Task;
        }

        public void Release(OutfitItem item, GameObject prefab)
        {
            Released.Add(prefab);
        }

        private List<TaskCompletionSource<GameObject>> Take(string key)
        {
            var loads = _pending[key];
            _pending.Remove(key);
            return loads;
        }
    }
}
