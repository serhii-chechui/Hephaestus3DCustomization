using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// The content of a <see cref="OutfitAttachMode.Material"/> item: materials for the
    /// character's body parts. Put it on the item's prefab; the prefab is never instantiated.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutfitMaterialSet : MonoBehaviour
    {
        [SerializeField]
        private List<OutfitMaterialOverride> _overrides = new List<OutfitMaterialOverride>();

        public List<OutfitMaterialOverride> Overrides => _overrides;
    }
}
