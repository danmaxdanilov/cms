using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.API.Models.DomainModels;

[Table("command", Schema = "cms_schema")]
public class Command
{
    public string Id { get; set; }
    public string Status { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime EventDate { get; set; }
    
    public string Comment { get; set; }

    [ForeignKey("entryId")]
    public Entry Entry { get; set; }
}