using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>Materials one material item puts on a renderer.</summary>
    internal sealed class MaterialLayer
    {
        public MaterialLayer(GameObject owner, Material[] materials)
        {
            Owner = owner;
            Materials = materials;
        }

        /// <summary>The instance of the item that applied the materials.</summary>
        public GameObject Owner { get; }

        public Material[] Materials { get; }
    }
}
