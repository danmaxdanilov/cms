namespace CMS.Shared.Kafka;

    public abstract class BaseKafkaConsumerWorkerService<TMessageKey, TMessageValue> : IHostedService
        where TMessageValue : BusRecordBase, IConsumerRecord
    {
        private const string ConfigBlockName = "KafkaConsumer";
        private readonly IConfiguration _configuration;
        private readonly ConsumerConfig _consumerConfig;
        private readonly ConsumerWorkerOptions<TMessageKey, TMessageValue> _workerOptions;
        private Task _runningTask;
        protected CancellationTokenSource CancellationTokenSource;

        protected BaseKafkaConsumerWorkerService
        (
            ConsumerConfig consumerConfig,
            IConfiguration configuration,
            ILogger logger,
            ConsumerWorkerOptions<TMessageKey, TMessageValue> workerOptions
        )
        {
            _consumerConfig = consumerConfig;
            _configuration = configuration;
            Logger = logger;
            _workerOptions = workerOptions;
            _consumerConfig.GroupId = ExploreNewGroup(_consumerConfig.GroupId);
        }

        protected abstract ActionBlock<TMessageValue> Worker { get; }
        protected ILogger Logger { get; }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            CancellationTokenSource = new CancellationTokenSource();
            var deserializer = new ByJsonDeserializer<TMessageValue>();
            var consumer = new ConsumerBuilder<TMessageKey, TMessageValue>(_consumerConfig)
                .SetValueDeserializer(deserializer)
                .Build();

            var topicName = TopicNameResolveUtils.ResolveName<TMessageValue>();
            consumer.Subscribe(new[] {topicName});

            var consumeTimeout = GetConfigValue("TimeoutMilliseconds", IntParse, 1000);

            _runningTask =
                Task.Factory.StartNew(async () =>
                    {
                        Logger.LogInformation("Topic: {Topic}. Consuming started. ConsumerGroupId: {GroupId}",
                            topicName, _consumerConfig.GroupId);
                        Logger.LogInformation("Topic: {Topic}. Consuming started. Concurrency degree: {ParallelDegree}",
                            topicName, _workerOptions.ParallelDegree);

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
                                    Logger.LogInformation(
                                        "Topic: {Topic}. Consume iteration: {consumeIteration}. Message.Value is empty",
                                        topicName, consumeIteration);
                                    continue;
                                }

                                Logger.LogDebug(
                                    "Topic: {Topic}. Consume iteration: {consumeIteration}. Offset: {offset}. Consuming succeed!",
                                    topicName, consumeIteration, message.Offset);

                                var record = message.Message.Value;

                                if (!await Worker.SendAsync(record, CancellationTokenSource.Token))
                                {
                                    Logger.LogInformation(
                                        "Topic: {Topic}. Consume iteration: {consumeIteration}. Offset: {offset}. Send async returns false",
                                        topicName, consumeIteration, message.Offset.Value);

                                    await Task.Delay(1000, cancellationToken);
                                    continue;
                                }

                                if (CancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    consumer.Commit(message);
                                    Logger.LogInformation(
                                        "Topic: {Topic}. Consume iteration: {consumeIteration}. Offset: {offset}. Cancel requested",
                                        topicName, consumeIteration, message.Offset.Value);

                                    CancellationTokenSource.Token.ThrowIfCancellationRequested();
                                }

                                consumer.Commit(message);
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(e, "Topic: {Topic}. Consume iteration: {consumeIteration}. Consume error",
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

        private static string ExploreNewGroup(string consumerConfigGroupId)
        {
            var allowedForAllPostfix = Dns.GetHostName();
            return (typeof(TMessageValue).GetCustomAttribute<ConsumingAttribute>()?.Mode ?? default) switch
            {
                ConsumeMode.SingleConsuming => typeof(TMessageValue).Name + "_SINGLE_CONSUME",
                ConsumeMode.AllowedForAllInstances => consumerConfigGroupId +
                                                      $"_{allowedForAllPostfix}",
                _ => consumerConfigGroupId
            };
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