using System;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    internal sealed class ThrowingAttachStrategy : IOutfitAttachStrategy
    {
        public OutfitAttachMode Mode => OutfitAttachMode.Skinned;

        public GameObject Attach(GameObject prefab, OutfitAttachContext context)
        {
            throw new InvalidOperationException("Attach failed.");
        }

        public void Detach(GameObject instance)
        {
        }
    }
}
