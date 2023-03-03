using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CMS.API.Infrastructure.Repositories;
using CMS.API.Infrastructure.Services;
using CMS.API.Models.DomainModels;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Commands;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Extensions;

namespace CMS.API.CommandHandlers;

public class AddEntryCommandResponseHandler: BaseKafkaConsumerWorkerService<string, AddEntryResponseCommand>, ICommandHandler<string,AddEntryResponseCommand>
{
    private readonly ILogger<AddEntryCommandResponseHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public AddEntryCommandResponseHandler(
        IConfiguration configuration, 
        IAdminClient adminClient, 
        ILogger<AddEntryCommandResponseHandler> logger, IServiceProvider serviceProvider) 
        : base(configuration, adminClient, logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override ActionBlock<Tuple<string, AddEntryResponseCommand>> Worker => new(HandleAsync);
    
    public async Task HandleAsync(Tuple<string, AddEntryResponseCommand> record)
    {
        _logger.LogInformation($"response received: key: {record.Item1}, message: {record.Item2.EntryId}");

        using var scope = _serviceProvider.CreateScope();
        var entryService = scope.ServiceProvider.GetRequiredService<IEntryService>();

        await entryService.AddCommandToEntry(
            record.Item2.EntryId,
            record.Item2.CommandStatus,
            record.Item2.CommandErrorMessage);
    }
}