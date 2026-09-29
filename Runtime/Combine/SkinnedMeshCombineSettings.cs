using System;
using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>How <see cref="SkinnedMeshCombiner"/> merges renderers.</summary>
    [Serializable]
    public class SkinnedMeshCombineSettings
    {
        [SerializeField]
        [Tooltip("Pack the textures of materials that share a shader into an atlas, so they draw as one material.")]
        private bool _atlas = true;

        [SerializeField]
        [Tooltip("Largest atlas side in pixels. Tiles are scaled down when they don't fit.")]
        private int _atlasMaxSize = 2048;

        [SerializeField]
        [Tooltip("Pixels of edge color around every tile, against bleeding between tiles in mipmaps.")]
        private int _atlasPadding = 4;

        [SerializeField]
        [Tooltip("Texture properties packed into atlases. The first one the shader has sets the layout. " +
                 "Leave out normal maps: their encoding doesn't survive a copy on every platform.")]
        private List<AtlasTextureProperty> _atlasTextures = new List<AtlasTextureProperty>
        {
            new AtlasTextureProperty("_BaseMap"),
            new AtlasTextureProperty("_MainTex")
        };

        public bool Atlas
        {
            get => _atlas;
            set => _atlas = value;
        }

        public int AtlasMaxSize
        {
            get => _atlasMaxSize;
            set => _atlasMaxSize = value;
        }

        public int AtlasPadding
        {
            get => _atlasPadding;
            set => _atlasPadding = value;
        }

        public List<AtlasTextureProperty> AtlasTextures => _atlasTextures;
    }
}
