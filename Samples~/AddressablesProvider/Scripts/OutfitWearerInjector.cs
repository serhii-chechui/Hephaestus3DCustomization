using System;
using UnityEngine;
using Zenject;

namespace WTFGames.Hephaestus.Customization3D.Samples.Addressables
{
    /// <summary>
    /// Hands the injected <see cref="IOutfitAssetProvider"/> to the <see cref="OutfitWearer"/>
    /// of a character prefab and puts on its default outfit.
    /// </summary>
    [RequireComponent(typeof(OutfitWearer))]
    public class OutfitWearerInjector : MonoBehaviour
    {
        [SerializeField]
        private OutfitPreset _defaultOutfit;

        private OutfitWearer _wearer;

        [Inject]
        public void Construct(IOutfitAssetProvider provider)
        {
            _wearer = GetComponent<OutfitWearer>();
            _wearer.Construct(provider);
        }

        private async void Start()
        {
            if (_wearer == null || _defaultOutfit == null) return;

            try
            {
                await _wearer.EquipAsync(_defaultOutfit);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
