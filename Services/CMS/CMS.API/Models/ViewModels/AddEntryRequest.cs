namespace CMS.API.Models.ViewModels;

public class AddEntryRequest
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string FileName { get; set; }
    public string PlistFileName { get; set; }
}