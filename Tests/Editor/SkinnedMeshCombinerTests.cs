using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    public class SkinnedMeshCombinerTests
    {
        private TestRig _rig;
        private OutfitWearer _character;
        private OutfitSkeleton _skeleton;
        private Material _red;
        private Material _green;
        private readonly List<CombinedSkinnedMesh> _results = new List<CombinedSkinnedMesh>();

        [SetUp]
        public void SetUp()
        {
            _rig = new TestRig();
            _character = _rig.CreateCharacter(out _skeleton);
            _red = _rig.CreateMaterial("Red", Color.red);
            _green = _rig.CreateMaterial("Green", Color.green);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var result in _results)
            {
                result.Dispose();
            }

            _results.Clear();
            _rig.Dispose();
        }

        [Test]
        public void Combine_SharedBonesAndMaterial_OneSubmeshAndUnionOfBones()
        {
            var body = CreateRenderer("Body", _red);
            var shirt = CreateRenderer("Shirt", _red);

            var result = Combine(new SkinnedMeshCombineSettings { Atlas = false }, body, shirt);

            Assert.That(result.Sources, Is.EqualTo(new[] { body, shirt }));
            Assert.That(result.Bones, Is.EquivalentTo(body.bones));
            Assert.That(result.Mesh.subMeshCount, Is.EqualTo(1));
            Assert.That(result.Mesh.vertexCount, Is.EqualTo(body.sharedMesh.vertexCount + shirt.sharedMesh.vertexCount));
            Assert.That(result.Materials, Is.EqualTo(new[] { _red }));
        }

        [Test]
        public void Combine_DifferentMaterialsWithoutAtlas_OneSubmeshPerMaterial()
        {
            var body = CreateRenderer("Body", _red);
            var shirt = CreateRenderer("Shirt", _green);

            var result = Combine(new SkinnedMeshCombineSettings { Atlas = false }, body, shirt);

            Assert.That(result.Mesh.subMeshCount, Is.EqualTo(2));
            Assert.That(result.Materials, Is.EqualTo(new[] { _red, _green }));
        }

        [Test]
        public void Combine_DifferentMeshSpaces_DeformsLikeSources()
        {
            var body = CreateRenderer("Body", _red);
            var shirt = CreateRenderer("Shirt", _green, meshOrigin: new Vector3(0.5f, -2f, 1f));

            var result = Combine(new SkinnedMeshCombineSettings { Atlas = false }, body, shirt);
            var combined = ShowOn(result);

            _skeleton.TryGetBone("Spine", out var spine);
            spine.localRotation = Quaternion.Euler(30f, 45f, 10f);
            spine.localScale = new Vector3(1f, 1.2f, 1f);

            var expected = new[] { body, shirt }
                .SelectMany(source => Enumerable.Range(0, source.sharedMesh.vertexCount).Select(vertex => TestRig.Skin(source, vertex)))
                .ToList();
            var actual = Enumerable.Range(0, result.Mesh.vertexCount).Select(vertex => TestRig.Skin(combined, vertex)).ToList();

            Assert.That(actual.Count, Is.EqualTo(expected.Count));

            for (var i = 0; i < expected.Count; i++)
            {
                Assert.That(Vector3.Distance(actual[i], expected[i]), Is.LessThan(0.0001f), $"vertex {i}");
            }
        }

        [Test]
        public void Combine_SkipsRenderersThatCantBeCombined_WithOneWarning()
        {
            var body = CreateRenderer("Body", _red);
            var face = CreateRenderer("Face", _red);
            face.sharedMesh.AddBlendShapeFrame("Smile", 100f, new Vector3[face.sharedMesh.vertexCount], null, null);
            var locked = CreateRenderer("Locked", _red);
            locked.sharedMesh.UploadMeshData(true);

            LogAssert.Expect(LogType.Warning, new Regex(@"2 renderer\(s\) aren't combined: 'Face' \(has blend shapes\), 'Locked' \(mesh Read/Write is off\)"));

            var result = Combine(null, body, face, locked);

            Assert.That(result.Sources, Is.EqualTo(new[] { body }));
        }

        [Test]
        public void Combine_Atlas_MergesMaterialsIntoOneAndRemapsUvs()
        {
            RequireGpu();

            var body = CreateRenderer("Body", _red);
            var shirt = CreateRenderer("Shirt", _green);

            var result = Combine(null, body, shirt);

            Assert.That(result.Mesh.subMeshCount, Is.EqualTo(1));

            var atlas = result.Materials[0].mainTexture as RenderTexture;
            Assert.That(atlas, Is.Not.Null);

            var uvs = new List<Vector2>();
            result.Mesh.GetUVs(0, uvs);
            var count = body.sharedMesh.vertexCount;

            // The center of each source triangle lands in that source's tile.
            Assert.That(Sample(atlas, uvs[2]), Is.EqualTo((Color32)Color.red));
            Assert.That(Sample(atlas, uvs[count + 2]), Is.EqualTo((Color32)Color.green));
        }

        [Test]
        public void Combine_Atlas_TilingUvs_KeepMaterial()
        {
            RequireGpu();

            var body = CreateRenderer("Body", _red);
            var shirt = CreateRenderer("Shirt", _green, uvOffset: new Vector2(2f, 0f));

            var result = Combine(null, body, shirt);

            Assert.That(result.Materials, Is.EqualTo(new[] { _red, _green }));
        }

        [Test]
        public void Dispose_DestroysGeneratedAssets()
        {
            RequireGpu();

            var result = Combine(null, CreateRenderer("Body", _red), CreateRenderer("Shirt", _green));
            var mesh = result.Mesh;
            var material = result.Materials[0];

            result.Dispose();

            Assert.That(mesh == null, Is.True);
            Assert.That(material == null, Is.True);
            Assert.That(result.IsDisposed, Is.True);
            Assert.That(_red == null, Is.False);
        }

        private SkinnedMeshRenderer CreateRenderer(string rendererName, Material material, Vector3 meshOrigin = default, Vector2 uvOffset = default)
        {
            return _rig.CreateSkinnedRenderer(_skeleton, rendererName, _character.transform, material, meshOrigin, uvOffset);
        }

        private CombinedSkinnedMesh Combine(SkinnedMeshCombineSettings settings, params SkinnedMeshRenderer[] renderers)
        {
            var result = SkinnedMeshCombiner.Combine(renderers, settings);
            _results.Add(result);
            return result;
        }

        private SkinnedMeshRenderer ShowOn(CombinedSkinnedMesh result)
        {
            var target = _rig.Track(new GameObject("Combined")).AddComponent<SkinnedMeshRenderer>();
            target.transform.SetParent(_character.transform, false);
            result.ApplyTo(target);
            return target;
        }

        private static Color32 Sample(RenderTexture atlas, Vector2 uv)
        {
            var active = RenderTexture.active;
            var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);

            try
            {
                RenderTexture.active = atlas;
                pixel.ReadPixels(new Rect(Mathf.FloorToInt(uv.x * atlas.width), Mathf.FloorToInt(uv.y * atlas.height), 1, 1), 0, 0);
                pixel.Apply();
                return pixel.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = active;
                Object.DestroyImmediate(pixel);
            }
        }

        private static void RequireGpu()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Atlases need a graphics device.");
        }
    }
}
