using CMS.Agent.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace CMS.Agent.Utils;

public class LiteDataContext : DbContext
{
    public LiteDataContext(DbContextOptions<LiteDataContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<EntryPackage>();
        
        base.OnModelCreating(modelBuilder);
    }
    
    public DbSet<EntryPackage> EntryPackages { get; set; }
}