using System.Text.Json.Serialization;

namespace CMS.Shared.Kafka.Commands;

public class RemoveEntryCommand : IntegrationCommand
{
    [JsonPropertyName("entry-id")]
    public string EntryId { get; set; }
    
    [JsonPropertyName("remove-reason")]
    public string Reason { get; set; }
}