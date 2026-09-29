using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Dresses a character: loads items through an <see cref="IOutfitAssetProvider"/> and
    /// attaches them with the <see cref="IOutfitAttachStrategy"/> for their
    /// <see cref="OutfitItem.AttachMode"/>, and turns off the body parts the worn items hide.
    /// Call <see cref="Construct"/> before equipping.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutfitWearer : MonoBehaviour, IOutfitWearer, IDisposable
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        [SerializeField]
        [Tooltip("Bone and socket index of the character. Defaults to the first one on this object or its children.")]
        private OutfitSkeleton _skeleton;

        [SerializeField]
        [Tooltip("Move spawned items to this GameObject's layer.")]
        private bool _matchLayer = true;

        [SerializeField]
        [Tooltip("Items a slot falls back to when its item is taken off, e.g. underwear or bare feet.")]
        private OutfitPreset _defaultOutfit;

        private readonly Dictionary<OutfitSlot, EquippedOutfit> _equipped = new Dictionary<OutfitSlot, EquippedOutfit>();
        private readonly Dictionary<OutfitSlot, int> _requestIds = new Dictionary<OutfitSlot, int>();
        private readonly Dictionary<OutfitSlot, PendingEquip> _pending = new Dictionary<OutfitSlot, PendingEquip>();
        private readonly Dictionary<OutfitAttachMode, IOutfitAttachStrategy> _strategies = new Dictionary<OutfitAttachMode, IOutfitAttachStrategy>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private IOutfitAssetProvider _provider;
        private bool _isDisposed;

        public event Action<OutfitItem> Equipped;

        public event Action<OutfitItem> Unequipped;

        public event Action<OutfitItem, Exception> EquipFailed;

        public OutfitSkeleton Skeleton
        {
            get
            {
                if (_skeleton == null) _skeleton = GetComponentInChildren<OutfitSkeleton>(true);
                return _skeleton;
            }
        }

        public bool IsConstructed => _provider != null;

        /// <summary>
        /// Items slots fall back to when their item is taken off: <see cref="Unequip"/> puts the
        /// slot's default item back on, <see cref="UnequipAll"/> keeps default items on and puts
        /// them back in the other slots, <see cref="EquipDefaultOutfitAsync"/> fills empty slots.
        /// Unequipping a slot that wears its default item leaves it empty.
        /// </summary>
        public OutfitPreset DefaultOutfit
        {
            get => _defaultOutfit;
            set => _defaultOutfit = value;
        }

        public IReadOnlyCollection<OutfitItem> EquippedItems
        {
            get
            {
                var items = new List<OutfitItem>(_equipped.Count);

                foreach (var equipped in _equipped.Values)
                {
                    items.Add(equipped.Item);
                }

                return items;
            }
        }

        /// <summary>
        /// Sets the asset source. With Zenject, call it from an [Inject] method of your own
        /// component or installer.
        /// </summary>
        public void Construct(IOutfitAssetProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>Replaces the strategy used for <see cref="IOutfitAttachStrategy.Mode"/>.</summary>
        public void SetStrategy(IOutfitAttachStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));

            _strategies[strategy.Mode] = strategy;
        }

        public bool IsLoading(OutfitSlot slot)
        {
            return slot != null && _pending.ContainsKey(slot);
        }

        public OutfitLoadout GetLoadout()
        {
            var ids = new List<string>(_equipped.Count);

            foreach (var equipped in _equipped.Values)
            {
                ids.Add(equipped.Item.Id);
            }

            return new OutfitLoadout(ids);
        }

        public async Task<bool> ApplyLoadoutAsync(OutfitLoadout loadout, IOutfitItemCatalog catalog, CancellationToken cancellationToken = default)
        {
            if (loadout == null) throw new ArgumentNullException(nameof(loadout));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var items = new List<OutfitItem>(loadout.ItemIds.Count);
            var slots = new HashSet<OutfitSlot>();
            var complete = true;

            foreach (var id in loadout.ItemIds)
            {
                if (catalog.TryGetItem(id, out var item) && item != null && item.Slot != null)
                {
                    items.Add(item);
                    slots.Add(item.Slot);
                }
                else
                {
                    Debug.LogWarning($"{LogTag} Loadout item '{id}' isn't in the catalog; it's skipped.", this);
                    complete = false;
                }
            }

            foreach (var slot in new List<OutfitSlot>(_equipped.Keys))
            {
                if (!slots.Contains(slot)) Unequip(slot);
            }

            var requests = new List<Task<bool>>(items.Count);

            foreach (var item in items)
            {
                requests.Add(EquipAsync(item, cancellationToken));
            }

            var results = await Task.WhenAll(requests);

            return complete && Array.TrueForAll(results, equipped => equipped);
        }

        /// <summary>Puts the default outfit's items on the slots that are empty.</summary>
        public Task<bool> EquipDefaultOutfitAsync(CancellationToken cancellationToken = default)
        {
            if (_defaultOutfit == null) return Task.FromResult(true);

            var requests = new List<Task<bool>>();

            foreach (var item in _defaultOutfit.Items)
            {
                if (item == null || item.Slot == null || _equipped.ContainsKey(item.Slot) || _pending.ContainsKey(item.Slot)) continue;
                requests.Add(EquipAsync(item, cancellationToken));
            }

            return WhenAllEquipped(requests);
        }

        public bool TryGetEquipped(OutfitSlot slot, out OutfitItem item)
        {
            if (slot != null && _equipped.TryGetValue(slot, out var equipped))
            {
                item = equipped.Item;
                return true;
            }

            item = null;
            return false;
        }

        /// <remarks>
        /// Requesting an item that is already loading into its slot returns that request;
        /// <paramref name="cancellationToken"/> then doesn't cancel it.
        /// </remarks>
        public Task<bool> EquipAsync(OutfitItem item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_provider == null)
            {
                throw new InvalidOperationException($"{LogTag} {nameof(OutfitWearer)} on '{name}' has no asset provider; call {nameof(Construct)} first.");
            }

            if (_isDisposed) return Task.FromResult(false);

            var slot = item.Slot;

            if (slot == null)
            {
                Debug.LogError($"{LogTag} Item '{item.name}' has no slot.", item);
                return Task.FromResult(false);
            }

            if (Skeleton == null)
            {
                Debug.LogError($"{LogTag} {nameof(OutfitWearer)} on '{name}' has no {nameof(OutfitSkeleton)}.", this);
                return Task.FromResult(false);
            }

            // The same item is already on its way into this slot: share that load.
            if (_pending.TryGetValue(slot, out var pending) && pending.Item == item) return pending.Task;

            // A new request supersedes the pending one for the same slot.
            var requestId = NextRequestId(slot);

            if (_equipped.TryGetValue(slot, out var current) && current.Item == item) return Task.FromResult(true);

            var request = LoadAndAttachAsync(item, slot, requestId, cancellationToken);

            if (!request.IsCompleted) _pending[slot] = new PendingEquip(item, requestId, request);

            return request;
        }

        public async Task<bool> EquipAsync(OutfitPreset preset, CancellationToken cancellationToken = default)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));

            var requests = new List<Task<bool>>(preset.Items.Count);

            foreach (var item in preset.Items)
            {
                if (item != null) requests.Add(EquipAsync(item, cancellationToken));
            }

            return await WhenAllEquipped(requests);
        }

        /// <remarks>When the default outfit has an item for the slot, it is put back on.</remarks>
        public bool Unequip(OutfitSlot slot)
        {
            if (slot == null) return false;

            NextRequestId(slot);

            if (!TakeOff(slot, out var removed)) return false;

            RefreshBodyParts();
            RestoreDefault(slot, removed);
            return true;
        }

        public void UnequipAll()
        {
            foreach (var slot in new List<OutfitSlot>(_requestIds.Keys))
            {
                NextRequestId(slot);
            }

            var removed = new List<OutfitItem>(_equipped.Count);

            foreach (var pair in new List<KeyValuePair<OutfitSlot, EquippedOutfit>>(_equipped))
            {
                // Default items stay on: taking everything off returns to the default outfit.
                if (GetDefault(pair.Key) == pair.Value.Item) continue;

                if (TakeOff(pair.Key, out var item)) removed.Add(item);
            }

            RefreshBodyParts();

            foreach (var item in removed)
            {
                RestoreDefault(item.Slot, item);
            }
        }

        /// <summary>
        /// Takes every item off, releases every loaded prefab and cancels pending loads; the
        /// wearer can't equip afterwards. Destroying the GameObject does the same, but Unity
        /// doesn't call OnDestroy on objects that were never active: dispose such a character
        /// before destroying it.
        /// </summary>
        public void Dispose()
        {
            Shutdown(detachInstances: true);
        }

        private void OnDestroy()
        {
            // The instances are destroyed with this hierarchy; only the prefabs are released.
            Shutdown(detachInstances: false);
        }

        private async Task<bool> LoadAndAttachAsync(OutfitItem item, OutfitSlot slot, int requestId, CancellationToken cancellationToken)
        {
            try
            {
                GameObject prefab;

                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token))
                {
                    try
                    {
                        prefab = await _provider.LoadAsync(item, linked.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return false;
                    }
                    catch (Exception) when (!IsRequestAlive(slot, requestId, linked.Token))
                    {
                        // Nobody waits for this item any more, so its failure doesn't matter.
                        return false;
                    }
                    catch (Exception exception)
                    {
                        EquipFailed?.Invoke(item, exception);
                        throw;
                    }

                    if (!IsRequestAlive(slot, requestId, linked.Token))
                    {
                        if (prefab != null) _provider.Release(item, prefab);
                        return false;
                    }
                }

                if (prefab == null)
                {
                    Debug.LogError($"{LogTag} Prefab '{item.AssetKey}' of item '{item.name}' wasn't loaded.", item);
                    EquipFailed?.Invoke(item, null);
                    return false;
                }

                IOutfitAttachStrategy strategy;
                GameObject instance;

                try
                {
                    strategy = GetStrategy(item);
                    instance = strategy.Attach(prefab, new OutfitAttachContext(item, Skeleton, transform));
                }
                catch (Exception exception)
                {
                    _provider.Release(item, prefab);
                    EquipFailed?.Invoke(item, exception);
                    throw;
                }

                if (instance == null)
                {
                    _provider.Release(item, prefab);
                    EquipFailed?.Invoke(item, null);
                    return false;
                }

                instance.AddComponent<OutfitInstance>().Item = item;

                if (_matchLayer) OutfitObjectUtility.SetLayerRecursively(instance, gameObject.layer);

                TakeOff(slot);
                _equipped[slot] = new EquippedOutfit(item, prefab, instance, strategy);
                RefreshBodyParts();
                Equipped?.Invoke(item);

                return true;
            }
            finally
            {
                if (_pending.TryGetValue(slot, out var pending) && pending.RequestId == requestId) _pending.Remove(slot);
            }
        }

        private void Shutdown(bool detachInstances)
        {
            if (_isDisposed) return;

            _isDisposed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
            _pending.Clear();

            foreach (var equipped in _equipped.Values)
            {
                if (detachInstances) equipped.Strategy.Detach(equipped.Instance);
                _provider?.Release(equipped.Item, equipped.Prefab);
            }

            _equipped.Clear();

            if (detachInstances) RefreshBodyParts();
        }

        private bool TakeOff(OutfitSlot slot)
        {
            return TakeOff(slot, out _);
        }

        private bool TakeOff(OutfitSlot slot, out OutfitItem removed)
        {
            removed = null;

            if (!_equipped.TryGetValue(slot, out var equipped)) return false;

            removed = equipped.Item;

            _equipped.Remove(slot);
            equipped.Strategy.Detach(equipped.Instance);
            _provider.Release(equipped.Item, equipped.Prefab);
            Unequipped?.Invoke(equipped.Item);

            return true;
        }

        // A body part stays hidden while at least one worn item hides it.
        private void RefreshBodyParts()
        {
            var skeleton = Skeleton;

            if (skeleton == null) return;

            var hidden = new HashSet<OutfitBodyPart>();

            foreach (var equipped in _equipped.Values)
            {
                foreach (var bodyPart in equipped.Item.HiddenBodyParts)
                {
                    if (bodyPart != null) hidden.Add(bodyPart);
                }
            }

            foreach (var bodyPart in skeleton.BodyParts)
            {
                var visible = !hidden.Contains(bodyPart);

                foreach (var renderer in skeleton.GetBodyPartRenderers(bodyPart))
                {
                    if (renderer != null) renderer.enabled = visible;
                }
            }
        }

        // The slot's default item goes back on when another item leaves the slot.
        private void RestoreDefault(OutfitSlot slot, OutfitItem removed)
        {
            if (_isDisposed) return;

            var fallback = GetDefault(slot);

            if (fallback != null && fallback != removed) EquipInBackground(fallback);
        }

        private OutfitItem GetDefault(OutfitSlot slot)
        {
            if (_defaultOutfit == null) return null;

            OutfitItem fallback = null;

            // The last item for the slot wins, as when the preset is equipped.
            foreach (var item in _defaultOutfit.Items)
            {
                if (item != null && item.Slot == slot) fallback = item;
            }

            return fallback;
        }

        private async void EquipInBackground(OutfitItem item)
        {
            try
            {
                await EquipAsync(item);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private static async Task<bool> WhenAllEquipped(List<Task<bool>> requests)
        {
            var results = await Task.WhenAll(requests);

            return Array.TrueForAll(results, equipped => equipped);
        }

        private IOutfitAttachStrategy GetStrategy(OutfitItem item)
        {
            if (item.AttachMode != OutfitAttachMode.Custom) return GetStrategy(item.AttachMode);

            if (item.CustomStrategy == null)
            {
                throw new InvalidOperationException($"{LogTag} Custom item '{item.name}' has no attach strategy.");
            }

            return item.CustomStrategy;
        }

        private IOutfitAttachStrategy GetStrategy(OutfitAttachMode mode)
        {
            if (_strategies.TryGetValue(mode, out var strategy)) return strategy;

            switch (mode)
            {
                case OutfitAttachMode.Skinned:
                    strategy = new SkinnedOutfitAttachStrategy();
                    break;
                case OutfitAttachMode.Socket:
                    strategy = new SocketOutfitAttachStrategy();
                    break;
                case OutfitAttachMode.Material:
                    strategy = new MaterialOutfitAttachStrategy();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, $"{LogTag} No strategy for attach mode {mode}; register one with {nameof(SetStrategy)}.");
            }

            _strategies[mode] = strategy;
            return strategy;
        }

        private int NextRequestId(OutfitSlot slot)
        {
            _pending.Remove(slot);
            _requestIds.TryGetValue(slot, out var id);
            _requestIds[slot] = ++id;
            return id;
        }

        private bool IsRequestAlive(OutfitSlot slot, int requestId, CancellationToken cancellationToken)
        {
            return !_isDisposed
                   && !cancellationToken.IsCancellationRequested
                   && _requestIds.TryGetValue(slot, out var id)
                   && id == requestId;
        }
    }
}
