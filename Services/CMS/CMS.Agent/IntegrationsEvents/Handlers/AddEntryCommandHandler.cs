using System.Threading.Tasks.Dataflow;
using Autofac;
using CMS.Agent.Services;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CMS.Agent.IntegrationsEvents.Handlers;

internal class AddEntryCommandHandler: BaseKafkaConsumerWorkerService<string, AddEntry>
{
    private readonly ILogger<AddEntryCommandHandler> _logger;
    private readonly ILifetimeScope _scope;
    
    public AddEntryCommandHandler(
        IConfiguration configuration, 
        ILogger<AddEntryCommandHandler> logger, 
        ILifetimeScope scope) : base(configuration, logger)
    {
        _logger = logger;
        _scope = scope;
        Worker = new ActionBlock<AddEntry>(
            HandleAsync);
    }

    protected override ActionBlock<AddEntry> Worker { get; }

    private async Task HandleAsync(AddEntry @event)
    {
        await using var currentScope = _scope.BeginLifetimeScope();
        try
        {
            var servive = _scope.Resolve<IEntrySevice>();
            await servive.AddNewEntry(@event);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Something is wrong with handler");
        }
    }
}