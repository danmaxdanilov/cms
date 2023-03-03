using System.Linq;
using CMS.API.Models.DomainModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CMS.API.Infrastructure.Repositories;

public class PgDbContext: DbContext
{
    public PgDbContext(DbContextOptions<PgDbContext> options): base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cms_schema");
        base.OnModelCreating(modelBuilder);
        var keysProperties = modelBuilder.Model.GetEntityTypes().Select(x => x.FindPrimaryKey()).SelectMany(x => x.Properties);
        foreach (var property in keysProperties)
        {
            property.ValueGenerated = ValueGenerated.OnAdd;
        }
    }

    public DbSet<Entry> Entries { get; set; }
    
    public DbSet<Command> Commands { get; set; }
}