using Biome.Configs.Abstract;
using UnityEngine;

namespace Biome.App.Tests
{
    /// <summary>
    /// The root route of the test environment seam: hands a test the environment the selected profile's scope carries,
    /// so no App test hard-codes one. A test that needs another environment uses any other route of
    /// <c>ConfigsTestEnvironment</c> and changes only the argument.
    /// </summary>
    internal static class AppTestEnvironment
    {
        /// <summary>The environment of the scope the composition root asset's profile selects.</summary>
        internal static IConfigsEnvironment NamedByRoot()
        {
            var root = Resources.Load<AppCompositionRoot>(AppCompositionRoot.ResourcePath);
            return root.SelectProfile().Scope.Environment;
        }
    }
}
