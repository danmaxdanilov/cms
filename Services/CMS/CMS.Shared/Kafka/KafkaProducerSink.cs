using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Autofac;
using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Shared.Kafka;

public class KafkaProducerSink
{
        public class KafkaProducerSink<TMessageKey, TMessageValue> : IKafkaSink<TMessageKey, TMessageValue>, IDisposable
        where TMessageValue : BusRecordBase
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

        public async Task<TResponse> CallAsync<TResponse>(TMessageValue record, TimeSpan timeout,
            CancellationToken cancellationToken)
            where TResponse : BusRecordBase, IResponseFor<TMessageValue>
        {
            SetupTraceId(record);
            var task = TakeFromChannelAsync<TResponse>(record, timeout, cancellationToken);
            await _producer.ProduceAsync(TopicName,
                new Message<TMessageKey, TMessageValue>
                {
                    Value = record
                }, cancellationToken);
            _producer.Flush(cancellationToken);
            _logger.LogDebug("Record produced {@Message} to Topic: {Topic}", record, TopicName);
            return await task;
        }

        public Task<TResponse> CallAsync<TResponse>(TMessageValue record, CancellationToken cancellationToken)
            where TResponse : BusRecordBase, IResponseFor<TMessageValue>
        {
            return CallAsync<TResponse>(record, Defaults.DefaultSinkCallTimeout, cancellationToken);
        }

        public async Task<TResponse> PulseAsync<TResponse>(TMessageValue record, TimeSpan timeout,
            CancellationToken cancellationToken)
            where TResponse : BusRecordBase
        {
            SetupTraceId(record);
            var task = TakeFromChannelAsync<TResponse>(record, timeout, cancellationToken);
            await _producer.ProduceAsync(TopicName,
                new Message<TMessageKey, TMessageValue>
                {
                    Value = record
                }, cancellationToken);
            _logger.LogDebug("Record produced {@Message} to Topic: {Topic}", record, TopicName);
            return await task;
        }


        public async Task SendAsync(TMessageValue record, CancellationToken cancellationToken)
        {
            SetupTraceId(record);
            await _producer.ProduceAsync(TopicName,
                new Message<TMessageKey, TMessageValue>
                {
                    Value = record
                }, cancellationToken);
            _logger.LogDebug("Record produced {@Message} to Topic: {Topic}", record, TopicName);
        }

        public async Task SendAsync(TMessageKey key, TMessageValue record, CancellationToken cancellationToken)
        {
            SetupTraceId(record);
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
            SetupTraceId(record);
            await _producer.ProduceAsync(topic,
                new Message<TMessageKey, TMessageValue>
                {
                    Key = key,
                    Value = record
                }, cancellationToken);
            _logger.LogDebug("Record produced {@Message} to Topic: {Topic}", record, TopicName);
        }

        private void SetupTraceId(TMessageValue record)
        {
            if (record.TraceId == default)
            {
                var traceIdAccessor = _httpContextAccessor?.HttpContext?.RequestServices.GetService<ITraceIdAccessor>();
                if (traceIdAccessor != null)
                    record.TraceId = traceIdAccessor.TraceId;
                else
                    _logger.LogWarning(
                        $"Record with empty TraceId('{typeof(TMessageValue).FullName}'). ITraceIdAccessor is not accessible or not registered");
            }
        }

        private Task<TResponse> TakeFromChannelAsync<TResponse>(TMessageValue record, TimeSpan timeout,
            CancellationToken cancellationToken) where TResponse : BusRecordBase
        {
            SetupTraceId(record);
            var channelBroadcaster = _scope.Resolve<IRecordBroadcaster<TResponse>>();
            return Observable.FromEventPattern<RecordReceivedEventArgs<TResponse>>(
                    x => channelBroadcaster.RecordReceived += x,
                    x => channelBroadcaster.RecordReceived -= x)
                .Timeout(timeout)
                .FirstOrDefaultAsync(x => x.EventArgs.Record.CorrelationId == record.CorrelationId)
                .Select(x => x.EventArgs.Record)
                .ToTask(cancellationToken);
        }
    }
}