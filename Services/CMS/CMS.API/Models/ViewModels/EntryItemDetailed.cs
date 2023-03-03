namespace CMS.API.Models.ViewModels;

public class EntryItemDetailed : EntryItem
{
    public string PackageFilePath { get; set; }

    public string PlistFilePath { get; set; }

    public string PlistFileContent { get; set; }
}