namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>Resolves item ids, e.g. from a saved <see cref="OutfitLoadout"/>, to items.</summary>
    public interface IOutfitItemCatalog
    {
        bool TryGetItem(string id, out OutfitItem item);
    }
}
