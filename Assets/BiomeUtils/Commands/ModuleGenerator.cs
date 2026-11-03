using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Biome.Utils.Commands
{
    /// <summary>Payload of <c>create_module</c>: paths are Unity-project-root relative, forward slashes.</summary>
    public sealed class ModuleGenerationResult
    {
        public string modulePath;
        public List<string> created = new List<string>();
        public List<string> skipped = new List<string>();
        public List<string> warnings = new List<string>();
    }

    /// <summary>
    /// Lays the canonical module skeleton (Abstract/ Config/ Data/ Installers/ Runtime/ Fakes/ InstallersMock/ Tests/,
    /// optionally Events/, + README with the A13 sections and the marker frame) from the base templates. Pure System.IO —
    /// no AssetDatabase — so it is testable against any folder. Idempotent: an existing file is never clobbered; every
    /// artefact is reported as <c>created</c> or <c>skipped</c>. Components/ · Editor/ · Templates/ are never laid; a
    /// pre-existing <c>&lt;Module&gt;/Templates/</c> folder is not a layer and is left untouched.
    /// </summary>
    public static class ModuleGenerator
    {
        /// <summary>The <c>/module</c> name rule: folder == name.</summary>
        public static readonly Regex NameRule = new Regex(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.Compiled);

        /// <summary>The default layout: template file → target path relative to the module folder.</summary>
        public static readonly (string template, string target)[] Layout =
        {
            ("Abstract.asmdef.txt", "Abstract/{AsmdefRoot}.{Module}.Abstract.asmdef"),
            ("IService.cs.txt", "Abstract/I{ModuleId}Service.cs"),
            ("Config.asmdef.txt", "Config/{AsmdefRoot}.{Module}.Config.asmdef"),
            ("Data.asmdef.txt", "Data/{AsmdefRoot}.{Module}.Data.asmdef"),
            ("Installers.asmdef.txt", "Installers/{AsmdefRoot}.{Module}.Installers.asmdef"),
            ("Installer.cs.txt", "Installers/{ModuleId}Installer.cs"),
            ("Runtime.asmdef.txt", "Runtime/{AsmdefRoot}.{Module}.asmdef"),
            ("Service.cs.txt", "Runtime/{ModuleId}Service.cs"),
            ("Factory.cs.txt", "Runtime/{ModuleId}Factory.cs"),
            ("Fakes.asmdef.txt", "Fakes/{AsmdefRoot}.{Module}.Fakes.asmdef"),
            ("FakeService.cs.txt", "Fakes/Fake{ModuleId}Service.cs"),
            ("InstallersMock.asmdef.txt", "InstallersMock/{AsmdefRoot}.{Module}.Installers.Mock.asmdef"),
            ("MockInstaller.cs.txt", "InstallersMock/Mock{ModuleId}Installer.cs"),
            ("Tests.asmdef.txt", "Tests/{AsmdefRoot}.{Module}.Tests.asmdef"),
            ("ContractTests.cs.txt", "Tests/{ModuleId}ContractTests.cs"),
            ("FakeContractTests.cs.txt", "Tests/Fake{ModuleId}ContractTests.cs"),
            ("ProductionContractTests.cs.txt", "Tests/Production{ModuleId}ContractTests.cs"),
            ("README.md.txt", "README.md"),
        };

        /// <summary>The optional Events/ layer (payload types only, references Data), laid on request.</summary>
        public static readonly (string template, string target)[] EventsLayout =
        {
            ("Events.asmdef.txt", "Events/{AsmdefRoot}.{Module}.Events.asmdef"),
        };

        /// <summary>The host layers the host-only mode always lays from a package's templates.</summary>
        public static readonly string[] HostLayers = { "Runtime", "Installers", "InstallersMock", "Tests" };

        /// <summary>The host layers the host-only mode lays on request.</summary>
        public static readonly string[] OptionalHostLayers = { "Events", "Editor" };

        private const string TemplateSuffix = ".txt";

        private static readonly Regex s_templateAsmdef = new Regex(@"^[^.]+\.(.+)\.asmdef\.txt$", RegexOptions.Compiled);

        public static bool IsValidName(string name) => !string.IsNullOrEmpty(name) && NameRule.IsMatch(name);

        /// <summary>Identifier-safe form of the module name (dots / hyphens → underscore) for C# names.</summary>
        public static string ToIdentifier(string module) => Regex.Replace(module, @"[^A-Za-z0-9_]", "_");

        /// <summary>
        /// Namespace-safe form of the module name: dots kept as segments, any other non-identifier character →
        /// underscore, a segment starting with a digit prefixed by an underscore. Equal to the name for every name made
        /// of identifier segments, so namespace == asmdef name (S6) holds there; it cannot hold for the rest.
        /// </summary>
        public static string ToNamespace(string module) =>
            string.Join(".", module.Split('.').Select(segment =>
            {
                var id = ToIdentifier(segment);
                return id.Length > 0 && char.IsDigit(id[0]) ? "_" + id : id;
            }));

        /// <summary>
        /// Lay the skeleton of <paramref name="module"/> under <paramref name="modulesHomeAbs"/> using the templates in
        /// <paramref name="templatesDirAbs"/>. <paramref name="projectRootAbs"/> is used to relativize the reported paths;
        /// <paramref name="withEvents"/> adds the optional Events/ layer.
        /// </summary>
        public static ModuleGenerationResult Create(string module, string modulesHomeAbs, string templatesDirAbs,
            string asmdefRoot, string projectRootAbs, bool withEvents = false)
        {
            if (!IsValidName(module))
                throw new ArgumentException($"module name '{module}' violates the name rule {NameRule}");
            if (!Directory.Exists(templatesDirAbs))
                throw new DirectoryNotFoundException("templates folder not found: " + templatesDirAbs);

            var moduleDir = Path.Combine(modulesHomeAbs, module);
            var result = new ModuleGenerationResult { modulePath = Relativize(moduleDir, projectRootAbs) };
            var vars = new Dictionary<string, string>
            {
                ["{Module}"] = module,
                ["{ModuleId}"] = ToIdentifier(module),
                ["{AsmdefRoot}"] = asmdefRoot,
                ["{Namespace}"] = asmdefRoot + "." + ToNamespace(module),
            };

            if (ToNamespace(module) != module)
            {
                result.warnings.Add($"S6: '{module}' is not made of identifier segments — namespace {vars["{Namespace}"]} " +
                    $"!= asmdef name {asmdefRoot}.{module}");
            }

            Directory.CreateDirectory(moduleDir);
            var layout = withEvents ? Layout.Concat(EventsLayout) : Layout;
            foreach (var (template, target) in layout)
            {
                var templatePath = Path.Combine(templatesDirAbs, template);
                if (!File.Exists(templatePath))
                    throw new FileNotFoundException("template missing", templatePath);
                var targetRel = Expand(target, vars);
                var targetAbs = Path.Combine(moduleDir, targetRel);
                var reported = Relativize(targetAbs, projectRootAbs);
                if (File.Exists(targetAbs))
                {
                    result.skipped.Add(reported);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(targetAbs));
                File.WriteAllText(targetAbs, Expand(File.ReadAllText(templatePath), vars));
                result.created.Add(reported);
            }
            return result;
        }

        /// <summary>The module a package's templates belong to: <c>&lt;Module&gt;</c> of <c>Runtime/&lt;Root&gt;.&lt;Module&gt;.asmdef.txt</c>.</summary>
        public static string ModuleNameFromTemplates(string templatesDirAbs)
        {
            var runtime = Path.Combine(templatesDirAbs, "Runtime");
            var asmdef = Directory.Exists(runtime) ? Directory.GetFiles(runtime, "*.asmdef.txt").FirstOrDefault() : null;
            var match = asmdef == null ? Match.Empty : s_templateAsmdef.Match(Path.GetFileName(asmdef));
            if (!match.Success)
                throw new FileNotFoundException("no Runtime/<Root>.<Module>.asmdef.txt template", runtime);
            return match.Groups[1].Value;
        }

        /// <summary>
        /// Lay only the host layers of <paramref name="module"/> from a package's fixed templates, copied as they are:
        /// <c>&lt;layer&gt;/&lt;path&gt;.txt</c> → <c>&lt;Module&gt;/&lt;layer&gt;/&lt;path&gt;</c>. Same payload and same
        /// never-clobber rule as <see cref="Create"/>; a layer the package has no templates for is a warning.
        /// </summary>
        public static ModuleGenerationResult CreateHostOnly(string module, string modulesHomeAbs, string templatesDirAbs,
            string projectRootAbs, IEnumerable<string> optionalLayers = null)
        {
            if (!IsValidName(module))
                throw new ArgumentException($"module name '{module}' violates the name rule {NameRule}");
            if (!Directory.Exists(templatesDirAbs))
                throw new DirectoryNotFoundException("templates folder not found: " + templatesDirAbs);

            var moduleDir = Path.Combine(modulesHomeAbs, module);
            var result = new ModuleGenerationResult { modulePath = Relativize(moduleDir, projectRootAbs) };
            foreach (var layer in HostLayers.Concat(optionalLayers ?? Enumerable.Empty<string>()))
            {
                var layerDir = Path.Combine(templatesDirAbs, layer);
                if (!Directory.Exists(layerDir))
                {
                    result.warnings.Add($"no-template: the package has no {layer}/ templates");
                    continue;
                }
                foreach (var template in Directory.GetFiles(layerDir, "*" + TemplateSuffix, SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                {
                    var relative = Path.GetRelativePath(templatesDirAbs, template);
                    var targetAbs = Path.Combine(moduleDir, relative.Substring(0, relative.Length - TemplateSuffix.Length));
                    var reported = Relativize(targetAbs, projectRootAbs);
                    if (File.Exists(targetAbs))
                    {
                        result.skipped.Add(reported);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(targetAbs));
                    File.Copy(template, targetAbs);
                    result.created.Add(reported);
                }
            }
            return result;
        }

        public static string Expand(string text, IReadOnlyDictionary<string, string> vars)
        {
            foreach (var kv in vars)
                text = text.Replace(kv.Key, kv.Value);
            return text;
        }

        private static string Relativize(string absolute, string rootAbs)
        {
            var root = Path.GetFullPath(rootAbs).Replace('\\', '/').TrimEnd('/') + "/";
            var full = Path.GetFullPath(absolute).Replace('\\', '/');
            return full.StartsWith(root, StringComparison.Ordinal) ? full.Substring(root.Length) : full;
        }
    }
}
