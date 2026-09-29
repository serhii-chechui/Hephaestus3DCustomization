using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>The materials a renderer had before a material item changed them.</summary>
    internal sealed class MaterialSnapshot
    {
        public MaterialSnapshot(Renderer renderer, Material[] materials)
        {
            Renderer = renderer;
            Materials = materials;
        }

        public Renderer Renderer { get; }

        public Material[] Materials { get; }
    }
}
