using EventBus.Base.Abstraction;
using EventBus.Base.Abstraction.Events;
using EventBus.Base.Subscription;

namespace EventBus.Base.SubscriptionManager
{
    public class InMemoryEventBusSubscriptionManager : IEventBusSubscritonManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;

        private readonly List<Type> _eventTypes;

        public event EventHandler<string> OnEventRemoved;

        public Func<string, string> eventNameGetter;

        public InMemoryEventBusSubscriptionManager(Func<string, string> eventNameGetter)
        {
            _eventTypes = new List<Type>();
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            this.eventNameGetter = eventNameGetter;
        }

        public bool IsEmpty => !_handlers.Keys.Any();

        public void Clear() => _handlers.Clear();

        public void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntergrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            AddSubscription(typeof(TH), eventName);
            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
        }

        public void AddSubscription(Type handlerType, string eventName)
        {
            if (!HasSubscripionForEvent(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }
            if (_handlers[eventName].Any(x => x.HandlerType == handlerType))
            {
                throw new ArgumentException($"Handler Type{handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }
            _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
        }

        public void RemoveSubscripion<T, TH>() where TH : IIntergrationEventHandler<T> where T : IntegrationEvent
        {
            var handlerToRemove = FindSubcriptionToRemove<T, TH>();
            var eventName = GetEventKey<T>();
            RemoveHandler(eventName, handlerToRemove);
        }

        private void RemoveHandler(string eventName, SubscriptionInfo subsToRemove)
        {
            if (subsToRemove != null)
            {
                _handlers[eventName].Remove(subsToRemove);
                if (!_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);
                    var eventType = _eventTypes.SingleOrDefault(x => x.Name == eventName);
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventType);
                    }
                    RaiseOnEventRemove(eventName);
                }
            }
        }

        public IEnumerable<SubscriptionInfo> GetHandlerForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return GetHandlerForEvent(key);
        }

        public IEnumerable<SubscriptionInfo> GetHandlerForEvent(string eventName) => _handlers[eventName];

        private void RaiseOnEventRemove(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }

        private SubscriptionInfo FindSubcriptionToRemove<T, TH>() where T : IntegrationEvent where TH : IIntergrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            return FindSubcriptionToRemove(eventName, typeof(TH));
        }

        private SubscriptionInfo FindSubcriptionToRemove(string eventName, Type handlerType)
        {
            if (!HasSubscripionForEvent(eventName))
            {
                return null;
            }
            return _handlers[eventName].SingleOrDefault(e => e.HandlerType == handlerType);
        }

        public bool HasSubscripionForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return HasSubscripionForEvent(key);
        }

        public bool HasSubscripionForEvent(string eventName) => _handlers.ContainsKey(eventName);

        public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t => t.Name == eventName);

        public string GetEventKey<T>()
        {
            string eventName = typeof(T).Name;
            return eventNameGetter(eventName);
        }








    }
}
