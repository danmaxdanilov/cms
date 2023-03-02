using System.Threading.Tasks.Dataflow;
using Autofac;

namespace CMS.Shared.Kafka.Events;

public interface IIntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task HandleAsync(TIntegrationEvent @event);
    
    protected ActionBlock<TIntegrationEvent> Worker { get; set; }
}

public interface IIntegrationEventHandler
{
}