using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// A place on a character that holds one outfit item at a time (head, torso, legs, ...).
    /// Slots are assets, so every project defines its own set without changing the package.
    /// </summary>
    [CreateAssetMenu(
        fileName = "OutfitSlot",
        menuName = "HephaestusMobile/3D/Customization/Outfit Slot")]
    public class OutfitSlot : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Name shown in tools and UI. Falls back to the asset name.")]
        private string _displayName;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        /// <summary>Creates a slot at runtime, e.g. for procedural content or tests.</summary>
        public static OutfitSlot Create(string displayName)
        {
            var slot = CreateInstance<OutfitSlot>();
            slot.name = displayName;
            slot._displayName = displayName;
            return slot;
        }
    }
}
