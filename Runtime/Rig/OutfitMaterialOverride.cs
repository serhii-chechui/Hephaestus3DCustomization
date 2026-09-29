using System;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>Materials a <see cref="OutfitAttachMode.Material"/> item puts on one body part.</summary>
    [Serializable]
    public class OutfitMaterialOverride
    {
        [Tooltip("Body part whose renderers get the materials.")]
        public OutfitBodyPart bodyPart;

        [Tooltip("Materials in the renderer's submesh order.")]
        public Material[] materials = new Material[0];
    }
}
