using Zenject;

namespace WTFGames.Hephaestus.Customization3D.Samples.Addressables
{
    /// <summary>
    /// Binds <see cref="IOutfitAssetProvider"/> to Addressables. Install it next to
    /// <c>HephaestusAddressablesManagerInstaller</c>, which provides <c>IAddressablesManager</c>.
    /// </summary>
    public class HephaestusOutfitAddressablesInstaller : Installer<HephaestusOutfitAddressablesInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<IOutfitAssetProvider>().To<AddressablesOutfitAssetProvider>().AsSingle();
        }
    }
}
