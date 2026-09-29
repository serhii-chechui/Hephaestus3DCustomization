using System;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>What a wearer's pending requests are doing, e.g. for spinners and error messages.</summary>
    public interface IOutfitLoadingStatus
    {
        /// <summary>
        /// Raised when a current request can't put its item on: the load threw, the prefab wasn't
        /// found or couldn't be attached. The exception is null when nothing was thrown.
        /// </summary>
        event Action<OutfitItem, Exception> EquipFailed;

        /// <summary>True while an item is loading into <paramref name="slot"/>.</summary>
        bool IsLoading(OutfitSlot slot);
    }
}
