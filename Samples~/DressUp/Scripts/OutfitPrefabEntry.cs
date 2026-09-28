using System;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Samples.DressUp
{
    /// <summary>An asset key and the prefab it resolves to.</summary>
    [Serializable]
    public class OutfitPrefabEntry
    {
        public string assetKey;

        public GameObject prefab;
    }
}
