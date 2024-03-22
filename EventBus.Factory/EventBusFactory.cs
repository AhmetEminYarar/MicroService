using EventBus.AzureServiceBus;
using EventBus.Base.Abstraction;
using EventBus.Base.SubscriptionManager;
using EventBus.RabbitMQ;

namespace EventBus.Factory
{
    public static class EventBusFactory
    {
        public static IEventBus Create(EventBusConfig config,IServiceProvider serviceProvider)
        {
            return config.EventBusType switch
            {
                EventBusType.AzureServiceBus => new EventBusServiceBus(serviceProvider, config),
                _ => new EventBusRabbitMQ(serviceProvider, config),
            };
        }
    }
}
