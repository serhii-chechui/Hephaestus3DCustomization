using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    public class OutfitWearerTests
    {
        private SynchronizationContext _previousContext;
        private TestRig _rig;
        private FakeOutfitAssetProvider _provider;
        private OutfitWearer _wearer;
        private OutfitSkeleton _skeleton;
        private OutfitSlot _torso;
        private OutfitSlot _head;

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
            _torso = _rig.Track(OutfitSlot.Create("Torso"));
            _head = _rig.Track(OutfitSlot.Create("Head"));
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
            SynchronizationContext.SetSynchronizationContext(_previousContext);
        }

        [Test]
        public void Equip_WearsItemAndRaisesEvent()
        {
            var shirt = CreateSkinnedItem("Shirt", _torso);
            var raised = new List<OutfitItem>();
            _wearer.Equipped += raised.Add;

            var result = _wearer.EquipAsync(shirt).Result;

            Assert.That(result, Is.True);
            Assert.That(_wearer.TryGetEquipped(_torso, out var worn), Is.True);
            Assert.That(worn, Is.SameAs(shirt));
            Assert.That(raised, Is.EqualTo(new[] { shirt }));
            Assert.That(_wearer.GetComponentInChildren<OutfitInstance>().Item, Is.SameAs(shirt));
        }

        [Test]
        public void Equip_ReplacesItemInSlotAndReleasesPreviousPrefab()
        {
            var shirt = CreateSkinnedItem("Shirt", _torso);
            var jacket = CreateSkinnedItem("Jacket", _torso);
            var unequipped = new List<OutfitItem>();
            _wearer.Unequipped += unequipped.Add;

            _wearer.EquipAsync(shirt).Wait();
            _wearer.EquipAsync(jacket).Wait();

            Assert.That(_wearer.EquippedItems, Is.EqualTo(new[] { jacket }));
            Assert.That(unequipped, Is.EqualTo(new[] { shirt }));
            Assert.That(_provider.Released.Count, Is.EqualTo(1));
            Assert.That(_wearer.GetComponentsInChildren<OutfitInstance>().Length, Is.EqualTo(1));
        }

        [Test]
        public void Equip_SameItemTwice_LoadsOnce()
        {
            var shirt = CreateSkinnedItem("Shirt", _torso);

            _wearer.EquipAsync(shirt).Wait();
            var result = _wearer.EquipAsync(shirt).Result;

            Assert.That(result, Is.True);
            Assert.That(_provider.LoadCount, Is.EqualTo(1));
        }

        [Test]
        public void Equip_SupersededRequest_ReleasesPrefabAndReturnsFalse()
        {
            var slowShirt = CreateSkinnedItem("SlowShirt", _torso, deferred: true);
            var jacket = CreateSkinnedItem("Jacket", _torso);

            var slowRequest = _wearer.EquipAsync(slowShirt);
            _wearer.EquipAsync(jacket).Wait();
            _provider.Complete(slowShirt.AssetKey);

            Assert.That(slowRequest.Result, Is.False);
            Assert.That(_wearer.EquippedItems, Is.EqualTo(new[] { jacket }));
            Assert.That(_provider.Released.Count, Is.EqualTo(1));
        }

        [Test]
        public void Unequip_TakesItemOffAndCancelsPendingRequest()
        {
            var shirt = CreateSkinnedItem("Shirt", _torso);
            var slowShirt = CreateSkinnedItem("SlowShirt", _torso, deferred: true);
            _wearer.EquipAsync(shirt).Wait();

            var slowRequest = _wearer.EquipAsync(slowShirt);
            var removed = _wearer.Unequip(_torso);
            _provider.Complete(slowShirt.AssetKey);

            Assert.That(removed, Is.True);
            Assert.That(slowRequest.Result, Is.False);
            Assert.That(_wearer.EquippedItems, Is.Empty);
            Assert.That(_provider.Released.Count, Is.EqualTo(2));
        }

        [Test]
        public void Equip_CancelledByCaller_ReleasesPrefabAndReturnsFalse()
        {
            var slowShirt = CreateSkinnedItem("SlowShirt", _torso, deferred: true);
            var cancellation = new CancellationTokenSource();

            var request = _wearer.EquipAsync(slowShirt, cancellation.Token);
            cancellation.Cancel();
            _provider.Complete(slowShirt.AssetKey);

            Assert.That(request.Result, Is.False);
            Assert.That(_wearer.EquippedItems, Is.Empty);
            Assert.That(_provider.Released.Count, Is.EqualTo(1));
        }

        [Test]
        public void EquipPreset_WearsAllItems()
        {
            var shirt = CreateSkinnedItem("Shirt", _torso);
            var hat = CreateHat("Hat");
            _rig.CreateSocket(_skeleton, "Head", _head);
            var preset = _rig.Track(OutfitPreset.Create("Default", new[] { shirt, hat }));

            var result = _wearer.EquipAsync(preset).Result;

            Assert.That(result, Is.True);
            Assert.That(_wearer.EquippedItems, Is.EquivalentTo(new[] { shirt, hat }));
        }

        [Test]
        public void Equip_SocketItemWithoutSocket_ReleasesPrefabAndReturnsFalse()
        {
            var hat = CreateHat("Hat");

            var result = _wearer.EquipAsync(hat).Result;

            Assert.That(result, Is.False);
            Assert.That(_provider.Released.Count, Is.EqualTo(1));
        }

        [Test]
        public void Equip_MissingPrefab_ReturnsFalse()
        {
            var ghost = _rig.Track(OutfitItem.Create("Ghost", _torso, OutfitAttachMode.Skinned, "missing"));
            LogAssert.Expect(LogType.Error, new Regex("wasn't loaded"));

            var result = _wearer.EquipAsync(ghost).Result;

            Assert.That(result, Is.False);
        }

        [Test]
        public void Equip_WithoutProvider_Throws()
        {
            var wearer = _rig.CreateCharacter(out _);
            var shirt = CreateSkinnedItem("Shirt", _torso);

            Assert.That(() => wearer.EquipAsync(shirt).GetAwaiter().GetResult(), Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void UnequipAll_TakesEverythingOff()
        {
            _rig.CreateSocket(_skeleton, "Head", _head);
            _wearer.EquipAsync(CreateSkinnedItem("Shirt", _torso)).Wait();
            _wearer.EquipAsync(CreateHat("Hat")).Wait();

            _wearer.UnequipAll();

            Assert.That(_wearer.EquippedItems, Is.Empty);
            Assert.That(_wearer.GetComponentsInChildren<OutfitInstance>(), Is.Empty);
            Assert.That(_provider.Released.Count, Is.EqualTo(2));
        }

        [Test]
        public void SetStrategy_ReplacesStrategyForMode()
        {
            var strategy = new RecordingAttachStrategy();
            _wearer.SetStrategy(strategy);

            var result = _wearer.EquipAsync(CreateSkinnedItem("Shirt", _torso)).Result;

            Assert.That(result, Is.True);
            Assert.That(strategy.AttachCount, Is.EqualTo(1));
        }

        [Test]
        public void Equip_HidesCoveredBodyParts()
        {
            var torso = _rig.Track(OutfitBodyPart.Create("Torso"));
            var arms = _rig.Track(OutfitBodyPart.Create("Arms"));
            var torsoRenderer = _rig.CreateBodyPart(_skeleton, torso);
            var armsRenderer = _rig.CreateBodyPart(_skeleton, arms);
            var tShirt = CreateSkinnedItem("TShirt", _torso, hiddenBodyParts: new[] { torso });

            _wearer.EquipAsync(tShirt).Wait();

            Assert.That(torsoRenderer.enabled, Is.False);
            Assert.That(armsRenderer.enabled, Is.True);
        }

        [Test]
        public void Unequip_ShowsBodyPartsAgain()
        {
            var torso = _rig.Track(OutfitBodyPart.Create("Torso"));
            var torsoRenderer = _rig.CreateBodyPart(_skeleton, torso);
            _wearer.EquipAsync(CreateSkinnedItem("TShirt", _torso, hiddenBodyParts: new[] { torso })).Wait();

            _wearer.Unequip(_torso);

            Assert.That(torsoRenderer.enabled, Is.True);
        }

        [Test]
        public void BodyPart_StaysHiddenWhileAnotherItemHidesIt()
        {
            var torso = _rig.Track(OutfitBodyPart.Create("Torso"));
            var torsoRenderer = _rig.CreateBodyPart(_skeleton, torso);
            var legs = _rig.Track(OutfitSlot.Create("Legs"));
            _wearer.EquipAsync(CreateSkinnedItem("TShirt", _torso, hiddenBodyParts: new[] { torso })).Wait();
            _wearer.EquipAsync(CreateSkinnedItem("Overalls", legs, hiddenBodyParts: new[] { torso })).Wait();

            _wearer.Unequip(_torso);

            Assert.That(torsoRenderer.enabled, Is.False);
        }

        [Test]
        public void ReplacingItem_UpdatesHiddenBodyParts()
        {
            var torso = _rig.Track(OutfitBodyPart.Create("Torso"));
            var arms = _rig.Track(OutfitBodyPart.Create("Arms"));
            var armsRenderer = _rig.CreateBodyPart(_skeleton, arms);
            _rig.CreateBodyPart(_skeleton, torso);
            _wearer.EquipAsync(CreateSkinnedItem("Jacket", _torso, hiddenBodyParts: new[] { torso, arms })).Wait();

            _wearer.EquipAsync(CreateSkinnedItem("TShirt", _torso, hiddenBodyParts: new[] { torso })).Wait();

            Assert.That(armsRenderer.enabled, Is.True);
        }

        private OutfitItem CreateSkinnedItem(string itemName, OutfitSlot slot, bool deferred = false, OutfitBodyPart[] hiddenBodyParts = null)
        {
            var key = itemName.ToLowerInvariant();
            _provider.Register(key, _rig.CreateSkinnedItem(itemName, extraBone: "Head"), deferred);
            return _rig.Track(OutfitItem.Create(itemName, slot, OutfitAttachMode.Skinned, key, hiddenBodyParts));
        }

        private OutfitItem CreateHat(string itemName)
        {
            var key = itemName.ToLowerInvariant();
            _provider.Register(key, _rig.CreateRigidItem(itemName, Vector3.zero));
            return _rig.Track(OutfitItem.Create(itemName, _head, OutfitAttachMode.Socket, key));
        }
    }
}
