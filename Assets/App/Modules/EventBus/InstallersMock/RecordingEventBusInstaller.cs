using Biome.EventBus.Abstract;
using Biome.EventBus.Fakes;
using VContainer;
using VContainer.Unity;

namespace Biome.EventBus.Installers.Mock
{
    /// <summary>
    /// Mock registration: the <see cref="RecordingEventBus"/> under the same three contracts as
    /// <see cref="EventBusInstaller"/>, owned and disposed by its scope.
    /// </summary>
    public sealed class RecordingEventBusInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IEventBus>(_ => new RecordingEventBus(), Lifetime.Singleton)
                .As<IEventBus, IEventPublisher, IEventSubscriber>();
        }
    }
}
