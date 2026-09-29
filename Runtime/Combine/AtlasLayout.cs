using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>Tile placement in an atlas, computed by <see cref="AtlasPacker"/>.</summary>
    internal sealed class AtlasLayout
    {
        public AtlasLayout(int width, int height, RectInt[] tiles, RectInt[] contents, Rect[] uvRects)
        {
            Width = width;
            Height = height;
            Tiles = tiles;
            Contents = contents;
            UvRects = uvRects;
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>Tiles with their padding, in pixels.</summary>
        public RectInt[] Tiles { get; }

        /// <summary>The texture area of every tile, in pixels.</summary>
        public RectInt[] Contents { get; }

        /// <summary>The texture area of every tile, in UV space.</summary>
        public Rect[] UvRects { get; }
    }
}
