using System;
using UnityEngine;

namespace Biome.App
{
    /// <summary>
    /// A composition profile as data: the file the composition root loads by its name. It holds only the reference to the
    /// scope prefab that runs the profile's installer list; the environment and the state flow sit on that scope.
    /// It decides nothing at run time. Main thread only.
    /// </summary>
    [CreateAssetMenu(menuName = "Biome/App Profile")]
    public sealed class AppProfile : ScriptableObject
    {
        [SerializeField] private AppProfileScope _scope;

        /// <summary>The scope prefab that runs this profile's installer list.</summary>
        public AppProfileScope Scope => _scope;

        /// <summary>Checks the profile is complete. Throws <see cref="InvalidOperationException"/> when the scope is missing.</summary>
        public void Validate()
        {
            if (_scope == null)
            {
                throw new InvalidOperationException($"AppProfile '{name}' has no scope prefab.");
            }
        }
    }
}
