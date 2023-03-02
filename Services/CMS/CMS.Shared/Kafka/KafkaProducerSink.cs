using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Autofac;
using CMS.Shared.Kafka.Events;
using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Shared.Kafka;

public interface IKafkaSink<TMessageKey, TMessageValue>
    where TMessageValue : IntegrationEvent
{
    Task SendAsync(TMessageValue record, CancellationToken cancellationToken);
    Task SendAsync(TMessageKey key, TMessageValue record, CancellationToken cancellationToken);

    Task SendAsync(TMessageKey key, TMessageValue record, string topic,
        CancellationToken cancellationToken);
}

public class KafkaProducerSink<TMessageKey, TMessageValue> : IKafkaSink<TMessageKey, TMessageValue>, IDisposable
    where TMessageValue : IntegrationEvent
{
    private static readonly string TopicName = TopicNameResolveUtils.ResolveName<TMessageValue>();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TMessageValue> _logger;
    private readonly IProducer<TMessageKey, TMessageValue> _producer;
    private readonly ILifetimeScope _scope;


    public KafkaProducerSink(IProducer<TMessageKey, TMessageValue> producer, ILifetimeScope scope,
        ILogger<TMessageValue> logger, IHttpContextAccessor httpContextAccessor)
    {
        _producer = producer;
        _scope = scope;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
    
    public async Task SendAsync(TMessageValue record, CancellationToken cancellationToken)
    {
        await _producer.ProduceAsync(TopicName,
            new Message<TMessageKey, TMessageValue>
            {
                Value = record
            }, cancellationToken);
        _logger.LogDebug("Record produced {@Message} to Topic: {Topic}", record, TopicName);
    }

    public async Task SendAsync(TMessageKey key, TMessageValue record, CancellationToken cancellationToken)
    {
        await _producer.ProduceAsync(TopicName,
            new Message<TMessageKey, TMessageValue>
            {
                Key = key,
                Value = record
            }, cancellationToken);
        _logger.LogDebug("Record produced {@Message} to Topic: {Topic}", record, TopicName);
    }

    public async Task SendAsync(TMessageKey key, TMessageValue record, string topic,
        CancellationToken cancellationToken)
    {
        await _producer.ProduceAsync(topic,
            new Message<TMessageKey, TMessageValue>
            {
                Key = key,
                Value = record
            }, cancellationToken);
        _logger.LogDebug("Record produced {@Message} to Topic: {Topic}", record, TopicName);
    }
}