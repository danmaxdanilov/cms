using System.Text.Json.Serialization;

namespace CMS.Shared.Kafka.Events;

public class IntegrationEvent
{
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    [JsonConstructor]
    public IntegrationEvent(Guid id, DateTime createDate)
    {
        Id = id;
        CreationDate = createDate;
    }

    [JsonPropertyName("id")]
    public Guid Id { get; private set; }

    [JsonPropertyName("creation-date")]
    public DateTime CreationDate { get; private set; }
}