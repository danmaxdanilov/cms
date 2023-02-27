using CMS.Shared.Kafka;
using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CMS.API.Infrastructure.Middlewares;
public class RequestTimerMiddleware
{
    private readonly string topic;
    private readonly KafkaProducer<string, long> producer;
    private readonly RequestDelegate next;
    private readonly ILogger logger;

    public RequestTimerMiddleware(RequestDelegate next, KafkaProducer<string, long> producer, IConfiguration config, ILogger<RequestTimerMiddleware> logger)
    {
        this.next = next;
        this.producer = producer;
        this.topic = config.GetValue<string>("Kafka:RequestTimeTopic");
        this.logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        Stopwatch s = new Stopwatch();
        try
        {
            s.Start();
            await next(context);
        }
        finally
        {
            s.Stop();

            // Write request timing infor to Kafka (non-blocking), handling any errors out-of-band.
            producer.Produce(topic, new Message<string, long> { Key = context.Request.Path.Value, Value = s.ElapsedMilliseconds }, deliveryReportHandler);

            // Alternatively, you can await the produce call. This will delay the request until the result of
            // the produce call is known. An exception will be throw in the event of an error.
            // await producer.ProduceAsync(topic, new Message<string, long> { Key = context.Request.Path.Value, Value = s.ElapsedMilliseconds });
        }
    }

    private void deliveryReportHandler(DeliveryReport<string, long> deliveryReport)
    {
        if (deliveryReport.Status == PersistenceStatus.NotPersisted)
        {
            this.logger.Log(LogLevel.Warning, $"Failed to log request time for path: {deliveryReport.Message.Key}");
        }
    }

}