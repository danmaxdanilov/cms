using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Reflection;
using Autofac;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CMS.Shared.Kafka;

    public static class KafkaExtensions
    {
        public static void AddKafka(this ContainerBuilder containerBuilder, 
            Assembly handlersAssembly,
            IConfiguration configuration, 
            Action<ConsumerConfig, ProducerConfig, AdminClientConfig> optionsBuilder,
            string kafkaHostUrlSectionName = "KafkaHostUrl")
        {
            var handlerArgumentTypes = handlersAssembly
                .GetTypes()
                .Select(x => x.GetInterface(typeof(IRequestHandler<>).Name))
                .Where(x => x != null && x.GetCustomAttribute<SubscribeIgnoreAttribute>() == null)
                .Select(x => x.GenericTypeArguments[0])
                .Where(x => typeof(BusRecordBase).IsAssignableFrom(x))
                .ToArray();

            if (optionsBuilder == null)
            {
                var kafkaHost = configuration[kafkaHostUrlSectionName];
                if (!string.IsNullOrEmpty(kafkaHost))
                {
                    containerBuilder.AddKafkaCluster(kafkaHost, groupId: handlersAssembly.GetName().Name);
                    containerBuilder.BindConsumerHandlers(handlerArgumentTypes, configuration);
                }
            }
            else
            {
                containerBuilder.AddKafkaCluster(optionsBuilder);
                containerBuilder.BindConsumerHandlers(handlerArgumentTypes, configuration);
            }
            
            containerBuilder.RegisterGeneric(typeof(RecordHandlerDurationPipelineBehavior<,>))
                .As(typeof(IPipelineBehavior<,>));
        }
        
        internal static ContainerBuilder AddKafkaCluster(
            this ContainerBuilder containerBuilder,
            Action<ConsumerConfig, ProducerConfig, AdminClientConfig> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder), " can't be null");

            var consumerConfig = new ConsumerConfig();
            var producerConfig = new ProducerConfig();
            var adminClientConfig = new AdminClientConfig();

            optionsBuilder.Invoke(consumerConfig, producerConfig, adminClientConfig);

            SetUpCluster(containerBuilder, producerConfig, adminClientConfig, () => new ConsumerConfig(consumerConfig));

            return containerBuilder;
        }

        public static ContainerBuilder AddKafkaCluster(
            this ContainerBuilder containerBuilder, [NotNull] string kafkaHost, string clientId = null,
            string groupId = null)
        {
            var kafkaProducerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaHost,
                ClientId = clientId ?? Dns.GetHostName()
            };

            var kafkaAdminConfig = new AdminClientConfig
            {
                BootstrapServers = kafkaHost
            };

            SetUpCluster(containerBuilder, kafkaProducerConfig, kafkaAdminConfig,
                () => new ConsumerConfig
                {
                    BootstrapServers = kafkaHost,
                    GroupId = groupId ?? Assembly.GetEntryAssembly()?.GetName().Name ?? Dns.GetHostName(),
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }
            );

            return containerBuilder;
        }

        private static void SetUpCluster(ContainerBuilder containerBuilder, ProducerConfig kafkaProducerConfig,
            AdminClientConfig kafkaAdminConfig, Func<ConsumerConfig> consumerConfigFactory)
        {
            containerBuilder.Register(x => consumerConfigFactory.Invoke()).InstancePerDependency();
            containerBuilder.RegisterInstance(kafkaProducerConfig);
            containerBuilder.Register(x => new AdminClientBuilder(kafkaAdminConfig).Build()).SingleInstance();
        }

        public static ContainerBuilder AddKafkaProducer<TMessageKey, TMessageValue>(
            this ContainerBuilder containerBuilder, ProducerConfig config = default)
            where TMessageValue : BusRecordBase
        {
            containerBuilder.Register(x =>
            {
                var adminClient = x.Resolve<IAdminClient>();
                adminClient.TryCreateKafkaTopicsAsync
                (
                    x.Resolve<IConfiguration>().GetPartitionCountOrDefault(),
                    TopicNameResolveUtils.ResolveName<TMessageValue>()
                ).GetAwaiter().GetResult();

                return new ProducerBuilder<TMessageKey, TMessageValue>(config ?? x.Resolve<ProducerConfig>())
                    .SetValueSerializer(new ByJsonSerializer<TMessageValue>())
                    .Build();
            }).SingleInstance();

            AddOrIgnoreResponseConsumer<TMessageValue>(containerBuilder);

            containerBuilder.RegisterType<KafkaProducerSink<TMessageKey, TMessageValue>>()
                .As<IKafkaSink<TMessageKey, TMessageValue>>()
                .SingleInstance();

            return containerBuilder;
        }

        private static void AddOrIgnoreResponseConsumer<TMessageValue>(ContainerBuilder containerBuilder)
        {
            var responseMarkerType = typeof(IResponseFor<>).MakeGenericType(typeof(TMessageValue));
            var responseTypes = typeof(TMessageValue).Assembly.GetTypes()
                .Where(x => responseMarkerType.IsAssignableFrom(x)).ToArray();
            if (!responseTypes.Any()) return;

            containerBuilder.RegisterConsumerTopics(responseTypes.Select(TopicNameResolveUtils.ResolveName).ToArray());

            foreach (var responseType in responseTypes)
            {
                var consumerType = typeof(RecordBroadcaster<,>).MakeGenericType(typeof(Null), responseType);
                var tMessageKey = typeof(Null);
                var tMessageValue = responseType;
                var consumerSettingsType = typeof(ConsumerWorkerOptions<,>)
                    .MakeGenericType(tMessageKey, tMessageValue);
                containerBuilder
                    .Register(x =>
                    {
                        var configuration = x.Resolve<IConfiguration>();
                        var consumerConfigurationSection = configuration.GetSection("ConsumerConcurrency");
                        var parallelDegree =
                            consumerConfigurationSection.GetValue($"{tMessageKey.Name}_{tMessageValue.Name}",
                                consumerConfigurationSection.GetValue("Default", 20));
                        return Activator.CreateInstance(consumerSettingsType, parallelDegree, parallelDegree);
                    }).As(consumerSettingsType).SingleInstance();
                containerBuilder.RegisterType(consumerType)
                    .As(typeof(IRecordBroadcaster<>).MakeGenericType(responseType), typeof(IHostedService))
                    .SingleInstance();
            }
        }

        private static void RegisterConsumerTopics(this ContainerBuilder containerBuilder, string[] topicNames)
        {
            containerBuilder.RegisterBuildCallback(x =>
            {
                using var scope = x.BeginLifetimeScope();
                var adminClient = x.Resolve<IAdminClient>();
                var partitionCount = x.Resolve<IConfiguration>().GetPartitionCountOrDefault();
                var loggerFactory = x.Resolve<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("TopicRegistration");

                var (isSuccess, message) = adminClient.TryCreateKafkaTopicsAsync(partitionCount, topicNames)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                if (isSuccess)
                    logger.LogInformation(message);
                else
                    logger.LogWarning(message);
            });
        }

        public static ContainerBuilder BindConsumerHandlers(
            this ContainerBuilder containerBuilder,
            Type[] handlerArgumentTypes, 
            IConfiguration configuration)
        {
            var topicNames = handlerArgumentTypes
                .Select(TopicNameResolveUtils.ResolveName)
                .ToArray();
            
            containerBuilder.RegisterConsumerTopics(topicNames);

            foreach (var handlerArgumentType in handlerArgumentTypes)
            {
                var tMessageKey = typeof(Null);
                var tMessageValue = handlerArgumentType;

                // Register consumer service for message type
                var consumerType = typeof(KafkaConsumerWorkerService<,>)
                    .MakeGenericType(tMessageKey, tMessageValue);
                containerBuilder
                    .RegisterType(consumerType)
                    .As<IHostedService>()
                    .SingleInstance();
                
                // Register consumer service settings for message type
                var consumerSettingsType = typeof(ConsumerWorkerOptions<,>)
                    .MakeGenericType(tMessageKey, tMessageValue);
                var consumerConfigurationSection = configuration.GetSection("ConsumerConcurrency");
                containerBuilder
                    .Register(x =>
                    {
                        var parallelDegree =
                            consumerConfigurationSection.GetValue($"{tMessageKey.Name}_{tMessageValue.Name}", 
                            consumerConfigurationSection.GetValue("Default", 20));
                        return Activator.CreateInstance(consumerSettingsType, parallelDegree, parallelDegree);
                    })
                    .As(consumerSettingsType)
                    .SingleInstance();
            }

            return containerBuilder;
        }

        private static int GetPartitionCountOrDefault(this IConfiguration configuration)
        {
            return int.TryParse(configuration["KafkaCluster.Topics.PartitionCount"], out var count)
                ? count
                : 1;
        }
    }