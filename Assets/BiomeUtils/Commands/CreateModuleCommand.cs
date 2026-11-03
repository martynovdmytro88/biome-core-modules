using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace Biome.Utils.Commands
{
    /// <summary>The <c>create_module</c> callable — the generator behind the module-creation process.</summary>
    public static class CreateModuleCommand
    {
        public const string Name = "create_module";
        public const string Description =
            "Lay the canonical module skeleton (Abstract/ Config/ Data/ Installers/ Runtime/ Fakes/ InstallersMock/ Tests/, optional Events/, + README) under the configured modules folder; with host_only + from_package, lay only the host layers from the package's Templates~. Idempotent: existing files are skipped, never clobbered.";

        /// <summary>
        /// Lays the canonical module skeleton under the configured modules home (<c>biome-utils.json</c>): the layer
        /// asmdefs Abstract · Config · Data · Installers · Runtime · Fakes · InstallersMock · Tests (+ Events on
        /// request), the empty <c>I&lt;Module&gt;Service</c> contract, its internal production implementation behind
        /// <c>&lt;Module&gt;Factory</c>, the shipped fake, the prod and mock installers, the contract suite with one
        /// fixture over production and one over the fake, and the README (A13 sections + marker frame). Folder == name,
        /// rule <c>[A-Za-z0-9][A-Za-z0-9._-]*</c>. Idempotent — every artefact is reported as <c>created</c> or
        /// <c>skipped</c>. With <paramref name="host_only"/> and <paramref name="from_package"/> it lays only the host
        /// layers Runtime · Installers · InstallersMock · Tests (+ Events · Editor on request) from the package's
        /// <c>Templates~/</c>, copied as they are, with asmdef references on the package assemblies.
        /// </summary>
        /// <param name="name">Module name; folder == name. Required unless host_only; there the default is the package's module.</param>
        /// <param name="with_events">Also lay the optional Events/ layer (payload types only, references Data).</param>
        /// <param name="host_only">Lay only the host layers from the templates of <c>from_package</c>.</param>
        /// <param name="from_package">host_only: the installed package whose <c>Templates~/</c> are laid, e.g. <c>com.biome.eventbus</c>.</param>
        /// <param name="with">host_only: optional layers, comma-separated, e.g. <c>Events,Editor</c>.</param>
        /// <param name="output_root">Modules home, project-relative; default <c>Assets/&lt;modulesFolder&gt;</c>. For tests.</param>
        /// <returns>Envelope with payload <c>{ modulePath, created[], skipped[] }</c>.</returns>
        [BiomeCallable(CallableKind.Generate)]
        [CliCommand(Name, Description, Tags = new[] { "biome/generate" })]
        public static BiomeEnvelope CreateModule(
            [CliArg("name", "Module name (folder == name; [A-Za-z0-9][A-Za-z0-9._-]*); required unless host_only")] string name = null,
            [CliArg("with_events", "Also lay the optional Events/ layer")] bool with_events = false,
            [CliArg("host_only", "Lay only the host layers from the templates of from_package")] bool host_only = false,
            [CliArg("from_package", "host_only: installed package whose Templates~ are laid")] string from_package = null,
            [CliArg("with", "host_only: optional layers, comma-separated (Events,Editor)")] string with = null,
            [CliArg("output_root", "Modules home, project-relative (default Assets/<modulesFolder>). For tests.")] string output_root = null)
        {
            try
            {
                if (!host_only && (!string.IsNullOrEmpty(from_package) || !string.IsNullOrEmpty(with)))
                    return BiomeEnvelope.Fail(Name, "from_package and with apply only with host_only");

                string templates = null;
                if (host_only)
                {
                    if (string.IsNullOrWhiteSpace(from_package))
                        return BiomeEnvelope.Fail(Name, "host_only needs from_package");
                    templates = BiomeUtilsPaths.PackageTemplates(from_package);
                    if (templates == null)
                        return BiomeEnvelope.Fail(Name, $"package '{from_package}' is not installed — add it to Packages/manifest.json");
                    if (!Directory.Exists(templates))
                        return BiomeEnvelope.Fail(Name, $"package '{from_package}' has no {BiomeUtilsPaths.PackageTemplatesFolder}/");
                    if (string.IsNullOrEmpty(name))
                        name = ModuleGenerator.ModuleNameFromTemplates(templates);
                }

                if (!ModuleGenerator.IsValidName(name))
                    return BiomeEnvelope.Fail(Name, $"invalid module name '{name}' — expected {ModuleGenerator.NameRule}");

                var cfg = BiomeUtilsConfig.Current;
                var home = BiomeUtilsPaths.Absolute(string.IsNullOrEmpty(output_root) ? cfg.ModulesHome : output_root);
                ModuleGenerationResult result;
                if (host_only)
                {
                    var layers = OptionalLayers(with, with_events, out var problem);
                    if (problem != null) return BiomeEnvelope.Fail(Name, problem);
                    result = ModuleGenerator.CreateHostOnly(name, home, templates, BiomeUtilsPaths.ProjectRoot, layers);
                }
                else
                {
                    result = ModuleGenerator.Create(
                        name,
                        home,
                        BiomeUtilsPaths.Absolute(BiomeUtilsPaths.TemplatesDir),
                        cfg.AsmdefRoot,
                        BiomeUtilsPaths.ProjectRoot,
                        with_events);
                }
                AssetDatabase.Refresh();

                var message = result.created.Count == 0
                    ? $"module '{name}' already complete — nothing created ({result.skipped.Count} skipped)"
                    : $"module '{name}' laid at {result.modulePath} ({result.created.Count} created, {result.skipped.Count} skipped)";
                return BiomeEnvelope.Ok(Name, message, result, result.warnings);
            }
            catch (Exception ex)
            {
                return BiomeEnvelope.Exception(Name, ex);
            }
        }

        private static List<string> OptionalLayers(string with, bool withEvents, out string problem)
        {
            problem = null;
            var layers = (with ?? "").Split(',').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            var unknown = layers.Except(ModuleGenerator.OptionalHostLayers).ToList();
            if (unknown.Count > 0)
                problem = $"unknown layer(s) in with: {string.Join(", ", unknown)} — expected {string.Join(", ", ModuleGenerator.OptionalHostLayers)}";
            if (withEvents) layers.Add("Events");
            return layers.Distinct().ToList();
        }

        /// <summary>Hands-only wrapper over the same static method: asks for the name, then calls <see cref="CreateModule"/>.</summary>
        [MenuItem("Biome/Utils/Create Module…", priority = 10)]
        private static void CreateModuleMenu()
        {
            NameInputWindow.Show("Create module", "Module name", name =>
            {
                var env = CreateModule(name);
                UnityEngine.Debug.Log($"[BiomeUtils] {env.Code}: {env.Message}");
                if (env.Code != ResultCode.Ok) EditorUtility.DisplayDialog("create_module", env.Message, "OK");
            });
        }
    }

    /// <summary>Minimal one-field prompt for the MenuItem wrappers (convenience only; never the contract).</summary>
    internal sealed class NameInputWindow : EditorWindow
    {
        private string m_Label;
        private string m_Value = "";
        private Action<string> m_OnOk;

        public static void Show(string title, string label, Action<string> onOk)
        {
            var w = CreateInstance<NameInputWindow>();
            w.titleContent = new UnityEngine.GUIContent(title);
            w.m_Label = label;
            w.m_OnOk = onOk;
            w.minSize = w.maxSize = new UnityEngine.Vector2(360, 70);
            w.ShowUtility();
        }

        private void OnGUI()
        {
            m_Value = EditorGUILayout.TextField(m_Label, m_Value);
            EditorGUILayout.BeginHorizontal();
            if (UnityEngine.GUILayout.Button("OK") && !string.IsNullOrWhiteSpace(m_Value))
            {
                var v = m_Value.Trim();
                Close();
                m_OnOk?.Invoke(v);
            }
            if (UnityEngine.GUILayout.Button("Cancel")) Close();
            EditorGUILayout.EndHorizontal();
        }
    }
}
