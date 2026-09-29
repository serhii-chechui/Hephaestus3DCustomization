using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Bakes skinned renderers that share a skeleton (a body and the clothes rebound to it)
    /// into one mesh, so the character is skinned once and draws once per material instead of
    /// once per renderer. Submeshes with the same material merge; with
    /// <see cref="SkinnedMeshCombineSettings.Atlas"/> on, materials that differ only in
    /// textures merge too.
    /// </summary>
    /// <remarks>
    /// The meshes need Read/Write on, since they are read on the CPU. Renderers with blend
    /// shapes (e.g. a face with expressions) are skipped and stay separate. The sources must
    /// share the rest pose, as skinned outfit items already do. The sources aren't changed:
    /// turning them off is up to the caller, see <see cref="OutfitMeshCombiner"/>.
    /// </remarks>
    public static class SkinnedMeshCombiner
    {
        private const string LogTag = "[Hephaestus 3D Customization]";
        private const float BindPoseTolerance = 0.001f;

        /// <summary>
        /// Combines <paramref name="renderers"/>. Returns null when none of them can be combined.
        /// Renderers that draw nothing (no materials) are left out of the result.
        /// Dispose the result when it is no longer shown.
        /// </summary>
        public static CombinedSkinnedMesh Combine(IReadOnlyList<SkinnedMeshRenderer> renderers, SkinnedMeshCombineSettings settings = null, Object context = null)
        {
            if (renderers == null) throw new ArgumentNullException(nameof(renderers));

            settings = settings ?? new SkinnedMeshCombineSettings();

            var sources = new List<CombineSource>(renderers.Count);
            var skipped = new List<string>();
            var mismatched = new List<string>();
            var bones = new List<Transform>();
            var boneIndices = new Dictionary<Transform, int>();
            var bindposes = new List<Matrix4x4>();

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                if (!CombineSource.TryCreate(renderer, out var source, out var reason))
                {
                    skipped.Add($"'{renderer.name}' ({reason})");
                    continue;
                }

                if (!MapBones(source, bones, boneIndices, bindposes)) mismatched.Add($"'{renderer.name}'");

                sources.Add(source);
            }

            if (skipped.Count > 0)
            {
                Debug.LogWarning($"{LogTag} {skipped.Count} renderer(s) aren't combined: {string.Join(", ", skipped)}.", context);
            }

            if (mismatched.Count > 0)
            {
                Debug.LogWarning($"{LogTag} The rest pose of {string.Join(", ", mismatched)} differs from the other renderers; they may deform differently when combined.", context);
            }

            if (sources.Count == 0) return null;

            var parts = CollectParts(sources);

            if (parts.Count == 0) return null;

            var generated = new List<Object>();
            var tiles = settings.Atlas
                ? MaterialAtlasBuilder.Build(parts, settings, generated, context)
                : new Dictionary<Material, AtlasTile>();

            var mesh = BuildMesh(sources, parts, tiles, bindposes, out var materials);
            generated.Insert(0, mesh);

            var rootBone = sources[0].Renderer.rootBone != null ? sources[0].Renderer.rootBone : bones[0];
            var included = new List<SkinnedMeshRenderer>(sources.Count);

            foreach (var part in parts)
            {
                if (!included.Contains(part.Source.Renderer)) included.Add(part.Source.Renderer);
            }

            return new CombinedSkinnedMesh(mesh, materials, bones.ToArray(), rootBone, GetLocalBounds(sources, rootBone), included, generated);
        }

        // Aligns the source mesh space with the combined one through a bone both already have,
        // so every bone keeps one bind pose, and adds the source's new bones.
        private static bool MapBones(CombineSource source, List<Transform> bones, Dictionary<Transform, int> boneIndices, List<Matrix4x4> bindposes)
        {
            var toCombined = Matrix4x4.identity;

            for (var i = 0; i < source.Bones.Length; i++)
            {
                if (!boneIndices.TryGetValue(source.Bones[i], out var shared)) continue;

                toCombined = bindposes[shared].inverse * source.Bindposes[i];
                break;
            }

            var fromCombined = toCombined.inverse;
            var matches = true;

            for (var i = 0; i < source.Bones.Length; i++)
            {
                var bindpose = source.Bindposes[i] * fromCombined;

                if (boneIndices.TryGetValue(source.Bones[i], out var index))
                {
                    matches &= Approximately(bindposes[index], bindpose);
                }
                else
                {
                    index = bones.Count;
                    bones.Add(source.Bones[i]);
                    bindposes.Add(bindpose);
                    boneIndices.Add(source.Bones[i], index);
                }

                source.BoneMap[i] = index;
            }

            source.ToCombined = toCombined;
            source.NormalToCombined = fromCombined.transpose;

            return matches;
        }

        private static bool Approximately(Matrix4x4 a, Matrix4x4 b)
        {
            for (var i = 0; i < 16; i++)
            {
                if (Mathf.Abs(a[i] - b[i]) > BindPoseTolerance) return false;
            }

            return true;
        }

        // Submeshes without a material aren't drawn by the source renderer, so they're left out.
        private static List<CombinePart> CollectParts(List<CombineSource> sources)
        {
            var parts = new List<CombinePart>();

            foreach (var source in sources)
            {
                var count = Mathf.Min(source.Indices.Length, source.Materials.Length);

                for (var subMesh = 0; subMesh < count; subMesh++)
                {
                    var material = source.Materials[subMesh];

                    if (material != null && source.Indices[subMesh].Length > 0) parts.Add(new CombinePart(source, subMesh, material));
                }
            }

            return parts;
        }

        private static Mesh BuildMesh(List<CombineSource> sources, List<CombinePart> parts, Dictionary<Material, AtlasTile> tiles, List<Matrix4x4> bindposes, out Material[] materials)
        {
            var hasNormals = sources.Exists(source => source.Normals != null);
            var hasTangents = sources.Exists(source => source.Tangents != null);
            var hasColors = sources.Exists(source => source.Colors != null);
            var uvDimensions = new int[CombineSource.UvChannels];

            foreach (var source in sources)
            {
                for (var channel = 0; channel < uvDimensions.Length; channel++)
                {
                    uvDimensions[channel] = Mathf.Max(uvDimensions[channel], source.UvDimensions[channel]);
                }
            }

            var positions = new List<Vector3>();
            var normals = hasNormals ? new List<Vector3>() : null;
            var tangents = hasTangents ? new List<Vector4>() : null;
            var colors = hasColors ? new List<Color>() : null;
            var uvs = new List<Vector4>[uvDimensions.Length];
            var bonesPerVertex = new List<byte>();
            var weights = new List<BoneWeight1>();

            for (var channel = 0; channel < uvs.Length; channel++)
            {
                if (uvDimensions[channel] > 0) uvs[channel] = new List<Vector4>();
            }

            var submeshes = new Dictionary<Material, List<int>>();
            var order = new List<Material>();
            var remap = new int[0];

            // Every part gets its own copy of the vertices it uses, so a vertex shared by two
            // submeshes can get different atlas UVs.
            foreach (var part in parts)
            {
                var source = part.Source;
                tiles.TryGetValue(part.Material, out var tile);

                var material = tile != null ? tile.Material : part.Material;

                if (!submeshes.TryGetValue(material, out var triangles))
                {
                    triangles = new List<int>();
                    submeshes.Add(material, triangles);
                    order.Add(material);
                }

                if (remap.Length < source.Vertices.Length) remap = new int[source.Vertices.Length];

                for (var i = 0; i < source.Vertices.Length; i++)
                {
                    remap[i] = -1;
                }

                foreach (var index in part.Indices)
                {
                    var combined = remap[index];

                    if (combined < 0)
                    {
                        combined = positions.Count;
                        remap[index] = combined;

                        positions.Add(source.ToCombined.MultiplyPoint3x4(source.Vertices[index]));

                        normals?.Add(source.Normals != null
                            ? source.NormalToCombined.MultiplyVector(source.Normals[index]).normalized
                            : Vector3.up);

                        if (tangents != null)
                        {
                            var tangent = source.Tangents != null ? source.Tangents[index] : new Vector4(1f, 0f, 0f, 1f);
                            Vector3 direction = source.ToCombined.MultiplyVector(tangent).normalized;
                            tangents.Add(new Vector4(direction.x, direction.y, direction.z, tangent.w));
                        }

                        colors?.Add(source.Colors != null ? source.Colors[index] : Color.white);

                        for (var channel = 0; channel < uvs.Length; channel++)
                        {
                            if (uvs[channel] == null) continue;

                            var uv = source.Uvs[channel] != null ? source.Uvs[channel][index] : Vector4.zero;

                            if (channel == 0 && tile != null)
                            {
                                var remapped = tile.Remap(uv);
                                uv = new Vector4(remapped.x, remapped.y, uv.z, uv.w);
                            }

                            uvs[channel].Add(uv);
                        }

                        var count = source.BonesPerVertex[index];
                        var start = source.WeightStart[index];

                        for (var k = 0; k < count; k++)
                        {
                            var weight = source.Weights[start + k];
                            weight.boneIndex = source.BoneMap[weight.boneIndex];
                            weights.Add(weight);
                        }

                        bonesPerVertex.Add(count);
                    }

                    triangles.Add(combined);
                }
            }

            var mesh = new Mesh
            {
                name = "Combined Outfit",
                indexFormat = positions.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
            };

            mesh.SetVertices(positions);
            if (normals != null) mesh.SetNormals(normals);
            if (tangents != null) mesh.SetTangents(tangents);
            if (colors != null) mesh.SetColors(colors);

            for (var channel = 0; channel < uvs.Length; channel++)
            {
                if (uvs[channel] != null) SetUvs(mesh, channel, uvs[channel], uvDimensions[channel]);
            }

            mesh.subMeshCount = order.Count;

            for (var subMesh = 0; subMesh < order.Count; subMesh++)
            {
                mesh.SetTriangles(submeshes[order[subMesh]], subMesh, false);
            }

            mesh.bindposes = bindposes.ToArray();

            using (var nativeBonesPerVertex = new NativeArray<byte>(bonesPerVertex.ToArray(), Allocator.Temp))
            using (var nativeWeights = new NativeArray<BoneWeight1>(weights.ToArray(), Allocator.Temp))
            {
                mesh.SetBoneWeights(nativeBonesPerVertex, nativeWeights);
            }

            mesh.RecalculateBounds();

            materials = order.ToArray();
            return mesh;
        }

        // Keeps the UV dimension of the sources, so 2D UVs don't grow the vertex.
        private static void SetUvs(Mesh mesh, int channel, List<Vector4> uvs, int dimension)
        {
            if (dimension >= 4)
            {
                mesh.SetUVs(channel, uvs);
            }
            else if (dimension == 3)
            {
                mesh.SetUVs(channel, uvs.ConvertAll(uv => (Vector3)uv));
            }
            else
            {
                mesh.SetUVs(channel, uvs.ConvertAll(uv => (Vector2)uv));
            }
        }

        // The sources' bounds relative to the combined root bone. Works for renderers that are
        // turned off, unlike Renderer.bounds.
        private static Bounds GetLocalBounds(List<CombineSource> sources, Transform rootBone)
        {
            var bounds = new Bounds();
            var isEmpty = true;

            foreach (var source in sources)
            {
                var local = source.Renderer.localBounds;
                var space = source.Renderer.rootBone != null ? source.Renderer.rootBone : source.Renderer.transform;

                for (var corner = 0; corner < 8; corner++)
                {
                    var point = local.center + Vector3.Scale(local.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));

                    point = rootBone.InverseTransformPoint(space.TransformPoint(point));

                    if (isEmpty)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        isEmpty = false;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            return bounds;
        }
    }
}
