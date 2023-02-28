using CMS.Agent.Models.Domain;
using CMS.Agent.Services;
using CMS.Agent.Utils;

namespace CMS.Agent.Repositories;

public interface IEntryRepository
{
    Task Add(EntryPackage entry);
    Task Update(EntryPackage entry);
    Task Remove(EntryPackage entry);
    Task<EntryPackage> FindById(int id);
}

public class EntryRepository : IEntryRepository
{
    private readonly LiteDataContext _ctx;

    public EntryRepository(LiteDataContext ctx)
    {
        _ctx = ctx;
    }
    
    public async Task Add(EntryPackage entry)
    {
        await _ctx.AddAsync(entry);
        await _ctx.SaveChangesAsync();
    }

    public async Task Update(EntryPackage entry)
    {
        var oldEntry = await _ctx.EntryPackages.FindAsync(entry.Id);
        
        oldEntry.Name = entry.Name;
        oldEntry.Path = entry.Path;
        oldEntry.Version = entry.Version;
        
        _ctx.EntryPackages.Update(oldEntry);
        await _ctx.SaveChangesAsync();
    }

    public async Task Remove(EntryPackage entry)
    {
        var oldEntry = await _ctx.EntryPackages.FindAsync(entry.Id);

        _ctx.Remove(oldEntry);
        await _ctx.SaveChangesAsync();
    }

    public async Task<EntryPackage> FindById(int id)
    {
        return await _ctx.EntryPackages.FindAsync(id);
    }
}