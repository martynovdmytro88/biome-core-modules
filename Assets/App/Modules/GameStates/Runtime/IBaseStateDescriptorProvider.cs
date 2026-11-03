using System;
using Biome.Assets.Abstract;

namespace Biome.GameStates
{
    /// <summary>The seam that tells the machine what it cannot know about a base state: which one is Init and where each scene is.</summary>
    public interface IBaseStateDescriptorProvider
    {
        /// <summary>The Init state: entered once, loads no scene, its scope lives for the whole application.</summary>
        Type InitState { get; }

        /// <summary>Gives the scene key of a base state.</summary>
        /// <param name="state">The base state type.</param>
        /// <param name="sceneKey">The scene key when found.</param>
        /// <returns>False when the state has no scene.</returns>
        bool TryGetScene(Type state, out AssetKey sceneKey);
    }
}
