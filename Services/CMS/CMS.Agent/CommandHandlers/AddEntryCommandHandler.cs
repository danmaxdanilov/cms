using System.Threading.Tasks.Dataflow;
using Autofac;
using CMS.Agent.Services;
using CMS.Shared.Domain;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Commands;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CMS.Agent.CommandHandlers;

public class AddEntryCommandHandler: BaseKafkaConsumerWorkerService<string, AddEntryCommand>, ICommandHandler<string, AddEntryCommand>
{
    private readonly ILogger<AddEntryCommandHandler> _logger;
    private IKafkaSink<string, AddEntryResponseCommand> _responseSink;
    private readonly IEntrySevice _service;

    public AddEntryCommandHandler(
        IConfiguration configuration, 
        ILogger<AddEntryCommandHandler> logger, 
        IEntrySevice service,
        IKafkaSink<string, AddEntryResponseCommand> responseSink,
        IAdminClient adminClient) : base(configuration, adminClient, logger)
    {
        _logger = logger;
        _service = service;
        _responseSink = responseSink;
    }

    protected override ActionBlock<Tuple<string, AddEntryCommand>> Worker => new(HandleAsync);

    public async Task HandleAsync(Tuple<string, AddEntryCommand> record)
    {
        var key = record.Item1;
        var value = record.Item2;

        try
        {
            await _service.AddNewEntry(value);
            var response = new AddEntryResponseCommand
            {
                EntryId = value.EntryId,
                CommandStatus = CommandStatus.Done
            };
            
            await _responseSink.SendAsync(key, response, CancellationToken.None);
            
            _logger.LogInformation(string.Format("Entry name: {0} version: {1} successfully added", value.PackageName, value.PackageVersion));
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Something is wrong with handler");
            var response = new AddEntryResponseCommand
            {
                EntryId = value.EntryId,
                CommandStatus = CommandStatus.Error,
                CommandErrorMessage = string.Format("Something is wrong with handler.\r\n{0}", e.Message)
            };
            await _responseSink.SendAsync(key, response, CancellationToken.None);
        }
    }
}