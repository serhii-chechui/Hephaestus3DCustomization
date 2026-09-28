using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    public class SkinnedOutfitAttachStrategyTests
    {
        private TestRig _rig;
        private OutfitWearer _character;
        private OutfitSkeleton _skeleton;
        private OutfitItem _item;
        private SkinnedOutfitAttachStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _rig = new TestRig();
            _character = _rig.CreateCharacter(out _skeleton);
            _item = _rig.Track(OutfitItem.Create("Shirt", _rig.Track(OutfitSlot.Create("Torso")), OutfitAttachMode.Skinned, "shirt"));
            _strategy = new SkinnedOutfitAttachStrategy();
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        [Test]
        public void Attach_RebindsRenderersToCharacterBonesByName()
        {
            var prefab = _rig.CreateSkinnedItem("Shirt", extraBone: "Head");

            var instance = Attach(prefab);
            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();

            _skeleton.TryGetBone("Hips", out var hips);
            _skeleton.TryGetBone("Spine", out var spine);
            _skeleton.TryGetBone("Head", out var head);

            Assert.That(renderer.bones, Is.EqualTo(new[] { hips, spine, head }));
            Assert.That(renderer.rootBone, Is.SameAs(hips));
        }

        [Test]
        public void Attach_RemovesItemSkeleton()
        {
            var prefab = _rig.CreateSkinnedItem("Shirt", extraBone: "Head");

            var instance = Attach(prefab);

            Assert.That(instance.transform.Find("Hips"), Is.Null);
            Assert.That(instance.transform.Find("Mesh"), Is.Not.Null);
        }

        [Test]
        public void Attach_MissingBone_BindsToClosestAncestor()
        {
            var prefab = _rig.CreateSkinnedItem("Shirt", extraBone: "Tail");

            var instance = Attach(prefab);
            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();

            _skeleton.TryGetBone("Spine", out var spine);
            Assert.That(renderer.bones[2], Is.SameAs(spine));
        }

        [Test]
        public void Attach_KeepsPrefabUnchanged()
        {
            var prefab = _rig.CreateSkinnedItem("Shirt", extraBone: "Head");
            var prefabRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var prefabBones = prefabRenderer.bones;

            Attach(prefab);

            Assert.That(prefabRenderer.bones, Is.EqualTo(prefabBones));
            Assert.That(prefab.transform.Find("Hips"), Is.Not.Null);
        }

        [Test]
        public void Attach_ParentsInstanceToCharacterRoot()
        {
            var prefab = _rig.CreateSkinnedItem("Shirt", extraBone: "Head");

            var instance = Attach(prefab);

            Assert.That(instance.transform.parent, Is.SameAs(_character.transform));
            Assert.That(instance.name, Is.EqualTo("Shirt"));
        }

        [Test]
        public void Attach_ItemWithSeveralRenderers_RebindsAll()
        {
            var prefab = _rig.CreateSkinnedItem("Shirt", extraBone: "Head");
            var second = Object.Instantiate(prefab.transform.Find("Mesh").gameObject, prefab.transform, false);
            second.name = "Mesh2";

            var instance = Attach(prefab);
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();

            Assert.That(renderers.Length, Is.EqualTo(2));
            Assert.That(renderers.SelectMany(r => r.bones).All(bone => bone.IsChildOf(_skeleton.RootBone)), Is.True);
        }

        private GameObject Attach(GameObject prefab)
        {
            return _rig.Track(_strategy.Attach(prefab, new OutfitAttachContext(_item, _skeleton, _character.transform)));
        }
    }
}
