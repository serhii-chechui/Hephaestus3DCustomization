using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Describes a wearable item: the slot it occupies, how it is attached and the key an
    /// <see cref="IOutfitAssetProvider"/> resolves to its prefab. The item never references
    /// the prefab directly, so the asset can live in Addressables, a bundle or a remote CDN.
    /// </summary>
    [CreateAssetMenu(
        fileName = "OutfitItem",
        menuName = "HephaestusMobile/3D/Customization/Outfit Item")]
    public class OutfitItem : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable id used in saved loadouts and on the network. Defaults to the asset name.")]
        private string _id;

        [SerializeField]
        [Tooltip("Slot the item occupies. Equipping it replaces whatever the slot holds.")]
        private OutfitSlot _slot;

        [SerializeField]
        [Tooltip("Skinned: the prefab's skinned meshes are rebound to the character's bones.\n" +
                 "Socket: the prefab is parented to the character's socket for the slot.\n" +
                 "Material: the prefab's OutfitMaterialSet changes materials of body parts.\n" +
                 "Custom: the item's own attach strategy puts it on.")]
        private OutfitAttachMode _attachMode = OutfitAttachMode.Skinned;

        [SerializeField]
        [Tooltip("Key the asset provider resolves to the item's prefab, e.g. an Addressables address.")]
        private string _assetKey;

        [SerializeField]
        [Tooltip("Socket items: id of the socket within the slot, e.g. \"Left\" or \"Right\". Empty for the slot's default socket.")]
        private string _socketId;

        [SerializeField]
        [Tooltip("Custom items: the strategy that attaches the item.")]
        private OutfitAttachStrategyAsset _customStrategy;

        [SerializeField]
        [Tooltip("Body parts the item covers. Their renderers are turned off while the item is worn.")]
        private List<OutfitBodyPart> _hiddenBodyParts = new List<OutfitBodyPart>();

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        public OutfitSlot Slot => _slot;

        public OutfitAttachMode AttachMode => _attachMode;

        public string AssetKey => _assetKey;

        public string SocketId => _socketId ?? string.Empty;

        public OutfitAttachStrategyAsset CustomStrategy => _customStrategy;

        public IReadOnlyList<OutfitBodyPart> HiddenBodyParts => _hiddenBodyParts;

        /// <summary>Creates an item at runtime, e.g. for procedural content or tests.</summary>
        public static OutfitItem Create(string itemName, OutfitSlot slot, OutfitAttachMode attachMode, string assetKey,
            IEnumerable<OutfitBodyPart> hiddenBodyParts = null)
        {
            var item = CreateInstance<OutfitItem>();
            item.name = itemName;
            item._slot = slot;
            item._attachMode = attachMode;
            item._assetKey = assetKey;
            if (hiddenBodyParts != null) item._hiddenBodyParts = new List<OutfitBodyPart>(hiddenBodyParts);
            return item;
        }

        /// <summary>Sets the stable id; for items created at runtime.</summary>
        public OutfitItem WithId(string id)
        {
            _id = id;
            return this;
        }

        /// <summary>Sets the socket id within the slot; for items created at runtime.</summary>
        public OutfitItem WithSocket(string socketId)
        {
            _socketId = socketId;
            return this;
        }

        /// <summary>Sets the custom attach strategy and switches the item to <see cref="OutfitAttachMode.Custom"/>.</summary>
        public OutfitItem WithCustomStrategy(OutfitAttachStrategyAsset strategy)
        {
            _customStrategy = strategy;
            _attachMode = OutfitAttachMode.Custom;
            return this;
        }
    }
}
