using EventBus.Base.Events;
using EventBus.Base.Subscription;

namespace EventBus.Base.Abstraction
{
    public interface IEventBusSubscritonManager
    {
        bool IsEmpty { get; }
        event EventHandler<string> OnEventRemoved;
        void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntergrationEventHandler<T>;
        void RemoveSubscripion<T, TH>() where TH : IIntergrationEventHandler<T> where T : IntegrationEvent;
        bool HasSubscripionForEvent<T>() where T : IntegrationEvent;
        bool HasSubscripionForEvent(string eventName);
        Type GetEventTypeByName(string eventName);
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlerForEvent<T>() where T : IntegrationEvent;
        IEnumerable<SubscriptionInfo> GetHandlerForEvent(string eventName);
        string GetEventKey<T>();


    }
}
