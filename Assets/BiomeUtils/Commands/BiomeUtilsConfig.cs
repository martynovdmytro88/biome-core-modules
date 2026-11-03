using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Biome.Utils.Commands
{
    /// <summary>
    /// The one human-readable config of the utilities root (<c>Assets/BiomeUtils/biome-utils.json</c>), read
    /// once per domain reload and held statically. Its agreement with the vault's project identity is a
    /// <c>discover --mode check</c> item, never a rule to remember.
    /// </summary>
    public sealed class BiomeUtilsConfig
    {
        [JsonProperty("modulesFolder")] public string ModulesFolder { get; set; } = "";
        [JsonProperty("asmdefRoot")] public string AsmdefRoot { get; set; } = "";
        [JsonProperty("appNamespace")] public string AppNamespace { get; set; } = "";
        [JsonProperty("statesRoot")] public string StatesRoot { get; set; } = "";

        private static BiomeUtilsConfig s_current;

        /// <summary>The config as loaded at domain reload (lazy on first use; cached until the next reload).</summary>
        public static BiomeUtilsConfig Current => s_current ??= Load(BiomeUtilsPaths.ConfigFile);

        public static BiomeUtilsConfig Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("BiomeUtils config not found", path);
            var cfg = JsonConvert.DeserializeObject<BiomeUtilsConfig>(File.ReadAllText(path)) ?? new BiomeUtilsConfig();
            if (string.IsNullOrWhiteSpace(cfg.ModulesFolder) || string.IsNullOrWhiteSpace(cfg.AsmdefRoot))
                throw new InvalidDataException("BiomeUtils config needs both modulesFolder and asmdefRoot: " + path);
            cfg.ModulesFolder = cfg.ModulesFolder.Trim().Trim('/');
            cfg.AsmdefRoot = cfg.AsmdefRoot.Trim();
            cfg.AppNamespace = cfg.AppNamespace?.Trim();
            cfg.StatesRoot = cfg.StatesRoot?.Trim().Trim('/');
            return cfg;
        }

        /// <summary>Modules home, project-root relative: <c>Assets/&lt;modulesFolder&gt;</c>.</summary>
        public string ModulesHome => "Assets/" + ModulesFolder;

        /// <summary>Game states home, project-root relative: <c>Assets/&lt;statesRoot&gt;</c>.</summary>
        public string StatesHome => "Assets/" + StatesRoot;
    }

    /// <summary>Fixed paths of the utilities root (project-root relative, forward slashes).</summary>
    public static class BiomeUtilsPaths
    {
        public const string Root = "Assets/BiomeUtils";
        public const string ConfigFile = Root + "/biome-utils.json";
        public const string TemplatesDir = Root + "/Templates";
        public const string Readme = Root + "/README.md";
        public const string PackageTemplatesFolder = "Templates~";

        /// <summary>Absolute Unity project root (the folder holding <c>Assets/</c>).</summary>
        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string Absolute(string projectRelative) =>
            Path.GetFullPath(Path.Combine(ProjectRoot, projectRelative));

        /// <summary>Absolute <c>Templates~</c> path of an installed package, wherever it resolved; null when it is not installed.</summary>
        public static string PackageTemplates(string packageName)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
            return package == null ? null : Path.Combine(package.resolvedPath, PackageTemplatesFolder);
        }

        public static string ToProjectRelative(string absolute)
        {
            var root = ProjectRoot.Replace('\\', '/').TrimEnd('/') + "/";
            var full = Path.GetFullPath(absolute).Replace('\\', '/');
            return full.StartsWith(root, StringComparison.Ordinal) ? full.Substring(root.Length) : full;
        }
    }
}
