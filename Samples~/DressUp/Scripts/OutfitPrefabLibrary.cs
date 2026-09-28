using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Samples.DressUp
{
    /// <summary>Maps asset keys to prefabs referenced directly, for projects without Addressables.</summary>
    [CreateAssetMenu(
        fileName = "OutfitPrefabLibrary",
        menuName = "HephaestusMobile/3D/Customization/Samples/Outfit Prefab Library")]
    public class OutfitPrefabLibrary : ScriptableObject
    {
        [SerializeField]
        private List<OutfitPrefabEntry> _entries = new List<OutfitPrefabEntry>();

        public List<OutfitPrefabEntry> Entries => _entries;

        public bool TryGetPrefab(string assetKey, out GameObject prefab)
        {
            foreach (var entry in _entries)
            {
                if (entry.assetKey == assetKey && entry.prefab != null)
                {
                    prefab = entry.prefab;
                    return true;
                }
            }

            prefab = null;
            return false;
        }
    }
}
