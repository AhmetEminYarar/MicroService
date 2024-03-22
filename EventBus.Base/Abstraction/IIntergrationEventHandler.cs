
using EventBus.Base.Events;

namespace EventBus.Base.Abstraction
{
    public interface IIntergrationEventHandler<T>: IntergrationEventHandler where T : IntegrationEvent
    {
        Task Handle(T @event);
    }
    public interface IntergrationEventHandler
    {

    }
}
