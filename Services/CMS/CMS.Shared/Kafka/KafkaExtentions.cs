using System.Net;
using System.Reflection;
using Autofac;
using CMS.Shared.Kafka.Events;
using CMS.Shared.Kafka.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CMS.Shared.Kafka;

    public static class KafkaExtensions
    {
        public static ContainerBuilder AddKafka(this ContainerBuilder containerBuilder,
            string kafkaHost,
            string groupId)
        {
            var kafkaProducerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaHost,
                ClientId = Dns.GetHostName()
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
            where TMessageValue : IntegrationEvent
        {
            containerBuilder.Register(x =>
            {
                var adminClient = x.Resolve<IAdminClient>();

                adminClient.TryCreateKafkaTopicAsync(
                    TopicNameResolveUtils.ResolveName<TMessageValue>()
                    ).GetAwaiter().GetResult();

                return new ProducerBuilder<TMessageKey, TMessageValue>(config ?? x.Resolve<ProducerConfig>())
                    .SetValueSerializer(new JsonSerializer<TMessageValue>())
                    .Build();
            }).SingleInstance();
            
            containerBuilder.RegisterType<KafkaProducerSink<TMessageKey, TMessageValue>>()
                .As<IKafkaSink<TMessageKey, TMessageValue>>()
                .SingleInstance();

            return containerBuilder;
        }


        public static ContainerBuilder AddConsumerHandler<TMessageKey, TMessageValue>(
            this ContainerBuilder containerBuilder)
            where TMessageValue : IntegrationEvent
        {
            var topicName = TopicNameResolveUtils.ResolveName<TMessageValue>();
            
            containerBuilder.RegisterConsumerTopic(topicNames);
        
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
        
        private static void RegisterConsumerTopic(this ContainerBuilder containerBuilder, string topicName, int numPartitions = 1)
        {
            containerBuilder.RegisterBuildCallback(x =>
            {
                using var scope = x.BeginLifetimeScope();
                var adminClient = x.Resolve<IAdminClient>();
                var partitionCount = numPartitions;
                var logger = x.Resolve<ILogger>();
        
                var (isSuccess, message) = adminClient.TryCreateKafkaTopicAsync(topicName, partitionCount)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
        
                if (isSuccess)
                    logger.LogInformation(message);
                else
                    logger.LogWarning(message);
            });
        }

        // public static ContainerBuilder BindConsumerHandlers(
        //     this ContainerBuilder containerBuilder,
        //     Type[] handlerArgumentTypes, 
        //     IConfiguration configuration)
        // {
        //     var topicNames = handlerArgumentTypes
        //         .Select(TopicNameResolveUtils.ResolveName)
        //         .ToArray();
        //     
        //     containerBuilder.RegisterConsumerTopics(topicNames);
        //
        //     foreach (var handlerArgumentType in handlerArgumentTypes)
        //     {
        //         var tMessageKey = typeof(Null);
        //         var tMessageValue = handlerArgumentType;
        //
        //         // Register consumer service for message type
        //         var consumerType = typeof(KafkaConsumerWorkerService<,>)
        //             .MakeGenericType(tMessageKey, tMessageValue);
        //         containerBuilder
        //             .RegisterType(consumerType)
        //             .As<IHostedService>()
        //             .SingleInstance();
        //         
        //         // Register consumer service settings for message type
        //         var consumerSettingsType = typeof(ConsumerWorkerOptions<,>)
        //             .MakeGenericType(tMessageKey, tMessageValue);
        //         var consumerConfigurationSection = configuration.GetSection("ConsumerConcurrency");
        //         containerBuilder
        //             .Register(x =>
        //             {
        //                 var parallelDegree =
        //                     consumerConfigurationSection.GetValue($"{tMessageKey.Name}_{tMessageValue.Name}", 
        //                     consumerConfigurationSection.GetValue("Default", 20));
        //                 return Activator.CreateInstance(consumerSettingsType, parallelDegree, parallelDegree);
        //             })
        //             .As(consumerSettingsType)
        //             .SingleInstance();
        //     }
        //
        //     return containerBuilder;
        // }
    }