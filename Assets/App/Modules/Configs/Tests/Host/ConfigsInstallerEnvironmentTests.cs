using System;
using System.Collections;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using Biome.Configs.Installers;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer;

namespace Biome.Configs.Tests.Host
{
    /// <summary>The environment the <see cref="ConfigsInstaller"/> takes by reference in its constructor and registers in the container.</summary>
    [TestFixture]
    public sealed class ConfigsInstallerEnvironmentTests
    {
        [Test]
        public void Constructor_NullEnvironment_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigsInstaller(null));
        }

        [Test]
        public void Install_Environment_ResolvesToTheInstancePassedIn()
        {
            IConfigsEnvironment environment = NewEnvironment();
            using (IObjectResolver container = Build(environment))
            {
                Assert.AreSame(environment, container.Resolve<IConfigsEnvironment>());
            }
        }

        [UnityTest]
        public IEnumerator Install_ConfigInitEntryPoint_ResolvesFromTheContainerAndInitialises() => UniTask.ToCoroutine(async () =>
        {
            using (IObjectResolver container = Build(NewEnvironment()))
            {
                var service = container.Resolve<IConfigsService>();

                await service.InitializeAsync(container.Resolve<IConfigsEnvironment>(), CancellationToken.None);

                Assert.IsTrue(service.IsInitialized);
            }
        });

        private static IConfigsEnvironment NewEnvironment()
        {
            return ConfigsTestEnvironment.FromFake(new FakeConfigsEnvironment("installer-env"));
        }

        private static IObjectResolver Build(IConfigsEnvironment environment)
        {
            var builder = new ContainerBuilder();
            new ConfigsInstaller(environment).Install(builder);
            return builder.Build();
        }
    }
}
