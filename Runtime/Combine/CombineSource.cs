using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>A renderer going into a combined mesh, with the mesh data read from it.</summary>
    internal sealed class CombineSource
    {
        public const int UvChannels = 4;

        private CombineSource(SkinnedMeshRenderer renderer, Mesh mesh, Transform[] bones, Matrix4x4[] bindposes)
        {
            Renderer = renderer;
            Mesh = mesh;
            Bones = bones;
            Bindposes = bindposes;
            BoneMap = new int[bones.Length];
            Materials = renderer.sharedMaterials;

            Vertices = mesh.vertices;
            Normals = mesh.HasVertexAttribute(VertexAttribute.Normal) ? mesh.normals : null;
            Tangents = mesh.HasVertexAttribute(VertexAttribute.Tangent) ? mesh.tangents : null;
            Colors = mesh.HasVertexAttribute(VertexAttribute.Color) ? mesh.colors : null;
            Uvs = new List<Vector4>[UvChannels];
            UvDimensions = new int[UvChannels];

            for (var channel = 0; channel < UvChannels; channel++)
            {
                var attribute = VertexAttribute.TexCoord0 + channel;

                if (!mesh.HasVertexAttribute(attribute)) continue;

                Uvs[channel] = new List<Vector4>(Vertices.Length);
                mesh.GetUVs(channel, Uvs[channel]);
                UvDimensions[channel] = GetDimension(Uvs[channel]);
            }

            Indices = new int[mesh.subMeshCount][];

            for (var subMesh = 0; subMesh < Indices.Length; subMesh++)
            {
                Indices[subMesh] = mesh.GetIndices(subMesh);
            }

            // The mesh owns these arrays; they stay valid while the mesh isn't changed.
            BonesPerVertex = mesh.GetBonesPerVertex();
            Weights = mesh.GetAllBoneWeights();
            WeightStart = new int[Vertices.Length];

            for (int vertex = 0, start = 0; vertex < WeightStart.Length; vertex++)
            {
                WeightStart[vertex] = start;
                start += BonesPerVertex[vertex];
            }
        }

        public SkinnedMeshRenderer Renderer { get; }

        public Mesh Mesh { get; }

        public Transform[] Bones { get; }

        public Matrix4x4[] Bindposes { get; }

        /// <summary>Index of every source bone in the combined bone list.</summary>
        public int[] BoneMap { get; }

        public Material[] Materials { get; }

        /// <summary>From the source mesh space to the combined mesh space.</summary>
        public Matrix4x4 ToCombined { get; set; } = Matrix4x4.identity;

        /// <summary>Transforms normals from the source mesh space to the combined mesh space.</summary>
        public Matrix4x4 NormalToCombined { get; set; } = Matrix4x4.identity;

        public Vector3[] Vertices { get; }

        public Vector3[] Normals { get; }

        public Vector4[] Tangents { get; }

        public Color[] Colors { get; }

        public List<Vector4>[] Uvs { get; }

        public int[] UvDimensions { get; }

        public int[][] Indices { get; }

        public NativeArray<byte> BonesPerVertex { get; }

        public NativeArray<BoneWeight1> Weights { get; }

        public int[] WeightStart { get; }

        /// <summary>Reads <paramref name="renderer"/>, or explains why it can't be combined.</summary>
        public static bool TryCreate(SkinnedMeshRenderer renderer, out CombineSource source, out string reason)
        {
            source = null;
            reason = GetSkipReason(renderer, out var bones);

            if (reason != null) return false;

            source = new CombineSource(renderer, renderer.sharedMesh, bones, renderer.sharedMesh.bindposes);
            return true;
        }

        // Reads the dimension from the data: the attribute's own dimension needs a newer Unity.
        private static int GetDimension(List<Vector4> uvs)
        {
            var dimension = 2;

            foreach (var uv in uvs)
            {
                if (uv.w != 0f) return 4;
                if (uv.z != 0f) dimension = 3;
            }

            return dimension;
        }

        private static string GetSkipReason(SkinnedMeshRenderer renderer, out Transform[] bones)
        {
            bones = null;

            var mesh = renderer.sharedMesh;

            if (mesh == null) return "no mesh";
            if (!mesh.isReadable) return "mesh Read/Write is off";
            if (mesh.blendShapeCount > 0) return "has blend shapes";
            if (mesh.vertexCount == 0) return "empty mesh";

            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (mesh.GetTopology(subMesh) != MeshTopology.Triangles) return "not made of triangles";
            }

            bones = renderer.bones;

            if (bones.Length == 0) return "no bones";
            if (mesh.bindposes.Length != bones.Length) return "bind poses don't match the bones";
            if (mesh.GetBonesPerVertex().Length != mesh.vertexCount) return "no bone weights";

            foreach (var bone in bones)
            {
                if (bone == null) return "a bone is missing";
            }

            return null;
        }
    }
}
