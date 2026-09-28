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
    public class OutfitWearer : MonoBehaviour, IOutfitWearer
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        [SerializeField]
        [Tooltip("Bone and socket index of the character. Defaults to the first one on this object or its children.")]
        private OutfitSkeleton _skeleton;

        [SerializeField]
        [Tooltip("Move spawned items to this GameObject's layer.")]
        private bool _matchLayer = true;

        private readonly Dictionary<OutfitSlot, EquippedOutfit> _equipped = new Dictionary<OutfitSlot, EquippedOutfit>();
        private readonly Dictionary<OutfitSlot, int> _requestIds = new Dictionary<OutfitSlot, int>();
        private readonly Dictionary<OutfitAttachMode, IOutfitAttachStrategy> _strategies = new Dictionary<OutfitAttachMode, IOutfitAttachStrategy>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private IOutfitAssetProvider _provider;
        private bool _isDestroyed;

        public event Action<OutfitItem> Equipped;

        public event Action<OutfitItem> Unequipped;

        public OutfitSkeleton Skeleton
        {
            get
            {
                if (_skeleton == null) _skeleton = GetComponentInChildren<OutfitSkeleton>(true);
                return _skeleton;
            }
        }

        public bool IsConstructed => _provider != null;

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

        public async Task<bool> EquipAsync(OutfitItem item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_provider == null)
            {
                throw new InvalidOperationException($"{LogTag} {nameof(OutfitWearer)} on '{name}' has no asset provider; call {nameof(Construct)} first.");
            }

            if (_isDestroyed) return false;

            var slot = item.Slot;

            if (slot == null)
            {
                Debug.LogError($"{LogTag} Item '{item.name}' has no slot.", item);
                return false;
            }

            if (Skeleton == null)
            {
                Debug.LogError($"{LogTag} {nameof(OutfitWearer)} on '{name}' has no {nameof(OutfitSkeleton)}.", this);
                return false;
            }

            // A new request supersedes the pending one for the same slot.
            var requestId = NextRequestId(slot);

            if (_equipped.TryGetValue(slot, out var current) && current.Item == item) return true;

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

                if (_isDestroyed || linked.IsCancellationRequested || !IsCurrentRequest(slot, requestId))
                {
                    if (prefab != null) _provider.Release(item, prefab);
                    return false;
                }
            }

            if (prefab == null)
            {
                Debug.LogError($"{LogTag} Prefab '{item.AssetKey}' of item '{item.name}' wasn't loaded.", item);
                return false;
            }

            var strategy = GetStrategy(item.AttachMode);
            var instance = strategy.Attach(prefab, new OutfitAttachContext(item, Skeleton, transform));

            if (instance == null)
            {
                _provider.Release(item, prefab);
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

        public async Task<bool> EquipAsync(OutfitPreset preset, CancellationToken cancellationToken = default)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));

            var requests = new List<Task<bool>>(preset.Items.Count);

            foreach (var item in preset.Items)
            {
                if (item != null) requests.Add(EquipAsync(item, cancellationToken));
            }

            var results = await Task.WhenAll(requests);

            return Array.TrueForAll(results, equipped => equipped);
        }

        public bool Unequip(OutfitSlot slot)
        {
            if (slot == null) return false;

            NextRequestId(slot);

            if (!TakeOff(slot)) return false;

            RefreshBodyParts();
            return true;
        }

        public void UnequipAll()
        {
            foreach (var slot in new List<OutfitSlot>(_requestIds.Keys))
            {
                NextRequestId(slot);
            }

            foreach (var slot in new List<OutfitSlot>(_equipped.Keys))
            {
                TakeOff(slot);
            }

            RefreshBodyParts();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();

            // The instances are destroyed with this hierarchy; only the prefabs are released.
            foreach (var equipped in _equipped.Values)
            {
                _provider?.Release(equipped.Item, equipped.Prefab);
            }

            _equipped.Clear();
        }

        private bool TakeOff(OutfitSlot slot)
        {
            if (!_equipped.TryGetValue(slot, out var equipped)) return false;

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
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, $"{LogTag} No strategy for attach mode {mode}; register one with {nameof(SetStrategy)}.");
            }

            _strategies[mode] = strategy;
            return strategy;
        }

        private int NextRequestId(OutfitSlot slot)
        {
            _requestIds.TryGetValue(slot, out var id);
            _requestIds[slot] = ++id;
            return id;
        }

        private bool IsCurrentRequest(OutfitSlot slot, int requestId)
        {
            return _requestIds.TryGetValue(slot, out var id) && id == requestId;
        }
    }
}
