using System;
using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// What a character wears, as item ids: a snapshot to save, send over the network or
    /// restore with <see cref="IOutfitLoadoutWearer.ApplyLoadoutAsync"/>. Serializable with JsonUtility.
    /// </summary>
    [Serializable]
    public class OutfitLoadout
    {
        [SerializeField]
        private List<string> _itemIds = new List<string>();

        public OutfitLoadout()
        {
        }

        public OutfitLoadout(IEnumerable<string> itemIds)
        {
            _itemIds = new List<string>(itemIds);
        }

        public IReadOnlyList<string> ItemIds => _itemIds;
    }
}
