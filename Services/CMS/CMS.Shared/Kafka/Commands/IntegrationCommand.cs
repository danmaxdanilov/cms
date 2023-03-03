using System.Text.Json.Serialization;

namespace CMS.Shared.Kafka.Commands;

public class IntegrationCommand
{
    public IntegrationCommand()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    [JsonConstructor]
    public IntegrationCommand(Guid id, DateTime createDate)
    {
        Id = id;
        CreationDate = createDate;
    }

    [JsonPropertyName("id")]
    public Guid Id { get; private set; }

    [JsonPropertyName("creation-date")]
    public DateTime CreationDate { get; private set; }
}