using System;
using System.Collections.Generic;
using Biome.Configs.Abstract;
using Biome.Configs.Data;
using VContainer;
using VContainer.Unity;

namespace Biome.Configs.Installers
{
    /// <summary>
    /// Prod registration: one <see cref="ConfigsService"/> per scope, exposed as <see cref="IConfigsService"/> and
    /// <see cref="IConfigsReader"/>, over a <see cref="ConfigStore"/> and a <see cref="ResourcesConfigSource"/>, with the
    /// open <see cref="IConfig{T}"/> provider. The loaders are the ones the owners registered in the same container; none
    /// is an empty list. The service starts with no base configs. The environment comes by reference through the
    /// constructor and is registered as <see cref="IConfigsEnvironment"/> for the boot to resolve and pass to the init.
    /// A thin passthrough of <see cref="IContainerBuilder"/>.
    /// </summary>
    public sealed class ConfigsInstaller : IInstaller
    {
        private readonly IConfigsEnvironment _environment;

        /// <summary>Creates the installer over <paramref name="environment"/>. Throws <see cref="ArgumentNullException"/> for null.</summary>
        public ConfigsInstaller(IConfigsEnvironment environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Registers the configs module into <paramref name="builder"/>. Throws <see cref="ArgumentNullException"/> for a
        /// null builder.
        /// </summary>
        public void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.RegisterInstance(_environment).As<IConfigsEnvironment>();
            builder.Register<ConfigStore>(Lifetime.Singleton);
            builder.Register<ResourcesConfigSource>(Lifetime.Singleton).As<IConfigSourceReader>();
            builder.Register(
                    resolver => new ConfigsService(
                        resolver.Resolve<ConfigStore>(),
                        resolver.Resolve<IConfigSourceReader>(),
                        resolver.Resolve<IReadOnlyList<IConfigLoader>>(),
                        Array.Empty<object>()),
                    Lifetime.Singleton)
                .As<IConfigsService, IConfigsReader>();
            builder.Register(typeof(ConfigProvider<>), Lifetime.Singleton).As(typeof(IConfig<>));
        }
    }
}
