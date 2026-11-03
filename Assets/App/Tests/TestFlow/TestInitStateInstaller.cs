using System;
using System.Collections.Generic;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using Biome.GameStates.Abstract;
using Biome.GameStates.Installers;
using Biome.LocalStore;
using Biome.LocalStore.Abstract;
using Biome.LocalStore.Fakes;
using Biome.PlayerData.Abstract;
using Biome.PlayerData.Fakes;
using VContainer;

namespace Biome.App.Tests
{
    /// <summary>The installer of <see cref="TestInitState"/>: it loads nothing and registers the fakes of its scope.</summary>
    public sealed class TestInitStateInstaller : StateInstallerBase
    {
        /// <inheritdoc />
        protected override IReadOnlyList<IStateInstaller> Dependents => Array.Empty<IStateInstaller>();

        /// <summary>
        /// Registers the fakes of the persistence and configs services in the scope of <see cref="TestInitState"/>,
        /// so the test flow never creates the real ones and writes no PlayerPrefs. Only this scope sees them.
        /// </summary>
        public static void RegisterFakes(IContainerBuilder scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            scope.Register<ILocalStoreService>(_ => InMemoryLocalStore.Create(new DictionaryLocalStoreRegistry()), Lifetime.Scoped);
            scope.Register<IPlayerDataService>(_ => new InMemoryPlayerDataService(), Lifetime.Scoped);
            scope.Register<IConfigsService>(
                _ => new FakeConfigsService(Array.Empty<IConfigLoader>(), Array.Empty<object>()),
                Lifetime.Scoped);
        }
    }
}
