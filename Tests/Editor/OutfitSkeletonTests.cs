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

        [Test]
        public void TryGetBone_UsesAliases()
        {
            _rig.CreateCharacter(out var skeleton);
            skeleton.BoneAliases.Add(new OutfitBoneAlias { alias = "Chest", bone = "Spine" });
            skeleton.Rebuild();

            Assert.That(skeleton.TryGetBone("Chest", out var bone), Is.True);
            Assert.That(bone.name, Is.EqualTo("Spine"));
        }

        [Test]
        public void TryGetBone_IgnoresCaseOnlyWhenEnabled()
        {
            _rig.CreateCharacter(out var skeleton);

            Assert.That(skeleton.TryGetBone("spine", out _), Is.False);

            skeleton.IgnoreCase = true;

            Assert.That(skeleton.TryGetBone("spine", out var bone), Is.True);
            Assert.That(bone.name, Is.EqualTo("Spine"));
        }

        [Test]
        public void TryGetBone_IgnoresNamespacesOnlyWhenEnabled()
        {
            _rig.CreateCharacter(out var skeleton);

            Assert.That(skeleton.TryGetBone("mixamorig:Hips", out _), Is.False);

            skeleton.IgnoreNamespaces = true;

            Assert.That(skeleton.TryGetBone("mixamorig:Hips", out var bone), Is.True);
            Assert.That(bone.name, Is.EqualTo("Hips"));
        }

        [Test]
        public void TryGetSocket_TellsSocketsOfOneSlotApartById()
        {
            _rig.CreateCharacter(out var skeleton);
            var ears = _rig.Track(OutfitSlot.Create("Ears"));
            var left = _rig.CreateSocket(skeleton, "Head", ears, "Left");
            var right = _rig.CreateSocket(skeleton, "Head", ears, "Right");

            Assert.That(skeleton.TryGetSocket(ears, "Left", out var foundLeft), Is.True);
            Assert.That(skeleton.TryGetSocket(ears, "Right", out var foundRight), Is.True);
            Assert.That(foundLeft, Is.SameAs(left));
            Assert.That(foundRight, Is.SameAs(right));
            Assert.That(skeleton.TryGetSocket(ears, out _), Is.False);
        }
    }
}
