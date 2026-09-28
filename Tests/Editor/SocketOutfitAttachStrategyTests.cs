using NUnit.Framework;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    public class SocketOutfitAttachStrategyTests
    {
        private TestRig _rig;
        private OutfitWearer _character;
        private OutfitSkeleton _skeleton;
        private OutfitSlot _headSlot;
        private OutfitItem _hat;
        private SocketOutfitAttachStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _rig = new TestRig();
            _character = _rig.CreateCharacter(out _skeleton);
            _headSlot = _rig.Track(OutfitSlot.Create("Head"));
            _hat = _rig.Track(OutfitItem.Create("Hat", _headSlot, OutfitAttachMode.Socket, "hat"));
            _strategy = new SocketOutfitAttachStrategy();
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        [Test]
        public void Attach_ParentsToSocketKeepingPrefabOffset()
        {
            var socket = _rig.CreateSocket(_skeleton, "Head", _headSlot);
            var prefab = _rig.CreateRigidItem("Hat", new Vector3(0f, 0.05f, 0f));

            var instance = _rig.Track(_strategy.Attach(prefab, new OutfitAttachContext(_hat, _skeleton, _character.transform)));

            Assert.That(instance.transform.parent, Is.SameAs(socket));
            Assert.That(instance.transform.localPosition, Is.EqualTo(new Vector3(0f, 0.05f, 0f)));
        }

        [Test]
        public void Attach_WithoutSocket_ReturnsNull()
        {
            var prefab = _rig.CreateRigidItem("Hat", Vector3.zero);

            var instance = _strategy.Attach(prefab, new OutfitAttachContext(_hat, _skeleton, _character.transform));

            Assert.That(instance, Is.Null);
        }
    }
}
