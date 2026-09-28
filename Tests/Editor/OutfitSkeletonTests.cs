using NUnit.Framework;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    public class OutfitSkeletonTests
    {
        private TestRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new TestRig();
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        [Test]
        public void TryGetBone_FindsBonesUnderRootBone()
        {
            _rig.CreateCharacter(out var skeleton);

            Assert.That(skeleton.TryGetBone("Spine", out var spine), Is.True);
            Assert.That(spine.name, Is.EqualTo("Spine"));
            Assert.That(skeleton.TryGetBone("Tail", out _), Is.False);
            Assert.That(skeleton.BoneCount, Is.EqualTo(4));
        }

        [Test]
        public void Rebuild_SkipsEquippedOutfits()
        {
            _rig.CreateCharacter(out var skeleton);
            skeleton.TryGetBone("Head", out var head);

            var outfit = new GameObject("Hat", typeof(OutfitInstance));
            outfit.transform.SetParent(head, false);
            new GameObject("Brim").transform.SetParent(outfit.transform, false);

            skeleton.Rebuild();

            Assert.That(skeleton.TryGetBone("Hat", out _), Is.False);
            Assert.That(skeleton.TryGetBone("Brim", out _), Is.False);
        }

        [Test]
        public void TryGetSocket_FindsSocketBySlot()
        {
            _rig.CreateCharacter(out var skeleton);
            var slot = _rig.Track(OutfitSlot.Create("Head"));
            var socket = _rig.CreateSocket(skeleton, "Head", slot);

            Assert.That(skeleton.TryGetSocket(slot, out var found), Is.True);
            Assert.That(found, Is.SameAs(socket));
        }
    }
}
