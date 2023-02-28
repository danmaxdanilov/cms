using CMS.Agent.Repositories;
using CMS.Agent.Utils;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CMS.Agent;

public class MainHostedService : IHostedService
{
    private readonly ILogger<MainHostedService> _logger;
    private readonly string _topic;
    private readonly IConsumer<Null, string> _kafkaConsumer;
    private readonly IFileRepository _repository;
    private readonly IServiceProvider _provider;

    public MainHostedService(
        IConfiguration config,
        ILogger<MainHostedService> logger,
        IFileRepository repository,
        IServiceProvider provider,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _provider = provider;

        //var consumerConfig = new ConsumerConfig();
        //config.GetSection("Kafka:ConsumerSettings").Bind(consumerConfig);
        //consumerConfig.AutoOffsetReset = AutoOffsetReset.Latest;
        //_topic = config.GetValue<string>("Kafka:FrivolousTopic");
        //_kafkaConsumer = new ConsumerBuilder<Null, string>(consumerConfig).Build();

        _repository = repository;
        
        // applicationLifetime.ApplicationStarted.Register(OnStarted);
        // applicationLifetime.ApplicationStopping.Register(OnStopping);
        // applicationLifetime.ApplicationStopped.Register(OnStopped);
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("1. StartAsync has been called.");

        await ApplyDataSchemaAsync(cancellationToken);
        
        //await StartConsumerLoop(cancellationToken);

        //return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("4. StopAsync has been called.");
        
        _kafkaConsumer.Close();
        _kafkaConsumer.Dispose();

        return Task.CompletedTask;
    }

    private void OnStarted()
    {
        _logger.LogInformation("2. OnStarted has been called.");
    }

    private void OnStopping()
    {
        _logger.LogInformation("3. OnStopping has been called.");
    }

    private void OnStopped()
    {
        _logger.LogInformation("5. OnStopped has been called.");
    }
    
    private async Task ApplyDataSchemaAsync(CancellationToken stoppingToken)
    {
        var ctx = _provider.GetRequiredService<LiteDataContext>();
        await ctx.Database.EnsureCreatedAsync(stoppingToken);
    }
    
    private async Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        _kafkaConsumer.Subscribe(this._topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cr = this._kafkaConsumer.Consume(cancellationToken);

                // Handle message...
                Console.WriteLine($"{cr.Message.Key}: {cr.Message.Value}ms");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException e)
            {
                // Consumer errors should generally be ignored (or logged) unless fatal.
                Console.WriteLine($"Consume error: {e.Error.Reason}");

                if (e.Error.IsFatal)
                {
                    // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e}");
                break;
            }
        }
    }
}