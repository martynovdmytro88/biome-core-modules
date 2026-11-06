using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Biome.Configs.Abstract;
using Biome.Configs.Fakes;
using Biome.Configs.Installers;
using Biome.Configs.Installers.Mock;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using VContainer;

namespace Biome.Configs.Tests.Host
{
    /// <summary>The production <see cref="ConfigsInstaller"/> against a real container, and its parity with the mock installer.</summary>
    [TestFixture]
    public sealed class ConfigsInstallerTests
    {
        private static readonly Type[] s_serviceFaces = { typeof(IConfigsService), typeof(IConfigsReader) };

        [Test]
        public void Install_ServiceFaces_ResolveToOneSingletonService()
        {
            using (IObjectResolver container = BuildContainer())
            {
                var service = container.Resolve<IConfigsService>();

                Assert.IsInstanceOf<ConfigsService>(service);
                foreach (Type face in s_serviceFaces)
                {
                    Assert.AreSame(service, container.Resolve(face), face.Name);
                }

                Assert.AreSame(service, container.Resolve<IConfigsService>());
            }
        }

        [Test]
        public void Install_OpenConfigFace_ResolvesAProviderOverTheService()
        {
            using (IObjectResolver container = BuildContainer())
            {
                var provider = container.Resolve<IConfig<TestConfigAsset>>();

                Assert.IsInstanceOf<ConfigProvider<TestConfigAsset>>(provider);
                Assert.Throws<ConfigNotInitializedException>(() => _ = provider.Value);
            }
        }

        [Test]
        public void Install_NoLoadersRegistered_ResolvesAnEmptyLoaderList()
        {
            using (IObjectResolver container = BuildContainer())
            {
                Assert.IsEmpty(container.Resolve<IReadOnlyList<IConfigLoader>>());
            }
        }

        [UnityTest]
        public IEnumerator Install_NoLoaders_InitAndLoadAllComplete() => UniTask.ToCoroutine(async () =>
        {
            using (IObjectResolver container = BuildContainer())
            {
                var service = container.Resolve<IConfigsService>();

                await service.InitializeAsync(NewEnvironment(), CancellationToken.None);
                await service.LoadAllAsync(CancellationToken.None);

                Assert.IsTrue(service.IsInitialized);
                Assert.IsTrue(service.IsLoaded);
            }
        });

        [Test]
        public void Install_NullBuilder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigsInstaller(NewEnvironment()).Install(null));
        }

        [Test]
        public void MockParity_ServiceFaces_ResolveInBothInstallers()
        {
            var builder = new ContainerBuilder();
            new ConfigsInstallerMock(NewEnvironment()).Install(builder);
            using (IObjectResolver mock = builder.Build())
            using (IObjectResolver real = BuildContainer())
            {
                var faces = new[] { typeof(IConfigsService), typeof(IConfigsReader), typeof(IConfig<TestConfigAsset>) };
                foreach (Type face in faces)
                {
                    Assert.IsTrue(real.TryResolve(face, out _), $"real: {face.Name}");
                    Assert.IsTrue(mock.TryResolve(face, out _), $"mock: {face.Name}");
                }

                // Both installers register the environment the boot passes to the init.
                Assert.IsTrue(mock.TryResolve(typeof(IConfigsEnvironment), out _));
                Assert.IsTrue(real.TryResolve(typeof(IConfigsEnvironment), out _));
            }
        }

        private static IConfigsEnvironment NewEnvironment()
        {
            return ConfigsTestEnvironment.FromFake(new FakeConfigsEnvironment("installer"));
        }

        private static IObjectResolver BuildContainer()
        {
            var builder = new ContainerBuilder();
            new ConfigsInstaller(NewEnvironment()).Install(builder);
            return builder.Build();
        }
    }
}
