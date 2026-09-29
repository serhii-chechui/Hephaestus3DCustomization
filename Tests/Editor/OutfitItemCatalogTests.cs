using NUnit.Framework;

namespace WTFGames.Hephaestus.Customization3D.Tests
{
    public class OutfitItemCatalogTests
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
        public void TryGetItem_FindsItemsByIdAndByAssetNameWithoutId()
        {
            var slot = _rig.Track(OutfitSlot.Create("Torso"));
            var withId = _rig.Track(OutfitItem.Create("Shirt", slot, OutfitAttachMode.Skinned, "shirt").WithId("item.shirt.red"));
            var withoutId = _rig.Track(OutfitItem.Create("Jacket", slot, OutfitAttachMode.Skinned, "jacket"));
            var catalog = _rig.Track(OutfitItemCatalog.Create(new[] { withId, withoutId }));

            Assert.That(catalog.TryGetItem("item.shirt.red", out var first), Is.True);
            Assert.That(first, Is.SameAs(withId));
            Assert.That(catalog.TryGetItem("Jacket", out var second), Is.True);
            Assert.That(second, Is.SameAs(withoutId));
            Assert.That(catalog.TryGetItem("Shirt", out _), Is.False);
        }
    }
}
