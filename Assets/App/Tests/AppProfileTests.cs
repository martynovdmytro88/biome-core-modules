#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biome.App.Tests
{
    /// <summary>
    /// The profile as data: a complete profile validates, an incomplete one fails loudly, and the shipped game asset
    /// is complete and is found in Resources by its file name.
    /// </summary>
    public sealed class AppProfileTests
    {
        private const string GameAssetPath = "Assets/App/Composition/Profiles/Resources/Game.asset";

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private AppProfile Profile(bool scope)
        {
            var profile = ScriptableObject.CreateInstance<AppProfile>();
            _created.Add(profile);
            var serialized = new UnityEditor.SerializedObject(profile);
            serialized.FindProperty("_scope").objectReferenceValue = scope ? NewScope() : null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private AppProfileScope NewScope()
        {
            var go = new GameObject("scope (template)");
            go.SetActive(false);
            _created.Add(go);
            return go.AddComponent<GameScope>();
        }

        [Test]
        public void Validate_CompleteProfile_DoesNotThrow()
        {
            var profile = Profile(true);

            Assert.DoesNotThrow(profile.Validate);
        }

        [Test]
        public void Validate_NullScope_ThrowsNamingTheProfile()
        {
            var profile = Profile(false);
            profile.name = "Broken";

            var ex = Assert.Throws<InvalidOperationException>(profile.Validate);

            Assert.That(ex.Message, Does.Contain("Broken"));
        }

        [Test]
        public void GameAsset_Validates_AndItsScopeCarriesTheProductionEnvironment()
        {
            var game = UnityEditor.AssetDatabase.LoadAssetAtPath<AppProfile>(GameAssetPath);

            Assert.That(game, Is.Not.Null);
            Assert.DoesNotThrow(game.Validate);
            Assert.That(game.Scope.Environment, Is.Not.Null);
            Assert.That(game.Scope, Is.InstanceOf<GameScope>());
        }

        [Test]
        public void GameAsset_LoadedByFileName_IsTheShippedAsset()
        {
            var byName = Resources.Load<AppProfile>("Game");

            Assert.That(byName, Is.Not.Null);
            Assert.That(byName, Is.SameAs(UnityEditor.AssetDatabase.LoadAssetAtPath<AppProfile>(GameAssetPath)));
        }
    }
}
#endif
