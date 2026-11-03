using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Biome.Utils.Commands;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine.TestTools;

namespace Biome.Utils.GameStates.Tests
{
    /// <summary>Tests of the <c>create_game_state</c> generator: the rendered text, the refusals, and a real compile of the output.</summary>
    [TestFixture]
    public sealed class GameStateGeneratorTests
    {
        private const string TempRoot = "Temp/GameStateGeneratorTests";
        private const string SampleId = "GeneratedSample";

        [TearDown]
        public void TearDown()
        {
            var absolute = GameStateGenerator.Absolute(TempRoot);
            if (Directory.Exists(absolute))
            {
                Directory.Delete(absolute, true);
            }
        }

        [Test]
        public void Render_Template_ReplacesEveryToken()
        {
            var state = Render(GameStateGenerator.StateTemplate);
            var installer = Render(GameStateGenerator.InstallerTemplate);

            StringAssert.DoesNotContain("{{", state);
            StringAssert.DoesNotContain("{{", installer);
            StringAssert.Contains("namespace " + BiomeUtilsConfig.Current.AppNamespace, state);
            StringAssert.Contains("public sealed class DemoState : IGameState", state);
            StringAssert.Contains("public sealed class DemoStateInstaller : StateInstallerBase", installer);
            StringAssert.Contains("<see cref=\"DemoState\"/>", installer);
        }

        [Test]
        public void Defaults_ComeFromTheConfig()
        {
            var config = BiomeUtilsConfig.Current;

            Assert.IsNotEmpty(config.AppNamespace);
            Assert.IsNotEmpty(config.StatesRoot);
            Assert.AreEqual(config.AppNamespace, GameStateGenerator.DefaultNamespace);
            Assert.AreEqual("Assets/" + config.StatesRoot, GameStateGenerator.DefaultStatesRoot);
        }

        [Test]
        public void MissingPackageProblem_NamesTheAbsentPackage()
        {
            Assert.IsNull(GameStateGenerator.MissingPackageProblem(GameStateGenerator.PackageName));
            StringAssert.Contains("com.biome.absent", GameStateGenerator.MissingPackageProblem("com.biome.absent"));
        }

        [Test]
        public void IsValidStateId_BadIds_AreRejected()
        {
            Assert.IsTrue(GameStateGenerator.IsValidStateId("Menu"));
            Assert.IsTrue(GameStateGenerator.IsValidStateId("Level2"));
            Assert.IsFalse(GameStateGenerator.IsValidStateId(null));
            Assert.IsFalse(GameStateGenerator.IsValidStateId(string.Empty));
            Assert.IsFalse(GameStateGenerator.IsValidStateId("menu"));
            Assert.IsFalse(GameStateGenerator.IsValidStateId("2Menu"));
            Assert.IsFalse(GameStateGenerator.IsValidStateId("Main Menu"));
            Assert.IsFalse(GameStateGenerator.IsValidStateId("Main.Menu"));
        }

        [Test]
        public void CreateGameState_InvalidId_FailsWithoutWriting()
        {
            var env = GameStateGenerator.CreateGameState("bad id", TempRoot);

            Assert.AreEqual(ResultCode.Fail, env.Code);
            Assert.IsFalse(Directory.Exists(GameStateGenerator.Absolute(TempRoot)));
        }

        [Test]
        public void CreateGameState_ExistingFolder_FailsAndKeepsContent()
        {
            var absolute = GameStateGenerator.Absolute(TempRoot);
            Directory.CreateDirectory(absolute);
            var marker = Path.Combine(absolute, "Keep.txt");
            File.WriteAllText(marker, "keep");

            var env = GameStateGenerator.CreateGameState("Demo", TempRoot);

            Assert.AreEqual(ResultCode.Fail, env.Code);
            Assert.AreEqual("keep", File.ReadAllText(marker));
            Assert.AreEqual(1, Directory.GetFileSystemEntries(absolute).Length);
        }

        [UnityTest]
        public IEnumerator CreateGameState_Sample_CompilesToStateAndInstallerPair()
        {
            Assume.That(
                CompilationPipeline.GetAssemblies(AssembliesType.Editor).Any(editorAssembly => editorAssembly.name == "Biome.GameStates.Installers"),
                "the state installer template derives from the host's installers layer");

            var env = GameStateGenerator.CreateGameState(SampleId, TempRoot);
            Assert.AreEqual(ResultCode.Ok, env.Code, env.Message);

            // The sources sit outside Assets/, so nothing is imported and no domain reload happens.
            var output = TempRoot + "/" + SampleId + ".dll";
            var sources = Directory.GetFiles(GameStateGenerator.Absolute(TempRoot), "*.cs")
                .Select(path => TempRoot + "/" + Path.GetFileName(path))
                .ToArray();
            var builder = new AssemblyBuilder(output, sources)
            {
                additionalReferences = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                    .Select(assembly => assembly.outputPath)
                    .ToArray(),
            };
            CompilerMessage[] messages = null;
            builder.buildFinished += (path, result) => messages = result;

            Assert.IsTrue(builder.Build(), "the assembly build did not start");
            while (messages == null)
            {
                yield return null;
            }

            var errors = messages.Where(message => message.type == CompilerMessageType.Error).Select(message => message.message).ToArray();
            Assert.IsEmpty(errors, string.Join("\n", errors));

            var assembly = System.Reflection.Assembly.LoadFrom(GameStateGenerator.Absolute(output));
            var appNamespace = BiomeUtilsConfig.Current.AppNamespace;
            var stateType = assembly.GetType(appNamespace + "." + SampleId + "State");
            var installerType = assembly.GetType(appNamespace + "." + SampleId + "StateInstaller");
            Assert.IsNotNull(stateType);
            Assert.IsNotNull(installerType);
            Assert.IsTrue(stateType.GetInterfaces().Any(type => type.FullName == "Biome.GameStates.Abstract.IGameState"));
            Assert.AreEqual("Biome.GameStates.Installers.StateInstallerBase", installerType.BaseType?.FullName);
            Assert.IsTrue(stateType.IsSealed && installerType.IsSealed);
            Assert.IsNotNull(installerType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null));
        }

        private static string Render(string templateName)
        {
            var path = Path.Combine(GameStateGenerator.TemplatesAbsolute(), templateName);
            return GameStateGenerator.Render(File.ReadAllText(path), "Demo", GameStateGenerator.DefaultNamespace);
        }
    }
}
