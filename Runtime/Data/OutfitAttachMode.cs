namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>How an outfit item is put on a character.</summary>
    public enum OutfitAttachMode
    {
        /// <summary>
        /// The item is a skinned mesh made for the character's skeleton. Its renderers are
        /// rebound to the character's bones by name, so the item deforms with the body.
        /// </summary>
        Skinned = 0,

        /// <summary>
        /// The item is a rigid object (hat, watch, glasses) parented to the character's
        /// <see cref="OutfitSocket"/> for the item's slot and socket id.
        /// </summary>
        Socket = 1,

        /// <summary>
        /// The item changes materials of the character's body parts (skin tone, tattoos,
        /// make-up). Its prefab holds an <see cref="OutfitMaterialSet"/>.
        /// </summary>
        Material = 2,

        /// <summary>The item is attached by its own <see cref="OutfitAttachStrategyAsset"/>.</summary>
        Custom = 3
    }
}
