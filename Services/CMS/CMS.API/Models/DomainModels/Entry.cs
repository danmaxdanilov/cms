using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.API.Models.DomainModels;

[Table("entry", Schema = "cms_schema")]
public class Entry
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string FileName { get; set; }
    public string PlistFileName { get; set; }

    public List<Command> Commands { get; set; }
}