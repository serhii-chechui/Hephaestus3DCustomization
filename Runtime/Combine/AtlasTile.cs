using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>Where a material's textures ended up in an atlas.</summary>
    internal sealed class AtlasTile
    {
        public AtlasTile(Material material, Rect rect, Vector2 scale, Vector2 offset)
        {
            Material = material;
            Rect = rect;
            Scale = scale;
            Offset = offset;
        }

        /// <summary>The atlas material that replaces the source material.</summary>
        public Material Material { get; }

        /// <summary>The tile in atlas UV space.</summary>
        public Rect Rect { get; }

        /// <summary>Texture scale of the source material, baked into the UVs.</summary>
        public Vector2 Scale { get; }

        /// <summary>Texture offset of the source material, baked into the UVs.</summary>
        public Vector2 Offset { get; }

        public Vector2 Remap(Vector2 uv)
        {
            var local = Vector2.Scale(uv, Scale) + Offset;
            return Rect.min + Vector2.Scale(local, Rect.size);
        }
    }
}
