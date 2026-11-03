using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Biome.Utils.Commands;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Biome.Utils.GameStates
{
    /// <summary>The <c>create_game_state</c> callable: lays a state skeleton and its installer from the templates.</summary>
    public static class GameStateGenerator
    {
        public const string Name = "create_game_state";
        public const string Description =
            "Generate a game state skeleton (<StateId>State + <StateId>StateInstaller) into <statesRoot>/<StateId>/ (biome-utils.json). Refuses an existing folder, never overwrites. Registering the state in a flow or a pack stays manual.";
        public const string PackageName = "com.biome.gamestates";
        public const string TemplatesFolder = BiomeUtilsPaths.Root + "/GameStates/Templates";
        public const string StateTemplate = "State.cs.txt";
        public const string InstallerTemplate = "StateInstaller.cs.txt";

        private const string IdRule = "^[A-Z][A-Za-z0-9]*$";

        private static readonly Regex s_idRegex = new Regex(IdRule);

        /// <summary>The namespace of a generated state: <c>appNamespace</c> of <c>biome-utils.json</c>.</summary>
        public static string DefaultNamespace => BiomeUtilsConfig.Current.AppNamespace;

        /// <summary>The home of generated states, project-relative: <c>statesRoot</c> of <c>biome-utils.json</c> under <c>Assets/</c>.</summary>
        public static string DefaultStatesRoot => BiomeUtilsConfig.Current.StatesHome;

        /// <summary>
        /// Generates the state skeleton: <c>&lt;StateId&gt;State</c> (an <c>IGameState</c> shell) and
        /// <c>&lt;StateId&gt;StateInstaller</c> (a <c>StateInstallerBase</c> with no dependents) into
        /// <c>Assets/&lt;statesRoot&gt;/&lt;StateId&gt;/</c> in the <c>appNamespace</c> namespace (<c>biome-utils.json</c>).
        /// An existing folder is refused, nothing is overwritten. The state is not registered anywhere — a flow and a
        /// pack stay manual.
        /// </summary>
        /// <param name="state_id">State id, PascalCase ([A-Z][A-Za-z0-9]*); the types are named after it.</param>
        /// <param name="output_root">Target folder (project-relative); default Assets/&lt;statesRoot&gt;/&lt;StateId&gt;. For tests.</param>
        /// <returns>Envelope with payload <c>{ folder, created[] }</c>.</returns>
        [BiomeCallable(CallableKind.Generate)]
        [CliCommand(Name, Description, Tags = new[] { "biome/generate" })]
        public static BiomeEnvelope CreateGameState(
            [CliArg("state_id", "State id, PascalCase ([A-Z][A-Za-z0-9]*)", Required = true)] string state_id,
            [CliArg("output_root", "Target folder, project-relative (default Assets/<statesRoot>/<StateId>)")] string output_root = null)
        {
            try
            {
                if (!IsValidStateId(state_id))
                {
                    return BiomeEnvelope.Fail(Name, $"invalid state id '{state_id}' — expected {IdRule}");
                }

                var packageProblem = MissingPackageProblem(PackageName);
                if (packageProblem != null)
                {
                    return BiomeEnvelope.Fail(Name, packageProblem);
                }

                if (string.IsNullOrWhiteSpace(DefaultNamespace) || string.IsNullOrWhiteSpace(BiomeUtilsConfig.Current.StatesRoot))
                {
                    return BiomeEnvelope.Fail(Name, $"{BiomeUtilsPaths.ConfigFile} needs appNamespace and statesRoot for {Name}");
                }

                var folder = string.IsNullOrEmpty(output_root) ? DefaultStatesRoot + "/" + state_id : output_root;
                if (Directory.Exists(Absolute(folder)))
                {
                    return BiomeEnvelope.Fail(Name, $"folder '{folder}' already exists — refusing to overwrite");
                }

                var created = Generate(state_id, DefaultNamespace, folder, TemplatesAbsolute());
                AssetDatabase.Refresh();
                return BiomeEnvelope.Ok(Name, $"state '{state_id}' laid at {folder} ({created.Count} files)", new { folder, created });
            }
            catch (Exception ex)
            {
                return BiomeEnvelope.Exception(Name, ex);
            }
        }

        /// <summary>True when <paramref name="stateId"/> is a PascalCase identifier segment.</summary>
        public static bool IsValidStateId(string stateId)
        {
            return !string.IsNullOrEmpty(stateId) && s_idRegex.IsMatch(stateId);
        }

        /// <summary>Replaces the <c>{{StateId}}</c> and <c>{{Namespace}}</c> tokens of a template.</summary>
        public static string Render(string template, string stateId, string ns)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            return template
                .Replace("\r\n", "\n")
                .Replace("{{StateId}}", stateId)
                .Replace("{{Namespace}}", ns);
        }

        /// <summary>
        /// Writes the state and installer files into <paramref name="folder"/> (project-relative) and returns their paths.
        /// The caller refreshes the asset database.
        /// </summary>
        /// <exception cref="ArgumentException">The state id is not a valid PascalCase identifier.</exception>
        /// <exception cref="InvalidOperationException">The target folder already exists.</exception>
        public static IReadOnlyList<string> Generate(string stateId, string ns, string folder, string templatesDirAbsolute)
        {
            if (!IsValidStateId(stateId))
            {
                throw new ArgumentException($"invalid state id '{stateId}'", nameof(stateId));
            }

            var target = Absolute(folder);
            if (Directory.Exists(target))
            {
                throw new InvalidOperationException($"folder '{folder}' already exists");
            }

            var stateText = Render(File.ReadAllText(Path.Combine(templatesDirAbsolute, StateTemplate)), stateId, ns);
            var installerText = Render(File.ReadAllText(Path.Combine(templatesDirAbsolute, InstallerTemplate)), stateId, ns);

            Directory.CreateDirectory(target);
            var utf8 = new UTF8Encoding(false);
            var statePath = folder + "/" + stateId + "State.cs";
            var installerPath = folder + "/" + stateId + "StateInstaller.cs";
            File.WriteAllText(Absolute(statePath), stateText, utf8);
            File.WriteAllText(Absolute(installerPath), installerText, utf8);
            return new List<string> { statePath, installerPath };
        }

        /// <summary>Why the generated code cannot compile without <paramref name="packageName"/>, or null when it is installed.</summary>
        public static string MissingPackageProblem(string packageName)
        {
            return UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName) != null
                ? null
                : $"package '{packageName}' is not installed — a generated state does not compile without it; add it to Packages/manifest.json";
        }

        /// <summary>Absolute path of the templates folder.</summary>
        public static string TemplatesAbsolute()
        {
            return Absolute(TemplatesFolder);
        }

        /// <summary>Project-relative path to absolute.</summary>
        public static string Absolute(string projectRelative)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, projectRelative);
        }
    }
}
