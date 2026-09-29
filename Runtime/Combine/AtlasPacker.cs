using System.Collections.Generic;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Packs tiles into rows (shelves), tallest first. When they don't fit into the largest
    /// atlas, every tile is scaled down by half until they do.
    /// </summary>
    internal static class AtlasPacker
    {
        public static AtlasLayout Pack(IReadOnlyList<Vector2Int> sizes, int maxSize, int padding)
        {
            // Atlas sides are powers of two, so a side between two of them rounds down.
            maxSize = Mathf.Max(1, maxSize);
            if (!Mathf.IsPowerOfTwo(maxSize)) maxSize = Mathf.NextPowerOfTwo(maxSize) / 2;
            padding = Mathf.Max(0, padding);

            var order = new List<int>(sizes.Count);

            for (var i = 0; i < sizes.Count; i++)
            {
                order.Add(i);
            }

            order.Sort((a, b) => sizes[b].y != sizes[a].y ? sizes[b].y.CompareTo(sizes[a].y) : a.CompareTo(b));

            var contents = new RectInt[sizes.Count];
            var tiles = new RectInt[sizes.Count];

            for (var scale = 1f; ; scale *= 0.5f)
            {
                var smallest = true;

                for (var i = 0; i < sizes.Count; i++)
                {
                    var width = Mathf.Max(1, Mathf.RoundToInt(sizes[i].x * scale));
                    var height = Mathf.Max(1, Mathf.RoundToInt(sizes[i].y * scale));

                    contents[i] = new RectInt(0, 0, width, height);
                    smallest &= width == 1 && height == 1;
                }

                if (TryPlace(order, contents, tiles, maxSize, padding, out var usedWidth, out var usedHeight))
                {
                    return CreateLayout(contents, tiles, usedWidth, usedHeight);
                }

                if (smallest) return null;
            }
        }

        private static bool TryPlace(List<int> order, RectInt[] contents, RectInt[] tiles, int maxSize, int padding, out int usedWidth, out int usedHeight)
        {
            usedWidth = 0;
            usedHeight = 0;

            var x = 0;
            var y = 0;
            var shelfHeight = 0;

            foreach (var index in order)
            {
                var width = contents[index].width + padding * 2;
                var height = contents[index].height + padding * 2;

                if (width > maxSize || height > maxSize) return false;

                if (x + width > maxSize)
                {
                    y += shelfHeight;
                    x = 0;
                    shelfHeight = 0;
                }

                if (y + height > maxSize) return false;

                tiles[index] = new RectInt(x, y, width, height);
                contents[index] = new RectInt(x + padding, y + padding, contents[index].width, contents[index].height);

                x += width;
                shelfHeight = Mathf.Max(shelfHeight, height);
                usedWidth = Mathf.Max(usedWidth, x);
            }

            usedHeight = y + shelfHeight;
            return true;
        }

        private static AtlasLayout CreateLayout(RectInt[] contents, RectInt[] tiles, int usedWidth, int usedHeight)
        {
            var width = Mathf.NextPowerOfTwo(Mathf.Max(1, usedWidth));
            var height = Mathf.NextPowerOfTwo(Mathf.Max(1, usedHeight));
            var uvRects = new Rect[contents.Length];

            for (var i = 0; i < contents.Length; i++)
            {
                var content = contents[i];
                uvRects[i] = new Rect(
                    (float)content.x / width,
                    (float)content.y / height,
                    (float)content.width / width,
                    (float)content.height / height);
            }

            return new AtlasLayout(width, height, (RectInt[])tiles.Clone(), (RectInt[])contents.Clone(), uvRects);
        }
    }
}
