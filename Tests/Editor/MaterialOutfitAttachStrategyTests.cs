using NUnit.Framework;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    public class MaterialOutfitAttachStrategyTests
    {
        private TestRig _rig;
        private OutfitWearer _character;
        private OutfitSkeleton _skeleton;
        private OutfitBodyPart _skin;
        private Renderer _skinRenderer;
        private Material _original;
        private Material _tanned;
        private OutfitItem _item;
        private MaterialOutfitAttachStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _rig = new TestRig();
            _character = _rig.CreateCharacter(out _skeleton);
            _skin = _rig.Track(OutfitBodyPart.Create("Skin"));
            _skinRenderer = _rig.CreateBodyPart(_skeleton, _skin);
            _original = _rig.Track(new Material(Shader.Find("Standard")) { name = "Original" });
            _tanned = _rig.Track(new Material(Shader.Find("Standard")) { name = "Tanned" });
            _skinRenderer.sharedMaterial = _original;
            _item = _rig.Track(OutfitItem.Create("Tan", _rig.Track(OutfitSlot.Create("SkinTone")), OutfitAttachMode.Material, "tan"));
            _strategy = new MaterialOutfitAttachStrategy();
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        [Test]
        public void Attach_PutsMaterialsOnBodyPartRenderers()
        {
            var instance = _rig.Track(_strategy.Attach(CreateMaterialPrefab(), Context()));

            Assert.That(instance, Is.Not.Null);
            Assert.That(_skinRenderer.sharedMaterial, Is.SameAs(_tanned));
        }

        [Test]
        public void Detach_RestoresPreviousMaterials()
        {
            var instance = _strategy.Attach(CreateMaterialPrefab(), Context());

            _strategy.Detach(instance);

            Assert.That(_skinRenderer.sharedMaterial, Is.SameAs(_original));
            Assert.That(instance == null, Is.True);
        }

        [Test]
        public void Detach_OfAnItemUnderAnother_KeepsTheTopItemsMaterials()
        {
            var natural = _strategy.Attach(CreateMaterialPrefab(_original), Context());
            var tanned = _rig.Track(_strategy.Attach(CreateMaterialPrefab(_tanned), Context()));

            // The wearer attaches the new item before it takes the old one off.
            _strategy.Detach(natural);

            Assert.That(_skinRenderer.sharedMaterial, Is.SameAs(_tanned));
        }

        [Test]
        public void Detach_InAnyOrder_EndsWithTheRenderersOwnMaterials()
        {
            var first = _strategy.Attach(CreateMaterialPrefab(_tanned), Context());
            var second = _strategy.Attach(CreateMaterialPrefab(_tanned), Context());

            _strategy.Detach(first);
            _strategy.Detach(second);

            Assert.That(_skinRenderer.sharedMaterial, Is.SameAs(_original));
        }

        [Test]
        public void Attach_PrefabWithoutMaterialSet_ReturnsNull()
        {
            var prefab = _rig.Track(new GameObject("NoSet"));

            Assert.That(_strategy.Attach(prefab, Context()), Is.Null);
            Assert.That(_skinRenderer.sharedMaterial, Is.SameAs(_original));
        }

        private GameObject CreateMaterialPrefab(Material material = null)
        {
            var prefab = _rig.Track(new GameObject("Skin"));
            prefab.AddComponent<OutfitMaterialSet>().Overrides.Add(new OutfitMaterialOverride { bodyPart = _skin, materials = new[] { material != null ? material : _tanned } });
            return prefab;
        }

        private OutfitAttachContext Context()
        {
            return new OutfitAttachContext(_item, _skeleton, _character.transform);
        }
    }
}
