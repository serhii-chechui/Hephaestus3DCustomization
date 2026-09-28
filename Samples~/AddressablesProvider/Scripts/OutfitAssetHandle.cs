using System.Threading.Tasks;
using UnityEngine;

namespace WTFGames.Hephaestus.Customization3D.Samples.Addressables
{
    /// <summary>A shared load of one address and the number of wearers using it.</summary>
    internal sealed class OutfitAssetHandle
    {
        public OutfitAssetHandle(Task<GameObject> load)
        {
            Load = load;
        }

        public Task<GameObject> Load { get; }

        public int Users { get; set; }
    }
}
