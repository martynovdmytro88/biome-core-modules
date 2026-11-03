using System;
using System.Collections.Generic;
using Biome.Assets.Installers;
using Biome.Configs.Config;
using Biome.Configs.Installers;
using Biome.EventBus.Installers;
using Biome.GameStates.Config;
using Biome.LocalStore;
using Biome.LocalStore.Config;
using Biome.LocalStore.Installers;
using Biome.PlayerData;
using Biome.PlayerData.Data;
using Biome.PlayerData.Installers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Biome.App
{
    /// <summary>
    /// A profile's scope: the root scope's composition. The environment and the state flow sit side by side on the
    /// variant's prefab; <see cref="Configure"/> takes a copy of the flow, registers it, runs the common installers and
    /// then the state machine over that copy. A variant differs by the values on its prefab and by <see cref="DeclareStates"/>.
    /// The container's disposal is <see cref="LifetimeScope"/>'s own (OnDestroy).
    /// </summary>
    public abstract class AppProfileScope : LifetimeScope
    {
        private static AppProfile s_Pending;

        [SerializeField] private EnvironmentConfig _environment;
        [SerializeField] private GameStatesFlowConfigSO _flow;

        /// <summary>The environment config this scope boots with, held by reference.</summary>
        public EnvironmentConfig Environment => _environment;

        /// <summary>The profile this scope is raised for; set before the container builds.</summary>
        protected AppProfile Profile { get; private set; }

        /// <summary>
        /// Gives the next scope that awakes its profile. The root is instantiated and activated in one call that
        /// offers no hook before <c>Awake</c>, so the hand-over is held for the length of the returned scope.
        /// </summary>
        internal static IDisposable Handover(AppProfile profile)
        {
            s_Pending = profile;
            return new PendingScope();
        }

        /// <summary>The common installers, in installation order. A variant overrides this only to change the list.</summary>
        protected virtual IEnumerable<IInstaller> Installers()
        {
            yield return new EventBusInstaller();
            // The registry holds the types its consumers register (PlayerData first); the limits are still placeholders.
            yield return new LocalStoreInstaller(
                new DictionaryLocalStoreRegistry().Add<PlayerDataRecord>(PlayerDataStoreEntry.Key, PlayerDataStoreEntry.Version),
                LocalStoreLimits.Placeholder);
            yield return new PlayerDataInstaller();
            yield return new AssetsInstaller();
            yield return new ConfigsInstaller(_environment);
        }

        /// <summary>Adds the states of this variant that the app catalogue does not know. The base declares none.</summary>
        protected virtual void DeclareStates(AppStatesInstaller states)
        {
        }

        protected override void Awake()
        {
            if (Profile == null)
            {
                Profile = s_Pending;
            }

            base.Awake();
        }

        protected sealed override void Configure(IContainerBuilder builder)
        {
            if (Profile == null)
            {
                throw new InvalidOperationException($"{GetType().Name}: the scope was built without a profile; raise it through the composition root.");
            }

            if (_environment == null)
            {
                throw new InvalidOperationException($"{name}: the scope has no environment config.");
            }

            if (_flow == null)
            {
                throw new InvalidOperationException($"{name}: the scope has no state flow asset.");
            }

            var flow = _flow.ToConfig();
            flow.Validate();
            builder.RegisterInstance(flow);
            foreach (var installer in Installers())
            {
                installer.Install(builder);
            }

            var states = new AppStatesInstaller(flow);
            DeclareStates(states);
            states.Install(builder);
        }

        private sealed class PendingScope : IDisposable
        {
            public void Dispose() => s_Pending = null;
        }
    }
}
