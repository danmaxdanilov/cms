using Microsoft.EntityFrameworkCore;

namespace CMS.Agent.Utils;

public interface IContextFactory<out TDbContext> where TDbContext: DbContext
{
    TDbContext Create();
}

public class LiteDataContextFactory : IContextFactory<LiteDataContext>
{
    private readonly DbContextOptions<LiteDataContext> _contextOptions;
    public LiteDataContextFactory(string connectionString)
    {
        _contextOptions = new DbContextOptionsBuilder<LiteDataContext>()
            .UseSqlite(connectionString)
            .Options;
    }
    
    public LiteDataContext Create()
    {
        return new LiteDataContext(_contextOptions);
    }
}