using System;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>A material texture property that <see cref="SkinnedMeshCombiner"/> packs into an atlas.</summary>
    [Serializable]
    public class AtlasTextureProperty
    {
        [Tooltip("Shader texture property, e.g. \"_BaseMap\" or \"_MainTex\".")]
        public string name;

        [Tooltip("The texture holds data rather than colors, e.g. masks. Color textures leave it off.")]
        public bool linear;

        [Tooltip("Color of the tile for a material that has no texture in this property.")]
        public Color emptyColor = Color.white;

        public AtlasTextureProperty()
        {
        }

        public AtlasTextureProperty(string name, bool linear = false)
        {
            this.name = name;
            this.linear = linear;
            emptyColor = Color.white;
        }
    }
}
