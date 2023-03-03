using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Autofac;
using CMS.Shared.Kafka.Commands;
using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Shared.Kafka;

public interface IKafkaSink<TMessageKey, TMessageValue>
    where TMessageValue : IntegrationCommand
{
    Task SendAsync(TMessageValue record, CancellationToken cancellationToken);
    Task SendAsync(TMessageKey key, TMessageValue record, CancellationToken cancellationToken);

    Task SendAsync(TMessageKey key, TMessageValue record, string topic,
        CancellationToken cancellationToken);
}

public class KafkaProducerSink<TMessageKey, TMessageValue> : IKafkaSink<TMessageKey, TMessageValue>, IDisposable
    where TMessageValue : IntegrationCommand
{
    private static readonly string TopicName = TopicNameResolveUtils.ResolveName<TMessageValue>();
    private readonly ILogger<TMessageValue> _logger;
    private readonly IProducer<TMessageKey, TMessageValue> _producer;


    public KafkaProducerSink(IProducer<TMessageKey, TMessageValue> producer,
        ILogger<TMessageValue> logger)
    {
        _producer = producer;
        _logger = logger;
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