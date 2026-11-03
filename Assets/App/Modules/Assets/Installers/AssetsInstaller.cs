using Biome.Assets.Abstract;
using VContainer;
using VContainer.Unity;

namespace Biome.Assets.Installers
{
    /// <summary>
    /// One <see cref="IAssetsService"/> per scope over Addressables (<see cref="AssetsFactory.CreateAddressables"/>).
    /// The container disposes it with the scope, which unloads every context. The module's only VContainer reference.
    /// </summary>
    public sealed class AssetsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAssetsService>(_ => AssetsFactory.CreateAddressables(), Lifetime.Singleton);
        }
    }
}
