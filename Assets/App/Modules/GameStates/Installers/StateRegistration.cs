using System;
using VContainer;

namespace Biome.GameStates.Installers
{
    /// <summary>What one base state adds to its own scope: the state, its optional installer and its extra registrations.</summary>
    internal sealed class StateRegistration
    {
        private readonly Action<IContainerBuilder> _install;

        public StateRegistration(Type state, Type installer, Action<IContainerBuilder> install)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Installer = installer;
            _install = install;
        }

        public Type State { get; }

        public Type Installer { get; }

        public void Register(IContainerBuilder builder)
        {
            builder.Register(State, Lifetime.Scoped);
            if (Installer != null)
            {
                builder.Register(Installer, Lifetime.Scoped);
            }

            _install?.Invoke(builder);
        }
    }
}
