using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine.TestTools;

namespace Biome.Utils.Commands.Tests
{
    public class ModuleGeneratorTests
    {
        private string m_Tmp;
        private string TemplatesAbs => BiomeUtilsPaths.Absolute(BiomeUtilsPaths.TemplatesDir);

        [SetUp] public void SetUp() => m_Tmp = Path.Combine(Path.GetTempPath(), "biome-utils-tests-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() { if (Directory.Exists(m_Tmp)) Directory.Delete(m_Tmp, true); }

        [Test]
        public void NameRule_AcceptsAndRejects()
        {
            Assert.That(ModuleGenerator.IsValidName("Inventory"), Is.True);
            Assert.That(ModuleGenerator.IsValidName("net.core-v2_x"), Is.True);
            Assert.That(ModuleGenerator.IsValidName(".hidden"), Is.False);
            Assert.That(ModuleGenerator.IsValidName("has space"), Is.False);
            Assert.That(ModuleGenerator.IsValidName(""), Is.False);
        }

        [Test]
        public void Create_LaysFixedLayout_AndIsIdempotent()
        {
            var first = ModuleGenerator.Create("Probe", m_Tmp, TemplatesAbs, "Biome", m_Tmp);
            Assert.That(first.created.Count, Is.EqualTo(ModuleGenerator.Layout.Length));
            Assert.That(first.skipped, Is.Empty);
            Assert.That(first.modulePath, Is.EqualTo("Probe"));
            var expected = new[] { "Abstract", "Config", "Data", "Fakes", "Installers", "InstallersMock", "Runtime", "Tests" };
            Assert.That(Directory.GetDirectories(Path.Combine(m_Tmp, "Probe")).Select(Path.GetFileName), Is.EquivalentTo(expected));
            Assert.That(File.Exists(Path.Combine(m_Tmp, "Probe", "Abstract", "IProbeService.cs")));
            Assert.That(File.Exists(Path.Combine(m_Tmp, "Probe", "Fakes", "FakeProbeService.cs")));
            Assert.That(File.Exists(Path.Combine(m_Tmp, "Probe", "Runtime", "Biome.Probe.asmdef")));
            Assert.That(File.Exists(Path.Combine(m_Tmp, "Probe", "InstallersMock", "Biome.Probe.Installers.Mock.asmdef")));
            Assert.That(first.warnings, Is.Empty);
            Assert.That(File.ReadAllText(Path.Combine(m_Tmp, "Probe", "README.md")), Does.Contain(DiscoverCommand.MarkerStart));

            var second = ModuleGenerator.Create("Probe", m_Tmp, TemplatesAbs, "Biome", m_Tmp);
            Assert.That(second.created, Is.Empty);
            Assert.That(second.skipped.Count, Is.EqualTo(ModuleGenerator.Layout.Length));
        }

        [Test]
        public void Create_WithEvents_AddsEventsLayer()
        {
            var r = ModuleGenerator.Create("Probe", m_Tmp, TemplatesAbs, "Biome", m_Tmp, withEvents: true);
            Assert.That(r.created.Count, Is.EqualTo(ModuleGenerator.Layout.Length + ModuleGenerator.EventsLayout.Length));
            var asmdef = JObject.Parse(File.ReadAllText(Path.Combine(m_Tmp, "Probe", "Events", "Biome.Probe.Events.asmdef")));
            Assert.That(asmdef["references"].Select(t => t.ToString()), Is.EqualTo(new[] { "Biome.Probe.Data" }));
        }

        [Test]
        public void Create_Asmdefs_RootNamespaceEqualsName_DoublesEditorOnly()
        {
            ModuleGenerator.Create("Probe", m_Tmp, TemplatesAbs, "Biome", m_Tmp, withEvents: true);
            foreach (var file in Directory.GetFiles(Path.Combine(m_Tmp, "Probe"), "*.asmdef", SearchOption.AllDirectories))
            {
                var asmdef = JObject.Parse(File.ReadAllText(file));
                var name = asmdef["name"].ToString();
                Assert.That(asmdef["rootNamespace"].ToString(), Is.EqualTo(name), file);
                Assert.That(Path.GetFileNameWithoutExtension(file), Is.EqualTo(name), file);
                if (name.EndsWith(".Fakes") || name.EndsWith(".Mock") || name.EndsWith(".Tests"))
                {
                    Assert.That(asmdef["includePlatforms"].Select(t => t.ToString()), Is.EqualTo(new[] { "Editor" }), file);
                    Assert.That(asmdef["autoReferenced"].Value<bool>(), Is.False, file);
                }
                if (name.EndsWith(".Abstract") || name.EndsWith(".Data"))
                    Assert.That(asmdef["noEngineReferences"].Value<bool>(), Is.True, file);
                if (name.EndsWith(".Fakes") || name.EndsWith(".Events"))
                    Assert.That(asmdef["noEngineReferences"].Value<bool>(), Is.False, file);
            }
        }

        [Test]
        public void Create_NeverClobbers_ExistingFile()
        {
            var iface = Path.Combine(m_Tmp, "Probe", "Abstract", "IProbeService.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(iface));
            File.WriteAllText(iface, "// hand-written");
            var r = ModuleGenerator.Create("Probe", m_Tmp, TemplatesAbs, "Biome", m_Tmp);
            Assert.That(File.ReadAllText(iface), Is.EqualTo("// hand-written"));
            Assert.That(r.skipped.Single(), Does.EndWith("IProbeService.cs"));
        }

        [Test]
        public void Create_ToleratesModuleTemplatesFolder()
        {
            var tpl = Path.Combine(m_Tmp, "Probe", "Templates");
            Directory.CreateDirectory(tpl);
            File.WriteAllText(Path.Combine(tpl, "Component.cs.txt"), "x");
            var r = ModuleGenerator.Create("Probe", m_Tmp, TemplatesAbs, "Biome", m_Tmp);
            Assert.That(r.created.Count, Is.EqualTo(ModuleGenerator.Layout.Length));
            Assert.That(File.ReadAllText(Path.Combine(tpl, "Component.cs.txt")), Is.EqualTo("x"));
        }

        [Test]
        public void Create_ExpandsPlaceholders_IdentifierSafe()
        {
            ModuleGenerator.Create("net.core-v2", m_Tmp, TemplatesAbs, "Biome", m_Tmp);
            var iface = File.ReadAllText(Path.Combine(m_Tmp, "net.core-v2", "Abstract", "Inet_core_v2Service.cs"));
            Assert.That(iface, Does.Contain("namespace Biome.net.core_v2.Abstract"));
            Assert.That(iface, Does.Not.Contain("{Module}").And.Not.Contain("{Namespace}"));
            var asmdef = File.ReadAllText(Path.Combine(m_Tmp, "net.core-v2", "Abstract", "Biome.net.core-v2.Abstract.asmdef"));
            Assert.That(JObject.Parse(asmdef)["name"].ToString(), Is.EqualTo("Biome.net.core-v2.Abstract"));
            Assert.That(ModuleGenerator.ToNamespace("net.core"), Is.EqualTo("net.core"));
            Assert.That(ModuleGenerator.ToNamespace("Foo.2d"), Is.EqualTo("Foo._2d"));
        }

        [Test]
        public void Create_RejectsBadName()
        {
            Assert.Throws<ArgumentException>(() => ModuleGenerator.Create("bad name", m_Tmp, TemplatesAbs, "Biome", m_Tmp));
        }
    }

    public class HostOnlyTests
    {
        private const string EventBusPackage = "com.biome.eventbus";
        private const string GameStatesPackage = "com.biome.gamestates";
        private const string TempRoot = "Temp/BiomeUtilsHostOnlyTests";
        private const string ModulesHome = TempRoot + "/Modules";

        private static readonly string[] s_packages =
        {
            EventBusPackage, "com.biome.localstore", "com.biome.assets", "com.biome.configs", GameStatesPackage, "com.biome.playerdata",
        };

        private static string ModulesAbs => BiomeUtilsPaths.Absolute(ModulesHome);

        [SetUp] public void SetUp() => TearDown();
        [TearDown] public void TearDown() { if (Directory.Exists(BiomeUtilsPaths.Absolute(TempRoot))) Directory.Delete(BiomeUtilsPaths.Absolute(TempRoot), true); }

        private static ModuleGenerationResult LayProbe(string package, params string[] optional) =>
            ModuleGenerator.CreateHostOnly("Probe", ModulesAbs, BiomeUtilsPaths.PackageTemplates(package), BiomeUtilsPaths.ProjectRoot, optional);

        [Test]
        public void Probe_LaysOnlyTheHostLayers_AndIsIdempotent()
        {
            var first = LayProbe(EventBusPackage);

            var probe = Path.Combine(ModulesAbs, "Probe");
            Assert.That(Directory.GetDirectories(probe).Select(Path.GetFileName), Is.EquivalentTo(ModuleGenerator.HostLayers));
            foreach (var file in new[]
            {
                "Runtime/Biome.EventBus.asmdef", "Runtime/EventBus.cs",
                "Installers/Biome.EventBus.Installers.asmdef", "Installers/EventBusInstaller.cs",
                "InstallersMock/Biome.EventBus.Installers.Mock.asmdef", "InstallersMock/RecordingEventBusInstaller.cs",
                "Tests/Host/Biome.EventBus.Tests.Host.asmdef", "Tests/Host/ProductionEventBusContractTests.cs",
            })
            {
                Assert.That(File.Exists(Path.Combine(probe, file)), file);
            }
            Assert.That(first.modulePath, Is.EqualTo(ModulesHome + "/Probe"));
            Assert.That(first.skipped, Is.Empty);
            Assert.That(first.warnings, Is.Empty);

            var second = LayProbe(EventBusPackage);
            Assert.That(second.created, Is.Empty);
            Assert.That(second.skipped, Is.EquivalentTo(first.created));
        }

        [Test]
        public void Probe_NeverClobbers_ExistingFile()
        {
            var service = Path.Combine(ModulesAbs, "Probe", "Runtime", "EventBus.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(service));
            File.WriteAllText(service, "// hand-written");

            var result = LayProbe(EventBusPackage);

            Assert.That(File.ReadAllText(service), Is.EqualTo("// hand-written"));
            Assert.That(result.skipped.Single(), Does.EndWith("Runtime/EventBus.cs"));
        }

        [Test]
        public void OptionalLayers_LaidOnRequest_AbsentOneIsAWarning()
        {
            var withEvents = LayProbe(GameStatesPackage, "Events");
            Assert.That(File.Exists(Path.Combine(ModulesAbs, "Probe", "Events", "Biome.GameStates.Events.asmdef")));
            Assert.That(withEvents.warnings, Is.Empty);

            var noEditor = LayProbe(GameStatesPackage, "Editor");
            Assert.That(noEditor.warnings.Single(), Does.StartWith("no-template: the package has no Editor/"));
        }

        [TestCaseSource(nameof(s_packages))]
        public void Templates_HaveEveryHostLayer_AndConsistentAsmdefs(string package)
        {
            var templates = BiomeUtilsPaths.PackageTemplates(package);
            Assert.That(ModuleGenerator.ModuleNameFromTemplates(templates), Is.Not.Empty);
            foreach (var layer in ModuleGenerator.HostLayers)
                Assert.That(Directory.GetFiles(Path.Combine(templates, layer), "*.cs.txt", SearchOption.AllDirectories), Is.Not.Empty, layer);
            foreach (var file in Directory.GetFiles(templates, "*.asmdef.txt", SearchOption.AllDirectories))
            {
                var asmdef = JObject.Parse(File.ReadAllText(file));
                Assert.That(asmdef["name"].ToString() + ".asmdef.txt", Is.EqualTo(Path.GetFileName(file)));
                Assert.That(asmdef["rootNamespace"].ToString(), Is.EqualTo(asmdef["name"].ToString()), file);
            }
        }

        [Test]
        public void Command_LaysIntoOutputRoot_NamedAfterThePackageModule()
        {
            var env = CreateModuleCommand.CreateModule(host_only: true, from_package: EventBusPackage, output_root: ModulesHome);

            Assert.That(env.Code, Is.EqualTo(ResultCode.Ok), env.Message);
            Assert.That(((ModuleGenerationResult)env.Payload).modulePath, Is.EqualTo(ModulesHome + "/EventBus"));
        }

        [Test]
        public void Command_Refuses_WhatTheModeCannotDo()
        {
            Assert.That(CreateModuleCommand.CreateModule(host_only: true, output_root: ModulesHome).Code, Is.EqualTo(ResultCode.Fail));
            Assert.That(CreateModuleCommand.CreateModule("Probe", from_package: EventBusPackage, output_root: ModulesHome).Code, Is.EqualTo(ResultCode.Fail));
            Assert.That(CreateModuleCommand.CreateModule(host_only: true, from_package: EventBusPackage, with: "Nope", output_root: ModulesHome).Code, Is.EqualTo(ResultCode.Fail));

            var absent = CreateModuleCommand.CreateModule(host_only: true, from_package: "com.biome.absent", output_root: ModulesHome);
            Assert.That(absent.Code, Is.EqualTo(ResultCode.Fail));
            Assert.That(absent.Message, Does.Contain("com.biome.absent"));
            Assert.That(Directory.Exists(ModulesAbs), Is.False);
        }

        [UnityTest]
        public IEnumerator Skeleton_Compiles([ValueSource(nameof(s_packages))] string package)
        {
            var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            Assume.That(editorAssemblies.Any(a => a.name == "VContainer"), "the skeleton's installers need VContainer, a host dependency");

            ModuleGenerator.CreateHostOnly("Probe", ModulesAbs, BiomeUtilsPaths.PackageTemplates(package), BiomeUtilsPaths.ProjectRoot, ModuleGenerator.OptionalHostLayers);

            // The sources sit outside Assets/, so nothing is imported and no domain reload happens.
            var sources = Directory.GetFiles(Path.Combine(ModulesAbs, "Probe"), "*.cs", SearchOption.AllDirectories)
                .Select(BiomeUtilsPaths.ToProjectRelative)
                .ToArray();
            var builder = new AssemblyBuilder(TempRoot + "/Probe.dll", sources)
            {
                // The host test assemblies are Editor-only, and Tests/Editor uses the UnityEditor API.
                flags = AssemblyBuilderFlags.EditorAssembly,
                referencesOptions = ReferencesOptions.UseEngineModules,
                additionalReferences = editorAssemblies.Select(a => a.outputPath)
                    .Append(typeof(Assert).Assembly.Location)
                    .ToArray(),
            };
            UnityEditor.Compilation.CompilerMessage[] messages = null;
            builder.buildFinished += (path, result) => messages = result;

            Assert.That(builder.Build(), "the assembly build did not start");
            while (messages == null) yield return null;

            var errors = messages.Where(m => m.type == CompilerMessageType.Error).Select(m => m.message).ToArray();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
    }

    public class EnvelopeTests
    {
        [Test]
        public void Ok_HasNoError_Fail_HasError()
        {
            var ok = BiomeEnvelope.Ok("x", "fine", 1);
            Assert.That(ok.Code, Is.EqualTo(ResultCode.Ok));
            Assert.That(ok.Error, Is.Null);
            var fail = BiomeEnvelope.Fail("x", "nope");
            Assert.That(fail.Code, Is.EqualTo(ResultCode.Fail));
            Assert.That(fail.Error, Is.EqualTo("nope"));
            var ex = BiomeEnvelope.Exception("x", new InvalidOperationException("boom"));
            Assert.That(ex.Code, Is.EqualTo(ResultCode.Exception));
            Assert.That(ex.Message, Does.Contain("boom"));
        }

        [Test]
        public void Json_HasTheFiveFields()
        {
            var j = JObject.FromObject(BiomeEnvelope.Ok("chan", "msg", new { a = 1 }, new[] { "w" }));
            foreach (var f in new[] { "code", "message", "channel", "warnings", "payload" })
                Assert.That(j[f], Is.Not.Null, f);
            Assert.That(j["channel"].ToString(), Is.EqualTo("chan"));
        }
    }

    public class ScannerAndInvokeTests
    {
        private const string TestsPath = "Assets/BiomeUtils/Tests";
        private const string ToolPath = "Assets/BiomeUtils";

        [Test]
        public void Scanner_FindsProbes_ReportsNoDoc_HashesStable()
        {
            var a = CallableScanner.Scan(TestsPath, out var warningsA);
            var b = CallableScanner.Scan(TestsPath, out _);
            Assert.That(a.Select(c => c.name), Does.Contain("ProbeParams").And.Contain("ProbeUndocumented"));
            Assert.That(warningsA.Any(w => w.StartsWith("no-doc: ProbeUndocumented")), string.Join("|", warningsA));
            Assert.That(a.Single(c => c.name == "ProbeParams").hasDoc, Is.True);
            Assert.That(a.Single(c => c.name == "ProbeParams").paramDocs.Keys, Does.Contain("tags"));
            Assert.That(a.Select(c => c.hash), Is.EqualTo(b.Select(c => c.hash)));
            Assert.That(a.Single(c => c.name == "ProbeParams").Key, Is.EqualTo("biome_probe_params"));
        }

        [Test]
        public void Scanner_ToolCallables_AllDocumented()
        {
            var tool = CallableScanner.Scan("Assets/BiomeUtils/Commands", out var warnings);
            Assert.That(tool.Select(c => c.Key), Is.EquivalentTo(new[] { "create_module", "invoke", "discover" }));
            Assert.That(warnings, Is.Empty);
        }

        [Test]
        public void Invoke_MapsJsonOntoSignature()
        {
            var env = InvokeCommand.Invoke(TestsPath, "ProbeSum", "{\"values\":[1,2,3]}");
            Assert.That(env.Code, Is.EqualTo(ResultCode.Ok), env.Message);
            Assert.That(env.Payload, Is.EqualTo(6));

            var undoc = InvokeCommand.Invoke(TestsPath, "ProbeUndocumented", "{\"a\":5}");
            Assert.That(undoc.Payload, Is.EqualTo(7));   // b defaults to 2

            var probe = InvokeCommand.Invoke(TestsPath, "biome_probe_params", "{\"tags\":[\"x\",\"y\"],\"args\":{\"label\":\"L\",\"count\":3,\"items\":[\"i\"]}}");
            Assert.That(probe.Code, Is.EqualTo(ResultCode.Ok), probe.Message);
            var p = JObject.FromObject(probe.Payload);
            Assert.That(p["tagCount"].Value<int>(), Is.EqualTo(2));
            Assert.That(p["label"].ToString(), Is.EqualTo("L"));
            Assert.That(p["items"].Count(), Is.EqualTo(1));
        }

        [Test]
        public void Invoke_ReportsMissingParam_Unknown_Exception()
        {
            Assert.That(InvokeCommand.Invoke(TestsPath, "ProbeUndocumented", "{}").Code, Is.EqualTo(ResultCode.Fail));
            var extra = InvokeCommand.Invoke(TestsPath, "ProbeUndocumented", "{\"a\":1,\"zzz\":1}");
            Assert.That(extra.Warnings.Single(), Does.Contain("zzz"));
            var thrown = InvokeCommand.Invoke(TestsPath, "ProbeThrows", "{\"why\":\"boom\"}");
            Assert.That(thrown.Code, Is.EqualTo(ResultCode.Exception));
            Assert.That(thrown.Message, Does.Contain("boom"));
            Assert.That(InvokeCommand.Invoke(TestsPath, "Nope", null).Code, Is.EqualTo(ResultCode.Fail));
            Assert.That(InvokeCommand.Invoke("Assets/Nowhere", "ProbeSum", null).Code, Is.EqualTo(ResultCode.Fail));
        }

        [Test]
        public void Discover_Render_RoundTripsHashes()
        {
            var scanned = CallableScanner.Scan(ToolPath, out _);
            var body = DiscoverCommand.Render(ToolPath, scanned);
            foreach (var c in scanned)
                Assert.That(body, Does.Contain($"<!-- h: {c.Key} {c.hash} -->"));
            Assert.That(DiscoverCommand.TrySplit("a\n" + DiscoverCommand.MarkerStart + "\nX\n" + DiscoverCommand.MarkerEnd + "\nb",
                out var head, out var mid, out var tail));
            Assert.That(head, Is.EqualTo("a\n"));
            Assert.That(mid, Is.EqualTo("\nX\n"));
            Assert.That(tail, Is.EqualTo("\nb"));
        }

        [Test]
        public void Discover_Check_RejectsBadMode_AndMissingPath()
        {
            Assert.That(DiscoverCommand.Discover(ToolPath, "nope").Code, Is.EqualTo(ResultCode.Fail));
            Assert.That(DiscoverCommand.Discover("Assets/Nowhere", "check").Code, Is.EqualTo(ResultCode.Fail));
        }

        [Test]
        public void ConfigCheck_DetectsMismatch_AndUnreachableIdentity()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "biome-identity-" + Guid.NewGuid().ToString("N") + ".md");
            try
            {
                File.WriteAllText(tmp, "```yaml\nproject-name: Other\nasmdef-root: Zed\n```\n");
                var drift = DiscoverCommand.CheckConfig(tmp, out var w);
                Assert.That(drift.Count, Is.EqualTo(2), string.Join("|", drift));
                Assert.That(w, Is.Empty);

                var good = DiscoverCommand.CheckConfig(DiscoverCommand.DefaultIdentityPath, out var w2);
                Assert.That(good, Is.Empty, string.Join("|", good));

                var missing = DiscoverCommand.CheckConfig("../nowhere/identity.md", out var w3);
                Assert.That(missing, Is.Empty);
                Assert.That(w3.Single(), Does.StartWith("identity-unreachable"));
            }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }
    }
}
