using System.Net;
using System.Threading.Tasks.Dataflow;
using CMS.Shared.Kafka.Events;
using CMS.Shared.Kafka.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CMS.Shared.Kafka;

    public abstract class BaseKafkaConsumerWorkerService<TMessageKey, TMessageValue> : IHostedService
        where TMessageValue : IntegrationEvent
    {
        private const string ConfigBlockName = "KafkaConsumer";
        private readonly IConfiguration _configuration;
        private readonly ConsumerConfig _consumerConfig;
        private Task _runningTask;
        protected CancellationTokenSource CancellationTokenSource;

        private readonly ILogger _logger;

        protected BaseKafkaConsumerWorkerService
        (
            IConfiguration configuration,
            ILogger logger
        )
        {
            _configuration = configuration;
            _consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = configuration["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false
            };
            _logger = logger;
        }

        protected abstract ActionBlock<TMessageValue> Worker { get; }
        protected ILogger Logger { get; }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            CancellationTokenSource = new CancellationTokenSource();
            var deserializer = new JsonDeserializer<TMessageValue>();
            var consumer = new ConsumerBuilder<TMessageKey, TMessageValue>(_consumerConfig)
                .SetValueDeserializer(deserializer)
                .Build();

            var topicName = TopicNameResolveUtils.ResolveName<TMessageValue>();
            consumer.Subscribe(new[] {topicName});

            var consumeTimeout = GetConfigValue("TimeoutMilliseconds", IntParse, 1000);

            _runningTask =
                Task.Factory.StartNew(async () =>
                    {
                        _logger.LogInformation("Topic: {Topic}. Consuming started. ConsumerGroupId: {GroupId}",
                            topicName, _consumerConfig.GroupId);

                        while (!CancellationTokenSource.IsCancellationRequested)
                        {
                            var consumeIteration = Guid.NewGuid();
                            
                            try
                            {
                                var message = consumer.Consume(consumeTimeout);

                                if (message == default)
                                    continue;

                                if (message.Message?.Value == default)
                                {
                                    _logger.LogInformation(
                                        "Topic: {Topic}. Consume iteration: {consumeIteration}. Message.Value is empty",
                                        topicName, consumeIteration);
                                    continue;
                                }

                                _logger.LogDebug(
                                    "Topic: {Topic}. Consume iteration: {consumeIteration}. Offset: {offset}. Consuming succeed!",
                                    topicName, consumeIteration, message.Offset);

                                var record = message.Message.Value;

                                if (!await Worker.SendAsync(record, CancellationTokenSource.Token))
                                {
                                    _logger.LogInformation(
                                        "Topic: {Topic}. Consume iteration: {consumeIteration}. Offset: {offset}. Send async returns false",
                                        topicName, consumeIteration, message.Offset.Value);

                                    await Task.Delay(1000, cancellationToken);
                                    continue;
                                }

                                if (CancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    consumer.Commit(message);
                                    _logger.LogInformation(
                                        "Topic: {Topic}. Consume iteration: {consumeIteration}. Offset: {offset}. Cancel requested",
                                        topicName, consumeIteration, message.Offset.Value);

                                    CancellationTokenSource.Token.ThrowIfCancellationRequested();
                                }

                                consumer.Commit(message);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Topic: {Topic}. Consume iteration: {consumeIteration}. Consume error",
                                    topicName, consumeIteration);
                                await Task.Delay(1000, cancellationToken);
                            }
                        }

                        consumer.Close();
                        consumer.Dispose();

                        Worker.Complete();
                        await Worker.Completion;
                    },
                    CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            CancellationTokenSource.Cancel();

            await _runningTask;

            _runningTask.Dispose();
            _runningTask = null;
        }

        private static int IntParse(string value, int defaultValue)
        {
            return int.TryParse(value, out var parsed)
                ? parsed
                : defaultValue;
        }

        private T GetConfigValue<T>(string configName, Func<string, T, T> parser, T defaultValue)
        {
            var value = _configuration[$"{ConfigBlockName}.{nameof(TMessageValue)}{configName}"]
                        ?? _configuration[$"{ConfigBlockName}.{configName}"]
                        ?? string.Empty;

            return value == string.Empty
                ? defaultValue
                : parser(value, defaultValue);
        }
    }