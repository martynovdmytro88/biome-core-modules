using System;
using System.Collections.Generic;
using Biome.GameStates.Abstract;
using Biome.GameStates.Installers;

namespace Biome.App
{
    /// <summary>The installer of <see cref="InitState"/>: a shell with nothing to load yet.</summary>
    public sealed class InitStateInstaller : StateInstallerBase
    {
        /// <inheritdoc />
        protected override IReadOnlyList<IStateInstaller> Dependents => Array.Empty<IStateInstaller>();
    }
}
