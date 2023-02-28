﻿using CMS.Agent.IntegrationsEvents.Events;
using CMS.Agent.Models.Domain;
using CMS.Agent.Repositories;
using Microsoft.Extensions.Logging;

namespace CMS.Agent.Services;

public interface IEntrySevice
{
    Task AddNewEntry(AddEntry newEntry);
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
    
    public async Task AddNewEntry(AddEntry newEntry)
    {
        var entryPath = await _fileRepository.CopyNewEntryAsync(
            newEntry.FileName,
            BuildPath(newEntry.PackageName, newEntry.PackageVersion, newEntry.FileName)
        );
        
        var entry = new EntryPackage
        {
            Name = newEntry.PackageName,
            Version = newEntry.PackageVersion,
            Path = entryPath
        };
        await _entryRepository.Add(entry);
    }

    private string BuildPath(string packageName, string packageVersion, string fileName)
    {
        var directory = string.Format("{0}@{1}", packageName, packageVersion);
        return Path.Combine(directory, string.Format("{0}{1}", packageName, Path.GetExtension(fileName)));
    }
}