using System;
using System.Net;
using CMS.Shared.Kafka;
using Confluent.Kafka;
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
        private readonly KafkaProducer<Null, string> _producer;
        private readonly ILogger<KafkaController> _logger;
        
        // GET: /<controller>/
        public KafkaController(IConfiguration config, KafkaProducer<Null, string> producer, ILogger<KafkaController> logger)
        {
            _topic = config.GetValue<string>("Kafka:FrivolousTopic");;
            _producer = producer;
            _logger = logger;
        }
            
        [HttpGet]
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        public IActionResult ProduceMessage()
        {
            _producer.Produce(_topic, new Message<Null, string> 
                { Value = $"Frivolous message #{(int)(new Random((int)DateTime.Now.Ticks).NextDouble()*100)}" }, deliveryReportHandler);
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