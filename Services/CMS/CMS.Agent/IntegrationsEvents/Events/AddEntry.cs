namespace CMS.Agent.IntegrationsEvents.Events;

public class AddEntry
{
    public string Id { get; set; }
    public string PackageName { get; set; }
    public string PackageVersion { get; set; }
    
    public string PackageFileName { get; set; }
    public string PlistFileName { get; set; }
}