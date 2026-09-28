using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// A complete look: a set of items equipped together, e.g. a default outfit or a shop bundle.
    /// When several items share a slot, the last one in the list wins.
    /// </summary>
    [CreateAssetMenu(
        fileName = "OutfitPreset",
        menuName = "HephaestusMobile/3D/Customization/Outfit Preset")]
    public class OutfitPreset : ScriptableObject
    {
        [SerializeField]
        private List<OutfitItem> _items = new List<OutfitItem>();

        public IReadOnlyList<OutfitItem> Items => _items;

        /// <summary>Creates a preset at runtime, e.g. for procedural content or tests.</summary>
        public static OutfitPreset Create(string presetName, IEnumerable<OutfitItem> items)
        {
            var preset = CreateInstance<OutfitPreset>();
            preset.name = presetName;
            preset._items = new List<OutfitItem>(items);
            return preset;
        }
    }
}
