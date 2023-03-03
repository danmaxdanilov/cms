using System.Net;
using System.Reflection;
using Autofac;
using CMS.Shared.Kafka.Commands;
using CMS.Shared.Kafka.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

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
            where TMessageValue : IntegrationCommand
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
            where TMessageValue : IntegrationCommand
        {
            var topicName = TopicNameResolveUtils.ResolveName<TMessageValue>();
            
            containerBuilder.RegisterConsumerTopic(topicName);

            return containerBuilder;
        }
        
        private static void RegisterConsumerTopic(this ContainerBuilder containerBuilder, string topicName, int numPartitions = 1)
        {
            containerBuilder.RegisterBuildCallback(x =>
            {
                using var scope = x.BeginLifetimeScope();
                var adminClient = x.Resolve<IAdminClient>();
                var partitionCount = numPartitions;
        
                var (isSuccess, message) = adminClient.TryCreateKafkaTopicAsync(topicName, partitionCount)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            });
        }

        #region IServiceProvider

        
        public static IServiceCollection AddKafka(this IServiceCollection services,
            string kafkaHost,
            string groupId)
        {
            var kafkaConsumerConfig = new ConsumerConfig
            {
                BootstrapServers = kafkaHost,
                GroupId = groupId ?? Assembly.GetEntryAssembly()?.GetName().Name ?? Dns.GetHostName(),
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
                
            var kafkaProducerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaHost,
                ClientId = Dns.GetHostName()
            };

            var kafkaAdminConfig = new AdminClientConfig
            {
                BootstrapServers = kafkaHost
            };
            
            SetUpCluster(services, kafkaProducerConfig, kafkaAdminConfig,
                () => new ConsumerConfig
                {
                    BootstrapServers = kafkaHost,
                    GroupId = groupId ?? Assembly.GetEntryAssembly()?.GetName().Name ?? Dns.GetHostName(),
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }
            );

            return services;
        }
        
        private static void SetUpCluster(IServiceCollection services, ProducerConfig kafkaProducerConfig,
            AdminClientConfig kafkaAdminConfig, Func<ConsumerConfig> consumerConfigFactory)
        {
            services.AddScoped(x => consumerConfigFactory.Invoke());
            services.AddSingleton(kafkaProducerConfig);
            services.AddSingleton<IAdminClient>(x => new AdminClientBuilder(kafkaAdminConfig).Build());
        }
        
        public static IServiceCollection AddKafkaProducer<TMessageKey, TMessageValue>(
            this IServiceCollection services, ProducerConfig config = default)
            where TMessageValue : IntegrationCommand
        {
            services.AddSingleton<IProducer<TMessageKey, TMessageValue>>(
                provider =>
                {
                    var adminClient = provider.GetRequiredService<IAdminClient>();

                    adminClient.TryCreateKafkaTopicAsync(
                        TopicNameResolveUtils.ResolveName<TMessageValue>()
                    ).GetAwaiter().GetResult();

                    return new ProducerBuilder<TMessageKey, TMessageValue>(config ?? provider.GetRequiredService<ProducerConfig>())
                        .SetValueSerializer(new JsonSerializer<TMessageValue>())
                        .Build();
                });

            services
                .AddSingleton<IKafkaSink<TMessageKey, TMessageValue>, KafkaProducerSink<TMessageKey, TMessageValue>>();

            return services;
        }

        #endregion
    }