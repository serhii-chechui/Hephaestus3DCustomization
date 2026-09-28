using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    internal sealed class EquippedOutfit
    {
        public EquippedOutfit(OutfitItem item, GameObject prefab, GameObject instance, IOutfitAttachStrategy strategy)
        {
            Item = item;
            Prefab = prefab;
            Instance = instance;
            Strategy = strategy;
        }

        public OutfitItem Item { get; }

        public GameObject Prefab { get; }

        public GameObject Instance { get; }

        public IOutfitAttachStrategy Strategy { get; }
    }
}
