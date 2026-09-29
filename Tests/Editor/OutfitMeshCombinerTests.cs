using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    public class OutfitMeshCombinerTests
    {
        private SynchronizationContext _previousContext;
        private TestRig _rig;
        private FakeOutfitAssetProvider _provider;
        private OutfitWearer _wearer;
        private OutfitSkeleton _skeleton;
        private OutfitMeshCombiner _combiner;
        private OutfitBodyPart _torso;
        private SkinnedMeshRenderer _torsoRenderer;
        private SkinnedMeshRenderer _headRenderer;

        [SetUp]
        public void SetUp()
        {
            // Without a context, continuations run inline, so tasks complete within the test.
            _previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);

            _rig = new TestRig();
            _provider = new FakeOutfitAssetProvider();
            _wearer = _rig.CreateCharacter(out _skeleton);
            _wearer.Construct(_provider);

            var material = _rig.CreateMaterial("Skin", Color.white);
            _torso = _rig.Track(OutfitBodyPart.Create("Torso"));
            _torsoRenderer = _rig.CreateSkinnedRenderer(_skeleton, "TorsoMesh", _wearer.transform, material);
            _torsoRenderer.gameObject.AddComponent<OutfitBodyPartRenderer>().BodyPart = _torso;
            _headRenderer = _rig.CreateSkinnedRenderer(_skeleton, "HeadMesh", _wearer.transform, material);
            _skeleton.Rebuild();

            _combiner = _wearer.gameObject.AddComponent<OutfitMeshCombiner>();
            _combiner.Settings.Atlas = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_combiner);
            _rig.Dispose();
            SynchronizationContext.SetSynchronizationContext(_previousContext);
        }

        [Test]
        public void Combine_HidesSourcesAndShowsCombinedRenderer()
        {
            Assert.That(_combiner.Combine(), Is.True);

            Assert.That(_combiner.IsCombined, Is.True);
            Assert.That(_combiner.CombinedRenderer.enabled, Is.True);
            Assert.That(_combiner.CombinedRenderer.sharedMesh.vertexCount, Is.EqualTo(8));
            Assert.That(_torsoRenderer.enabled, Is.False);
            Assert.That(_headRenderer.enabled, Is.False);
        }

        [Test]
        public void Separate_ShowsSourcesAgain()
        {
            _combiner.Combine();

            _combiner.Separate();

            Assert.That(_combiner.IsCombined, Is.False);
            Assert.That(_combiner.CombinedRenderer.enabled, Is.False);
            Assert.That(_torsoRenderer.enabled, Is.True);
            Assert.That(_headRenderer.enabled, Is.True);
        }

        [Test]
        public void Combine_AfterEquip_IncludesItemAndLeavesOutHiddenBodyParts()
        {
            _combiner.Combine();

            var slot = _rig.Track(OutfitSlot.Create("Torso"));
            var shirt = _rig.Track(OutfitItem.Create("Shirt", slot, OutfitAttachMode.Skinned, "shirt", new[] { _torso }));
            var prefab = _rig.CreateSkinnedItem("Shirt", extraBone: "Head");
            prefab.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = _torsoRenderer.sharedMaterial;
            _provider.Register("shirt", prefab);
            _wearer.EquipAsync(shirt).Wait();

            _combiner.Combine();

            Assert.That(_combiner.CombinedRenderer.sharedMesh.vertexCount, Is.EqualTo(4 + 3));
            Assert.That(_torsoRenderer.enabled, Is.False);

            _combiner.Separate();

            Assert.That(_torsoRenderer.enabled, Is.False, "the shirt still hides the torso");
            Assert.That(_headRenderer.enabled, Is.True);
        }

        [Test]
        public void Combine_SingleRenderer_StaysSeparate()
        {
            _headRenderer.enabled = false;

            Assert.That(_combiner.Combine(), Is.False);
            Assert.That(_torsoRenderer.enabled, Is.True);
        }
    }
}
