using System.Collections;
using System.Threading;
using Biome.GameStates.Abstract;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Biome.GameStates.Editor.Tests
{
    /// <summary>
    /// The contract of <see cref="ISubStateService"/> as a consumer sees it: the current sub-state, null before the first
    /// switch. A host binds its service by overriding <see cref="CreateService"/>.
    /// </summary>
    public abstract class SubStateServiceContractTests
    {
        private ISubStateService _service;

        [SetUp]
        public void SetUpService() => _service = CreateService();

        [Test]
        public void Current_BeforeTheFirstSwitch_IsNull()
        {
            Assert.That(_service.Current, Is.Null);
        }

        [UnityTest]
        public IEnumerator SwitchAsync_OneAfterAnother_CurrentFollows() => UniTask.ToCoroutine(async () =>
        {
            await _service.SwitchAsync<TestStateA>(CancellationToken.None);
            Assert.That(_service.Current, Is.EqualTo(typeof(TestStateA)));

            await _service.SwitchAsync<TestStateB>(CancellationToken.None);

            Assert.That(_service.Current, Is.EqualTo(typeof(TestStateB)));
        });

        /// <summary>A fresh service under test, before its first switch.</summary>
        protected abstract ISubStateService CreateService();
    }
}
