namespace CMS.Shared.Kafka;

internal class KafkaConsumerWorkerService<TMessageKey, TMessageValue> : BaseKafkaConsumerWorkerService<TMessageKey, TMessageValue> 
    where TMessageValue : BusRecordBase, IConsumerRecord
{
    private readonly ILifetimeScope _scope;

    public KafkaConsumerWorkerService(ConsumerConfig consumerConfig, IConfiguration configuration,
        ILogger<KafkaConsumerWorkerService<TMessageKey, TMessageValue>> logger,
        ConsumerWorkerOptions<TMessageKey, TMessageValue>  workerOptions, ILifetimeScope scope)
        : base(consumerConfig, configuration, logger, workerOptions)
    {
        _scope = scope;
        Worker = new ActionBlock<TMessageValue>(
            HandleAsync,
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = workerOptions.ParallelDegree,
                BoundedCapacity = workerOptions.BufferSize
            });
    }

    protected override ActionBlock<TMessageValue> Worker { get; }

    private async Task HandleAsync(TMessageValue msg)
    {
        await using var currentScope = _scope.BeginLifetimeScope();
        using var _ = LoggerExtensions.UsingTraceId(msg.TraceId == Guid.Empty ? msg.CorrelationId : msg.TraceId);
        try
        {
            var mediator = currentScope.Resolve<IMediator>();
            await mediator.Send(msg, CancellationTokenSource.Token);
        }
        catch (Exception e)
        {
            Logger.LogCritical(e, "Something is wrong with handler");
        }
    }
}