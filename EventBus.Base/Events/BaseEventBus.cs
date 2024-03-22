using EventBus.Base.Abstraction;
using EventBus.Base.SubscriptionManager;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Events
{
    public abstract class BaseEventBus : IEventBus
    {
        private readonly IServiceProvider ServiceProvider;
        public readonly IEventBusSubscritonManager SubsManager;
        public EventBusConfig EventBusConfig { get; set; }

        public BaseEventBus(IServiceProvider serviceProvider, EventBusConfig config)
        {
            ServiceProvider = serviceProvider;
            EventBusConfig = config;
            SubsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
        }

        public virtual string ProcessEventName(string eventName)
        {
            if (EventBusConfig.DeleteEventPreFix)
                eventName = eventName.TrimStart(EventBusConfig.EventNamePrefix.ToArray());

            if (EventBusConfig.DeleteEventSuffix)
                eventName = eventName.TrimEnd(EventBusConfig.EventNameSuffix.ToArray());
            return eventName;
        }

        public virtual string GetSubName(string eventName)
        {
            return $"{EventBusConfig.SubcriberClientAppName}.{ProcessEventName(eventName)}";
        }

        public virtual void Dispose()
        {
            EventBusConfig = null;
        }

        public async Task<bool> ProcessEvent(string eventName,string message)
        {
            eventName = ProcessEventName(eventName);
            var processed = false;

            if(SubsManager.HasSubscripionForEvent(eventName))
            {
                var subscriptions=SubsManager.GetHandlerForEvent(eventName);

                using(var scope=ServiceProvider./*dikkat*/CreateAsyncScope())

                foreach(var subscription in subscriptions)
                {

                        var handler = ServiceProvider.GetService(subscription.HandlerType);

                        if (handler == null) continue;

                        var eventType = SubsManager.GetEventTypeByName($"{EventBusConfig.EventNamePrefix}{eventName}{EventBusConfig.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        var concreteType=typeof(IIntergrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new[] { integrationEvent });
                }
            }
            processed = true;
            return processed;
        }

        public abstract void Publish(IntegrationEvent @event);

        public abstract void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntergrationEventHandler<T>;

        public abstract void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntergrationEventHandler<T>;
    }
}
