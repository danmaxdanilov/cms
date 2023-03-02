using System.Text.Json.Serialization;

namespace CMS.Shared.Kafka.Events;

public class AddEntry : IntegrationEvent
{
    [JsonPropertyName("task-id")]
    public string TaskId { get; set; }
    
    [JsonPropertyName("package-name")]
    public string PackageName { get; set; }
    
    [JsonPropertyName("package-version")]
    public string PackageVersion { get; set; }
    
    [JsonPropertyName("package-filename")]
    public string PackageFileName { get; set; }

    [JsonPropertyName("package-plist-filename")]
    public string PlistFileName { get; set; }
}