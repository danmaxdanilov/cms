using System.Text.Json.Serialization;
using CMS.Shared.Domain;

namespace CMS.Shared.Kafka.Commands;

public class AddEntryResponseCommand: IntegrationCommand
{
    [JsonPropertyName("entry-id")]
    public string EntryId { get; set; }
    
    [JsonPropertyName("command-status")]
    public CommandStatus CommandStatus { get; set; }

    [JsonPropertyName("command-error-message")]
    public string CommandErrorMessage { get; set; }
}