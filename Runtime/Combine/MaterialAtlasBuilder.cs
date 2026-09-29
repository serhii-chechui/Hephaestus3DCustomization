using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>
    /// Replaces materials that differ only in textures with one material per group whose
    /// textures are atlases. Materials are grouped by shader, keywords, render queue and tint
    /// (_BaseColor, _Color); every other property comes from the first material of a group.
    /// A material stays as it is when the UVs of a submesh drawn with it leave the 0..1 range
    /// (tiling textures can't be atlased). The atlases are RenderTextures filled on the GPU,
    /// so the source textures don't need Read/Write.
    /// </summary>
    internal static class MaterialAtlasBuilder
    {
        private const string LogTag = "[Hephaestus 3D Customization]";
        private const float UvTolerance = 0.001f;
        private const int EmptyTextureSize = 32;

        private static readonly string[] TintProperties = { "_BaseColor", "_Color" };

        public static Dictionary<Material, AtlasTile> Build(List<CombinePart> parts, SkinnedMeshCombineSettings settings, List<Object> generated, Object context)
        {
            var tiles = new Dictionary<Material, AtlasTile>();

            if (settings.AtlasTextures == null || settings.AtlasTextures.Count == 0) return tiles;

            if (!CanBuildAtlases())
            {
                Debug.LogWarning($"{LogTag} This device can't copy textures on the GPU; the combined mesh keeps its materials without atlases.", context);
                return tiles;
            }

            foreach (var group in GroupMaterials(parts, settings))
            {
                BuildGroup(group, settings, tiles, generated);
            }

            return tiles;
        }

        private static bool CanBuildAtlases()
        {
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null
                   && SystemInfo.copyTextureSupport != CopyTextureSupport.None;
        }

        private static IEnumerable<List<Material>> GroupMaterials(List<CombinePart> parts, SkinnedMeshCombineSettings settings)
        {
            var rejected = new HashSet<Material>();

            foreach (var part in parts)
            {
                if (!rejected.Contains(part.Material) && !CanAtlas(part, settings)) rejected.Add(part.Material);
            }

            var groups = new Dictionary<string, List<Material>>();
            var order = new List<List<Material>>();

            foreach (var part in parts)
            {
                if (rejected.Contains(part.Material)) continue;

                var key = GetGroupKey(part.Material, settings);

                if (!groups.TryGetValue(key, out var group))
                {
                    group = new List<Material>();
                    groups.Add(key, group);
                    order.Add(group);
                }

                if (!group.Contains(part.Material)) group.Add(part.Material);
            }

            // One material gains nothing from an atlas.
            return order.FindAll(group => group.Count > 1);
        }

        private static bool CanAtlas(CombinePart part, SkinnedMeshCombineSettings settings)
        {
            var material = part.Material;
            var layout = GetLayoutProperty(material, settings);

            if (layout == null) return false;

            foreach (var property in settings.AtlasTextures)
            {
                if (property == null || !material.HasProperty(property.name)) continue;

                var texture = material.GetTexture(property.name);

                if (texture != null && texture.dimension != TextureDimension.Tex2D) return false;
            }

            var uvs = part.Source.Uvs[0];

            if (uvs == null) return false;

            var scale = material.GetTextureScale(layout.name);
            var offset = material.GetTextureOffset(layout.name);

            foreach (var index in part.Indices)
            {
                var uv = Vector2.Scale(uvs[index], scale) + offset;

                if (uv.x < -UvTolerance || uv.y < -UvTolerance || uv.x > 1f + UvTolerance || uv.y > 1f + UvTolerance) return false;
            }

            return true;
        }

        private static AtlasTextureProperty GetLayoutProperty(Material material, SkinnedMeshCombineSettings settings)
        {
            foreach (var property in settings.AtlasTextures)
            {
                if (property != null && !string.IsNullOrEmpty(property.name) && material.HasProperty(property.name)) return property;
            }

            return null;
        }

        private static string GetGroupKey(Material material, SkinnedMeshCombineSettings settings)
        {
            var keywords = material.shaderKeywords;
            System.Array.Sort(keywords, System.StringComparer.Ordinal);

            var key = new StringBuilder();
            key.Append(material.shader.GetInstanceID()).Append('|')
                .Append(material.renderQueue).Append('|')
                .Append(GetLayoutProperty(material, settings).name).Append('|')
                .Append(string.Join(" ", keywords));

            foreach (var tint in TintProperties)
            {
                if (material.HasProperty(tint)) key.Append('|').Append(material.GetColor(tint));
            }

            return key.ToString();
        }

        private static void BuildGroup(List<Material> materials, SkinnedMeshCombineSettings settings, Dictionary<Material, AtlasTile> tiles, List<Object> generated)
        {
            var first = materials[0];
            var layoutProperty = GetLayoutProperty(first, settings);
            var sizes = new List<Vector2Int>(materials.Count);

            foreach (var material in materials)
            {
                var texture = material.GetTexture(layoutProperty.name);
                sizes.Add(texture != null ? new Vector2Int(texture.width, texture.height) : new Vector2Int(EmptyTextureSize, EmptyTextureSize));
            }

            var layout = AtlasPacker.Pack(sizes, settings.AtlasMaxSize, settings.AtlasPadding);

            if (layout == null) return;

            var atlasMaterial = new Material(first) { name = first.name + " (Atlas)" };
            generated.Add(atlasMaterial);

            // Properties that hold the same textures (e.g. _BaseMap and _MainTex) share an atlas.
            var built = new List<KeyValuePair<Texture[], RenderTexture>>();

            foreach (var property in settings.AtlasTextures)
            {
                if (property == null || string.IsNullOrEmpty(property.name) || !first.HasProperty(property.name)) continue;

                var textures = new Texture[materials.Count];
                var hasTexture = false;

                for (var i = 0; i < materials.Count; i++)
                {
                    textures[i] = materials[i].GetTexture(property.name);
                    hasTexture |= textures[i] != null;
                }

                if (!hasTexture && property != layoutProperty) continue;

                var atlas = FindBuilt(built, textures, property.linear);

                if (atlas == null)
                {
                    atlas = BuildAtlas(layout, textures, property, settings.AtlasPadding, first.name);
                    generated.Add(atlas);
                    built.Add(new KeyValuePair<Texture[], RenderTexture>(textures, atlas));
                }

                atlasMaterial.SetTexture(property.name, atlas);
                atlasMaterial.SetTextureScale(property.name, Vector2.one);
                atlasMaterial.SetTextureOffset(property.name, Vector2.zero);
            }

            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                tiles[material] = new AtlasTile(
                    atlasMaterial,
                    layout.UvRects[i],
                    material.GetTextureScale(layoutProperty.name),
                    material.GetTextureOffset(layoutProperty.name));
            }
        }

        private static RenderTexture FindBuilt(List<KeyValuePair<Texture[], RenderTexture>> built, Texture[] textures, bool linear)
        {
            foreach (var pair in built)
            {
                if (pair.Value.sRGB == linear) continue;

                var same = true;

                for (var i = 0; i < textures.Length && same; i++)
                {
                    same = pair.Key[i] == textures[i];
                }

                if (same) return pair.Value;
            }

            return null;
        }

        private static RenderTexture BuildAtlas(AtlasLayout layout, Texture[] textures, AtlasTextureProperty property, int padding, string materialName)
        {
            padding = Mathf.Max(0, padding);

            var atlas = new RenderTexture(CreateDescriptor(layout.Width, layout.Height, property.linear, true))
            {
                name = $"{materialName} {property.name} (Atlas)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear
            };

            atlas.Create();
            Clear(atlas, property.emptyColor);

            for (var i = 0; i < textures.Length; i++)
            {
                var tile = layout.Tiles[i];
                var content = layout.Contents[i];
                var temporary = RenderTexture.GetTemporary(CreateDescriptor(tile.width, tile.height, property.linear, false));

                if (textures[i] == null)
                {
                    Clear(temporary, property.emptyColor);
                }
                else
                {
                    // Samples a little past the texture's edges; with clamping, the padding
                    // repeats the edge pixels.
                    var texture = textures[i];
                    var wrapMode = texture.wrapMode;
                    texture.wrapMode = TextureWrapMode.Clamp;

                    Graphics.Blit(
                        texture,
                        temporary,
                        new Vector2((float)tile.width / content.width, (float)tile.height / content.height),
                        new Vector2((float)-padding / content.width, (float)-padding / content.height));

                    texture.wrapMode = wrapMode;
                }

                Graphics.CopyTexture(temporary, 0, 0, 0, 0, tile.width, tile.height, atlas, 0, 0, tile.x, tile.y);
                RenderTexture.ReleaseTemporary(temporary);
            }

            atlas.GenerateMips();
            return atlas;
        }

        private static RenderTextureDescriptor CreateDescriptor(int width, int height, bool linear, bool mipmaps)
        {
            return new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = !linear,
                useMipMap = mipmaps,
                autoGenerateMips = false
            };
        }

        private static void Clear(RenderTexture target, Color color)
        {
            var active = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(false, true, color);
            RenderTexture.active = active;
        }
    }
}
