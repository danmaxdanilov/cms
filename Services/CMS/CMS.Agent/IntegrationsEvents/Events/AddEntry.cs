using Newtonsoft.Json;

namespace CMS.Agent.IntegrationsEvents.Events;

public class AddEntry
{
    [JsonRequired]
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonRequired]
    [JsonProperty("package-name")]
    public string PackageName { get; set; }
    
    [JsonRequired]
    [JsonProperty("package-version")]
    public string PackageVersion { get; set; }
    
    [JsonRequired]
    [JsonProperty("package-filename")]
    public string PackageFileName { get; set; }
    
    [JsonRequired]
    [JsonProperty("package-plist-filename")]
    public string PlistFileName { get; set; }
}