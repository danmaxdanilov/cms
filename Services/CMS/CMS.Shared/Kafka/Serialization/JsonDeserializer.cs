using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace CMS.Shared.Kafka.Serialization;

public class JsonDeserializer<T>: IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(data.ToArray()), Default.SerializationOptions);
    }
}