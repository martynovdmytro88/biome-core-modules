using Biome.GameStates.Abstract;
using VContainer;

namespace Biome.App.Tests
{
    /// <summary>
    /// The test variant of the profile scope: the common installers over <c>EnvironmentConfig.Test</c> and the test flow,
    /// whose one state lives on the test side. It lists that state in the app's state table; the app never names it.
    /// </summary>
    public sealed class TestScope : AppProfileScope
    {
        protected override void DeclareStates(AppStatesInstaller states)
        {
            states.AddCatalogEntry<TestInitState, TestInitStateInstaller>(scope =>
            {
                scope.Register<DefaultStateScenarioHandler>(Lifetime.Scoped).As<IStateScenarioHandler>();
                TestInitStateInstaller.RegisterFakes(scope);
            });
        }
    }
}
