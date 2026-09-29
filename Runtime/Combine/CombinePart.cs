using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>One submesh of a source renderer and the material it is drawn with.</summary>
    internal readonly struct CombinePart
    {
        public CombinePart(CombineSource source, int subMesh, Material material)
        {
            Source = source;
            SubMesh = subMesh;
            Material = material;
        }

        public CombineSource Source { get; }

        public int SubMesh { get; }

        public Material Material { get; }

        public int[] Indices => Source.Indices[SubMesh];
    }
}
