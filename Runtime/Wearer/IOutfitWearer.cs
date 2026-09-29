using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>A character that can wear outfit items, one per <see cref="OutfitSlot"/>.</summary>
    public interface IOutfitWearer
    {
        /// <summary>Raised after an item is put on.</summary>
        event Action<OutfitItem> Equipped;

        /// <summary>Raised after an item is taken off, including when another item replaces it.</summary>
        event Action<OutfitItem> Unequipped;

        /// <summary>Items currently worn.</summary>
        IReadOnlyCollection<OutfitItem> EquippedItems { get; }

        bool TryGetEquipped(OutfitSlot slot, out OutfitItem item);

        /// <summary>
        /// Loads and puts on <paramref name="item"/>, replacing the item in its slot. A later
        /// request for the same slot supersedes this one. Returns true when the item ends up
        /// worn, false when the request was superseded or cancelled, or the prefab wasn't found.
        /// A load that fails while the request is still current faults the task; a failure of a
        /// superseded request is ignored. Requesting an item that is already loading into its
        /// slot returns that pending request.
        /// </summary>
        Task<bool> EquipAsync(OutfitItem item, CancellationToken cancellationToken = default);

        /// <summary>Equips every item of <paramref name="preset"/>. Returns true when all of them end up worn.</summary>
        Task<bool> EquipAsync(OutfitPreset preset, CancellationToken cancellationToken = default);

        /// <summary>Takes off the item in <paramref name="slot"/> and cancels a pending request for it.</summary>
        /// <returns>True when an item was taken off.</returns>
        bool Unequip(OutfitSlot slot);

        /// <summary>Takes off every item and cancels every pending request.</summary>
        void UnequipAll();
    }
}
