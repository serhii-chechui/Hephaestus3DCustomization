using System;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D
{
    /// <summary>Maps a bone name used by items to the character's bone, for items made on another rig.</summary>
    [Serializable]
    public class OutfitBoneAlias
    {
        [Tooltip("Bone name in the item, e.g. \"Head\".")]
        public string alias;

        [Tooltip("The character's bone it stands for, e.g. \"head\".")]
        public string bone;
    }
}
