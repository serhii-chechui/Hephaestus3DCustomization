using System.Threading;
using System.Threading.Tasks;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>A wearer whose outfit can be saved and restored as item ids.</summary>
    public interface IOutfitLoadoutWearer : IOutfitWearer
    {
        /// <summary>The ids of the items currently worn, to save or send.</summary>
        OutfitLoadout GetLoadout();

        /// <summary>
        /// Makes the character wear exactly the loadout's items: slots the loadout doesn't use
        /// are emptied, and ids the catalog doesn't know are skipped with a warning. Returns true
        /// when every item ends up worn.
        /// </summary>
        Task<bool> ApplyLoadoutAsync(OutfitLoadout loadout, IOutfitItemCatalog catalog, CancellationToken cancellationToken = default);
    }
}
