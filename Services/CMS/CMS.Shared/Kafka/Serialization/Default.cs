using System.ComponentModel;
using System.Text.Json;

namespace CMS.Shared.Kafka.Serialization;

public static class Default
{
    public static JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
    {
        Converters = {new TimeSpanConverter()}
    };
}