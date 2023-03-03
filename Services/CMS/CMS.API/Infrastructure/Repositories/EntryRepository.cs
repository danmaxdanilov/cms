using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.API.Models.DomainModels;
using CMS.API.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Infrastructure.Repositories;

public interface IEntryRepository
{
    Task<List<Entry>> FindEntriesByFilterAsync(
        string entryName,
        string entryVersion,
        int pageIndex,
        int pageSize);

    Task<int> CountEntriesByFilterAsync(
        string entryName,
        string entryVersion);

    Task<Entry> FindFirstEntryByFilterAsync(
        string entryName,
        string entryVersion);

    Task<Entry> AddEntryAsync(
        Entry entry
    );

    Task<Entry> FindEntryById(string id);
}

public class EntryRepository : IEntryRepository
{
    private readonly PgDbContext _context;

    public EntryRepository(PgDbContext context)
    {
        _context = context;
    }

    public async Task<int> CountEntriesByFilterAsync(
        string entryName,
        string entryVersion)
    {
        IQueryable<Entry> result = _context.Entries;

        if (!string.IsNullOrEmpty(entryName))
            result = result.Where(x => x.Name.Contains(entryName));
        if (!string.IsNullOrEmpty(entryVersion))
            result = result.Where(x => x.Version.Contains(entryVersion));
        
        
        return await result.CountAsync();
    }
    
    public async Task<List<Entry>> FindEntriesByFilterAsync(
        string entryName,
        string entryVersion,
        int pageIndex,
        int pageSize)
    {
        IQueryable<Entry> result = _context.Entries.Include(x => x.Commands);

        if (!string.IsNullOrEmpty(entryName))
            result = result.Where(x => x.Name.Contains(entryName));
        if (!string.IsNullOrEmpty(entryVersion))
            result = result.Where(x => x.Version.Contains(entryVersion));
        
        
        return await result.OrderBy(x => x.Id).Skip(pageIndex*pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<Entry> FindFirstEntryByFilterAsync(
        string entryName,
        string entryVersion)
    {
        IQueryable<Entry> result = _context.Entries.Include(x => x.Commands);

        if (!string.IsNullOrEmpty(entryName))
            result = result.Where(x => x.Name.Contains(entryName));
        if (!string.IsNullOrEmpty(entryVersion))
            result = result.Where(x => x.Version.Contains(entryVersion));

        return await result.FirstOrDefaultAsync();
    }

    public async Task<Entry> FindEntryById(string id)
    {
        return await _context.Entries.SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Entry> AddEntryAsync(
        Entry entry
    )
    {
        await _context.AddAsync(entry);
        await _context.SaveChangesAsync();
        return entry;
    }
}