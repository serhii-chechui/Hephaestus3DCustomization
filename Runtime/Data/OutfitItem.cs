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
        [Tooltip("Slot the item occupies. Equipping it replaces whatever the slot holds.")]
        private OutfitSlot _slot;

        [SerializeField]
        [Tooltip("Skinned: the prefab's skinned meshes are rebound to the character's bones.\n" +
                 "Socket: the prefab is parented to the character's socket for the slot.")]
        private OutfitAttachMode _attachMode = OutfitAttachMode.Skinned;

        [SerializeField]
        [Tooltip("Key the asset provider resolves to the item's prefab, e.g. an Addressables address.")]
        private string _assetKey;

        [SerializeField]
        [Tooltip("Body parts the item covers. Their renderers are turned off while the item is worn.")]
        private List<OutfitBodyPart> _hiddenBodyParts = new List<OutfitBodyPart>();

        public OutfitSlot Slot => _slot;

        public OutfitAttachMode AttachMode => _attachMode;

        public string AssetKey => _assetKey;

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
    }
}
