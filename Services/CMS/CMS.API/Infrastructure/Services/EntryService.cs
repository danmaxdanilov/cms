using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CMS.API.Infrastructure.Repositories;
using CMS.API.Models.DomainModels;
using CMS.API.Models.ViewModels;
using CMS.Shared.Domain;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Extensions;

namespace CMS.API.Infrastructure.Services;

public interface IEntryService
{
    Task<PaginatedItemsViewModel<EntryItem>> FindEntriesListByFilter(
        string entryName,
        string entryVersion,
        int pageIndex,
        int pageSize);

    Task<EntryItemDetailed> FindFirstEntryByFilterAsync(
        EntryRequest entryRequest);

    Task<EntryItem> AddEntryToRepository(EntryRequest entryRequest);

    Task AddCommandToEntry(string entryId, CommandStatus commandStatus, string commandErrorMessage);
}

public class EntryService : IEntryService
{
    private readonly ILogger<IEntryService> _logger;
    private readonly IEntryRepository _entryRepository;
    private readonly ICommandRepository _commandRepository;
    private readonly IKafkaSink<string, AddEntryCommand> _kafkaSink;
    private readonly IMapper _mapper;

    public EntryService(
        IEntryRepository repository, 
        ILogger<IEntryService> logger,
        IMapper mapper, 
        ICommandRepository commandRepository, 
        IKafkaSink<string, AddEntryCommand> kafkaSink)
    {
        _entryRepository = repository;
        _logger = logger;
        _mapper = mapper;
        _commandRepository = commandRepository;
        _kafkaSink = kafkaSink;
    }

    public async Task<PaginatedItemsViewModel<EntryItem>> FindEntriesListByFilter(
        string entryName,
        string entryVersion,
        int pageIndex,
        int pageSize)
    {
        var totalItems = await _entryRepository.CountEntriesByFilterAsync(entryName, entryVersion);

        var dataFromDb = await _entryRepository.FindEntriesByFilterAsync(entryName, entryVersion, pageIndex, pageSize);

        var data = _mapper.Map<List<Entry>, List<EntryItem>>(dataFromDb);
        
        return new PaginatedItemsViewModel<EntryItem>(
            pageIndex,
            pageSize,
            totalItems,
            data);
    }

    public async Task<EntryItemDetailed> FindFirstEntryByFilterAsync(
        EntryRequest entryRequest)
    {
        var dataFromDb = await _entryRepository.FindFirstEntryByFilterAsync(
            entryRequest.Name,
            entryRequest.Version);
        return _mapper.Map<Entry, EntryItemDetailed>(dataFromDb);
    }

    public async Task<EntryItem> AddEntryToRepository(EntryRequest entryRequest)
    {
        var entryItem = new EntryItem
        {
            Name = entryRequest.Name,
            Version = entryRequest.Version,
            FileName = entryRequest.FileName,
            PlistFileName = entryRequest.PlistFileName
        };
        var entry = _mapper.Map<EntryItem, Entry>(entryItem);
        var entryIdDb = await _entryRepository.AddEntryAsync(entry);

        try
        {
            var command = new Command
            {
                Entry = entryIdDb,
                Status = "В процессе",
                EventDate = DateTime.UtcNow
            };
            await _commandRepository.AddCommandAsync(command);
            
            var message = new AddEntryCommand
            {
                EntryId = entryIdDb.Id,
                PackageName = entryIdDb.Name,
                PackageVersion = entryIdDb.Version,
                PackageFileName = entryIdDb.FileName,
                PlistFileName = entryIdDb.PlistFileName
            };
            await _kafkaSink.SendAsync(command.Id, message, CancellationToken.None);
        }
        catch (Exception e)
        {
            var command = new Command
            {
                Entry = entryIdDb,
                Status = "Ошибка при отправке",
                ErrorMessage = e.Message,
                EventDate = DateTime.UtcNow
            };
            await _commandRepository.AddCommandAsync(command);
        }
        return _mapper.Map<Entry, EntryItem>(entryIdDb);
    }

    public async Task AddCommandToEntry(string entryId, CommandStatus commandStatus, string commandErrorMessage)
    {
        var entryInDb = await _entryRepository.FindEntryById(entryId);
        var command = new Command
        {
            Entry = entryInDb,
            Status = commandStatus.GetAttribute<DisplayAttribute>()?.Name,
            ErrorMessage = commandErrorMessage,
            EventDate = DateTime.UtcNow
        };
        await _commandRepository.AddCommandAsync(command);
    }
}