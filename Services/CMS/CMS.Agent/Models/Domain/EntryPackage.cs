using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Agent.Models.Domain;

[Table("ENTRY_PACKAGE")]
public class EntryPackage
{
    [Key]
    [Column("ID", TypeName = "INTEGER")]
    public int Id { get; set; }
    
    [Column("NAME", TypeName = "TEXT")]
    public string Name { get; set; }

    [Column("VERSION", TypeName = "TEXT")]
    public string Version { get; set; }
    
    [Column("PATH", TypeName = "TEXT")]
    public string Path { get; set; }
}