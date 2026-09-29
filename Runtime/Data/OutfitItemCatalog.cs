using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>A list of items that resolves item ids, for restoring saved loadouts.</summary>
    [CreateAssetMenu(
        fileName = "OutfitItemCatalog",
        menuName = "HephaestusMobile/3D/Customization/Outfit Item Catalog")]
    public class OutfitItemCatalog : ScriptableObject, IOutfitItemCatalog
    {
        private const string LogTag = "[Hephaestus 3D Customization]";

        [SerializeField]
        private List<OutfitItem> _items = new List<OutfitItem>();

        private Dictionary<string, OutfitItem> _byId;

        public IReadOnlyList<OutfitItem> Items => _items;

        /// <summary>Creates a catalog at runtime, e.g. for procedural content or tests.</summary>
        public static OutfitItemCatalog Create(IEnumerable<OutfitItem> items)
        {
            var catalog = CreateInstance<OutfitItemCatalog>();
            catalog._items = new List<OutfitItem>(items);
            return catalog;
        }

        public bool TryGetItem(string id, out OutfitItem item)
        {
            if (_byId == null) Index();

            item = null;
            return id != null && _byId.TryGetValue(id, out item);
        }

        private void OnValidate()
        {
            _byId = null;
        }

        private void Index()
        {
            _byId = new Dictionary<string, OutfitItem>();

            foreach (var item in _items)
            {
                if (item == null) continue;

                if (_byId.ContainsKey(item.Id))
                {
                    Debug.LogWarning($"{LogTag} Catalog '{name}' has several items with id '{item.Id}'; the first one is used.", this);
                    continue;
                }

                _byId.Add(item.Id, item);
            }
        }
    }
}
