using CMS.Agent.Models.Domain;
using CMS.Agent.Repositories;
using CMS.Shared.Kafka.Commands;
using Microsoft.Extensions.Logging;

namespace CMS.Agent.Services;

public interface IEntrySevice
{
    Task AddNewEntry(AddEntryCommand newEntry);
}

public class EntrySevice : IEntrySevice
{
    private readonly ILogger<EntrySevice> _logger;
    private readonly IFileRepository _fileRepository;
    private readonly IEntryRepository _entryRepository;

    public EntrySevice(
        IFileRepository fileRepository,
        IEntryRepository entryRepository,
        ILogger<EntrySevice> logger)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _entryRepository = entryRepository;
    }
    
    public async Task AddNewEntry(AddEntryCommand newEntry)
    {
        var entryPath = await _fileRepository.CopyNewEntryAsync(
            newEntry.PackageFileName,
            BuildPath(newEntry.PackageName, newEntry.PackageVersion, newEntry.PackageFileName)
        );
        
        var plistPath = await _fileRepository.CopyNewEntryAsync(
            newEntry.PlistFileName,
            BuildPath(newEntry.PackageName, newEntry.PackageVersion, newEntry.PlistFileName)
        );
        
        var entry = new EntryPackage
        {
            Name = newEntry.PackageName,
            Version = newEntry.PackageVersion,
            Path = entryPath,
            PlistPath = plistPath
        };
        await _entryRepository.Add(entry);
    }

    private string BuildPath(string packageName, string packageVersion, string fileName)
    {
        var directory = string.Format("{0}@{1}", packageName, packageVersion);
        return Path.Combine(directory, string.Format("{0}{1}", packageName, Path.GetExtension(fileName)));
    }
}