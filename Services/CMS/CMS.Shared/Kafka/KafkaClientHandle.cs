using Confluent.Kafka;

namespace CMS.Shared.Kafka;

public class KafkaClientHandle : IDisposable
{
    IProducer<byte[], byte[]> kafkaProducer;

    public KafkaClientHandle(string bootstrapServers)
    {
        var conf = new ProducerConfig();
        conf.BootstrapServers = bootstrapServers;
        this.kafkaProducer = new ProducerBuilder<byte[], byte[]>(conf).Build();
    }

    public Handle Handle { get => this.kafkaProducer.Handle; }

    public void Dispose()
    {
        // Block until all outstanding produce requests have completed (with or
        // without error).
        kafkaProducer.Flush();
        kafkaProducer.Dispose();
    }
}