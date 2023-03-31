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
        AddEntryRequest entryRequest);
    
    Task<EntryItemDetailed> FindFirstEntryByFilterAsync(
        RemoveEntryRequest entryRequest);

    Task<EntryItem> AddEntryToRepository(AddEntryRequest entryRequest);

    Task AddCommandToEntry(string entryId, CommandStatus commandStatus, string commandErrorMessage);

    Task<string> RemoveEntryFromRepository(RemoveEntryRequest entryRequest);
}

public class EntryService : IEntryService
{
    private readonly ILogger<IEntryService> _logger;
    private readonly IEntryRepository _entryRepository;
    private readonly ICommandRepository _commandRepository;
    private readonly IKafkaSink<string, AddEntryCommand> _kafkaAddEntry;
    private readonly IKafkaSink<string, RemoveEntryCommand> _kafkaRemoveEntry;
    private readonly IMapper _mapper;

    public EntryService(
        IEntryRepository repository, 
        ILogger<IEntryService> logger,
        IMapper mapper, 
        ICommandRepository commandRepository, 
        IKafkaSink<string, AddEntryCommand> kafkaAddEntry, 
        IKafkaSink<string, RemoveEntryCommand> kafkaRemoveEntry)
    {
        _entryRepository = repository;
        _logger = logger;
        _mapper = mapper;
        _commandRepository = commandRepository;
        _kafkaAddEntry = kafkaAddEntry;
        _kafkaRemoveEntry = kafkaRemoveEntry;
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
        AddEntryRequest entryRequest)
    {
        var dataFromDb = await _entryRepository.FindFirstEntryByFilterAsync(
            entryRequest.Name,
            entryRequest.Version);
        return _mapper.Map<Entry, EntryItemDetailed>(dataFromDb);
    }

    public async Task<EntryItemDetailed> FindFirstEntryByFilterAsync(RemoveEntryRequest entryRequest)
    {
        var dataFromDb = await _entryRepository.FindEntryById(
            entryRequest.EntryId);
        return _mapper.Map<Entry, EntryItemDetailed>(dataFromDb);
    }

    public async Task<EntryItem> AddEntryToRepository(AddEntryRequest entryRequest)
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
                Status = "В процессе добавления",
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
            await _kafkaAddEntry.SendAsync(command.Id, message, CancellationToken.None);
        }
        catch (Exception e)
        {
            var command = new Command
            {
                Entry = entryIdDb,
                Status = "Ошибка при отправке команды на добавление",
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

    public async Task<string> RemoveEntryFromRepository(RemoveEntryRequest entryRequest)
    {
        var entryIdDb = await _entryRepository.FindEntryById(entryRequest.EntryId);

        try
        {
            var command = new Command
            {
                Entry = entryIdDb,
                Status = "В процессе удаления",
                Comment = entryRequest.Reason,
                EventDate = DateTime.UtcNow
            };
            await _commandRepository.AddCommandAsync(command);
            
            var message = new RemoveEntryCommand
            {
                EntryId = entryIdDb.Id,
                Reason = entryRequest.Reason
            };
            await _kafkaRemoveEntry.SendAsync(command.Id, message, CancellationToken.None);
        }
        catch (Exception e)
        {
            var command = new Command
            {
                Entry = entryIdDb,
                Status = "Ошибка при отправке команды на удаление",
                ErrorMessage = e.Message,
                EventDate = DateTime.UtcNow
            };
            await _commandRepository.AddCommandAsync(command);
        }
        
        return entryIdDb.Id;
    }
}