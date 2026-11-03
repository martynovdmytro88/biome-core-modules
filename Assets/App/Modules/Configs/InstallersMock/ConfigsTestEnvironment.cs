using System;
using System.Reflection;
using Biome.Configs.Abstract;
using Biome.Configs.Config;
using UnityEditor;
using UnityEngine;

namespace Biome.Configs.Fakes
{
    /// <summary>
    /// The test seam that hands a test its <see cref="IConfigsEnvironment"/>: by an asset name in Resources, from a
    /// supplied instance, or from a <see cref="FakeConfigsEnvironment"/>. A test picks a route and nothing else, so
    /// swapping the route changes the argument only. The same seam builds a real <see cref="EnvironmentConfig"/> from
    /// code for the tests that need one. Main thread only.
    /// </summary>
    public static class ConfigsTestEnvironment
    {
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string FixtureFolderName = "ConfigsTestEnvironmentFixture";

        /// <summary>
        /// The <see cref="EnvironmentConfig"/> asset named <paramref name="assetName"/> under a Resources folder.
        /// Throws <see cref="ArgumentException"/> for a blank name and <see cref="InvalidOperationException"/> when
        /// no such asset exists, so an unknown name fails loudly.
        /// </summary>
        public static IConfigsEnvironment FromResources(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                throw new ArgumentException("The environment asset name is blank.", nameof(assetName));
            }

            var environment = Resources.Load<EnvironmentConfig>(assetName);
            if (environment == null)
            {
                throw new InvalidOperationException($"No EnvironmentConfig named '{assetName}' under a Resources folder.");
            }

            return environment;
        }

        /// <summary>The supplied <paramref name="environment"/>. Throws <see cref="ArgumentNullException"/> when it is null.</summary>
        public static IConfigsEnvironment FromInstance(IConfigsEnvironment environment)
        {
            return environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>The supplied <paramref name="environment"/>. Throws <see cref="ArgumentNullException"/> when it is null.</summary>
        public static IConfigsEnvironment FromFake(FakeConfigsEnvironment environment)
        {
            return environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// A new <see cref="EnvironmentConfig"/> with one Resources source per (type, path) pair, in order. The caller
        /// owns the instance and destroys it. Throws <see cref="ArgumentNullException"/> for a null id or source list.
        /// </summary>
        public static EnvironmentConfig CreateAsset(string id, params (Type type, string path)[] sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            var raw = new (string typeName, string path)[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                raw[i] = (sources[i].type.AssemblyQualifiedName, sources[i].path);
            }

            return CreateRawAsset(id, raw);
        }

        /// <summary>
        /// A new <see cref="EnvironmentConfig"/> whose entries carry the given type names verbatim, even
        /// unresolvable ones. The caller owns the instance and destroys it.
        /// </summary>
        public static EnvironmentConfig CreateRawAsset(string id, params (string typeName, string path)[] sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            var environment = ScriptableObject.CreateInstance<EnvironmentConfig>();
            var entryType = typeof(EnvironmentConfig).GetNestedType("SourceEntry", BindingFlags.NonPublic);
            var entries = Array.CreateInstance(entryType, sources.Length);
            for (var i = 0; i < sources.Length; i++)
            {
                var entry = Activator.CreateInstance(entryType, true);
                SetField(entry, "_configType", sources[i].typeName);
                SetField(entry, "_kind", ConfigSourceKind.Resources);
                SetField(entry, "_path", sources[i].path);
                entries.SetValue(entry, i);
            }

            SetField(environment, "_id", id);
            SetField(environment, "_sources", entries);
            return environment;
        }

        /// <summary>
        /// Puts a real, empty <see cref="EnvironmentConfig"/> named <paramref name="assetName"/> under a Resources
        /// folder so <see cref="FromResources"/> can find it. Disposing the result deletes it again.
        /// </summary>
        public static IDisposable CreateResourcesAsset(string assetName, string id)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                throw new ArgumentException("The environment asset name is blank.", nameof(assetName));
            }

            var scope = new ResourcesAssetScope();
            scope.Dispose();
            AssetDatabase.CreateFolder("Assets", FixtureFolderName);
            AssetDatabase.CreateFolder(scope.Folder, "Resources");
            AssetDatabase.CreateAsset(CreateAsset(id), scope.Folder + "/Resources/" + assetName + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return scope;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, NonPublicInstance);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, name);
            }

            field.SetValue(target, value);
        }

        private sealed class ResourcesAssetScope : IDisposable
        {
            public string Folder => "Assets/" + FixtureFolderName;

            public void Dispose()
            {
                if (AssetDatabase.IsValidFolder(Folder))
                {
                    AssetDatabase.DeleteAsset(Folder);
                }
            }
        }
    }
}
