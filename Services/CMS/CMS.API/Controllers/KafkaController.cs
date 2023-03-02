using System.Net;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Events;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CMS.API.Controllers;

[Route("api/v1/[controller]")]
//[Authorize]
[ApiController]
public class KafkaController : Controller
{

        private string _topic;
        private readonly ILogger<KafkaController> _logger;

        private readonly ProducerConfig _producerConfig;
        private readonly IKafkaSink<string, AddEntry> _sink;

        // GET: /<controller>/
        public KafkaController(
            IConfiguration config, 
            IKafkaSink<string,AddEntry> sink,
            ILogger<KafkaController> logger)
        {
            _topic = config.GetValue<string>("Kafka:FrivolousTopic");
            _logger = logger;
            
            _producerConfig = new ProducerConfig();
            config.GetSection("Kafka:ProducerSettings").Bind(_producerConfig);

            _sink = sink;
        }
            
        //[HttpGet]
        // [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        // public IActionResult ProduceMessage()
        // {
        //     _producer.Produce(_topic, new Message<Null, string> 
        //         { Value = $"Frivolous message #{(int)(new Random((int)DateTime.Now.Ticks).NextDouble()*100)}" }, deliveryReportHandler);
        //     return Ok();
        // }
        
         [HttpGet("sink")]
         [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
         public async Task<IActionResult> ProduceMessage()
         {
             var entry = new AddEntry
             {
                 TaskId = "id#323",
                 PackageName = "mc111",
                 PackageVersion = "1.29-1",
                 PackageFileName = "mc.pkg",
                 PlistFileName = "mc.plist"
             };
             await _sink.SendAsync("message_key#17", entry, CancellationToken.None);
             return Ok();
         }
        
        [HttpGet]
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ProduceCustomMessage()
        {
            using (var producer =
                   new ProducerBuilder<string, AddEntry>(_producerConfig)
                       .SetValueSerializer(new Shared.Kafka.Serialization.JsonSerializer<AddEntry>())
                       .Build())
            {
                _logger.LogInformation($"{producer.Name} producing on {_topic}");
                var entry = new AddEntry
                {
                    TaskId = "id#3",
                    PackageName = "mc",
                    PackageVersion = "1.29",
                    PackageFileName = "mc.pkg",
                    PlistFileName = "mc.plist"
                };

                await producer.ProduceAsync(_topic, new Message<string, AddEntry> { Value = entry });
            }
            return Ok();
        }
        
        private void deliveryReportHandler(DeliveryReport<Null, string> deliveryReport)
        {
            if (deliveryReport.Status == PersistenceStatus.NotPersisted)
            {
                // It is common to write application logs to Kafka (note: this project does not provide
                // an example logger implementation that does this). Such an implementation should
                // ideally fall back to logging messages locally in the case of delivery problems.
                _logger.Log(LogLevel.Warning, $"Message delivery failed: {deliveryReport.Message.Value}");
            }
        }
    
}