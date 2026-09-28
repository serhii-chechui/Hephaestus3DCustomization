using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// A part of the character's body that clothes can cover (torso, arms, feet, ...). Items
    /// list the parts they hide, so the skin under them never pokes through when the body bends.
    /// </summary>
    [CreateAssetMenu(
        fileName = "OutfitBodyPart",
        menuName = "HephaestusMobile/3D/Customization/Outfit Body Part")]
    public class OutfitBodyPart : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Name shown in tools and UI. Falls back to the asset name.")]
        private string _displayName;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        /// <summary>Creates a body part at runtime, e.g. for procedural content or tests.</summary>
        public static OutfitBodyPart Create(string displayName)
        {
            var bodyPart = CreateInstance<OutfitBodyPart>();
            bodyPart.name = displayName;
            bodyPart._displayName = displayName;
            return bodyPart;
        }
    }
}
