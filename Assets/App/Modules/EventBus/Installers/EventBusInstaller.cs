using Biome.EventBus.Abstract;
using Biome.EventBus.Config;
using VContainer;
using VContainer.Unity;

namespace Biome.EventBus.Installers
{
    /// <summary>
    /// Prod registration: one production <see cref="EventBus"/> per scope, exposed as <see cref="IEventBus"/> ·
    /// <see cref="IEventPublisher"/> · <see cref="IEventSubscriber"/>. The container constructs it, so the container
    /// owns it: the bus is disposed with its scope. A thin passthrough of <see cref="IContainerBuilder"/>.
    /// </summary>
    public sealed class EventBusInstaller : IInstaller
    {
        private readonly EventBusConfig _config;

        public EventBusInstaller(EventBusConfig config = null)
        {
            _config = config ?? new EventBusConfig();
        }

        public void Install(IContainerBuilder builder)
        {
            var config = _config;
            builder.Register<IEventBus>(_ => EventBusFactory.Create(config), Lifetime.Singleton)
                .As<IEventBus, IEventPublisher, IEventSubscriber>();
        }
    }
}
