using System;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using VContainer;
using VContainer.Unity;

namespace Biome.Configs.Installers.Mock
{
    /// <summary>
    /// Mock registration: the <see cref="FakeConfigsService"/> under the same faces as the production installer
    /// (<see cref="IConfigsService"/> · <see cref="IConfigsReader"/> · the open <see cref="IConfig{T}"/>), as one
    /// singleton owned by its scope. The service stays uninitialised, so the boot runs its two steps as in production;
    /// the environment it is given is registered for that boot to resolve and pass to the init.
    /// </summary>
    public sealed class ConfigsInstallerMock : IInstaller
    {
        private readonly IConfigsEnvironment _environment;
        private readonly object[] _baseConfigs;

        /// <summary>
        /// Creates the installer over <paramref name="environment"/> and the base configs the fake lands at its init.
        /// Throws <see cref="ArgumentNullException"/> for a null environment or a null base config list.
        /// </summary>
        public ConfigsInstallerMock(IConfigsEnvironment environment, params object[] baseConfigs)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _baseConfigs = baseConfigs ?? throw new ArgumentNullException(nameof(baseConfigs));
        }

        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_environment).As<IConfigsEnvironment>();
            builder.Register(_ => new FakeConfigsService(Array.Empty<IConfigLoader>(), _baseConfigs), Lifetime.Singleton)
                .As<IConfigsService, IConfigsReader>();
            builder.Register(typeof(FakeConfigsService.Provider<>), Lifetime.Singleton)
                .As(typeof(IConfig<>));
        }
    }
}
