using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace CMS.Shared.Kafka.Serialization;

public class JsonSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, Default.SerializationOptions));
    }
}