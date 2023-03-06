using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CMS.API.Infrastructure.Services;
using CMS.API.Models.ViewModels;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CMS.API.Controllers;

[Route("api/v1/[controller]")]
//[Authorize]
[ApiController]
public class EntryController : Controller
{
    private readonly ILogger<EntryController> _logger;
    private readonly IEntryService _entryService;

    public EntryController(ILogger<EntryController> logger, IEntryService entryService)
    {
        _logger = logger;
        _entryService = entryService;
    }
    
    [HttpGet("list")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<object>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetEntryByFilter(
        [FromQuery] string entryName, 
        [FromQuery] string entryVersion, 
        [FromQuery] int pageIndex = 0, 
        [FromQuery] int pageSize = 10)
    {
        var error_message = string.Empty;
        // if (string.IsNullOrEmpty(entryName))
        //     error_message += $"Поле {nameof(entryName)} не может быть пустым. \r\n";
        // if (string.IsNullOrEmpty(entryVersion))
        //     error_message += $"Поле {nameof(entryVersion)} не может быть пустым. \r\n";
        if (pageSize <= 0)
            error_message += $"Поле {nameof(pageSize)} не может быть меньше или равно нулю. \r\n";
        if (pageIndex < 0)
            error_message += $"Поле {nameof(pageIndex)} не может быть меньше нуля. \r\n";

        if (error_message.Length > 0)
            return BadRequest(error_message);

        try
        {
            var model = await _entryService.FindEntriesListByFilter(entryName, entryVersion, pageIndex, pageSize);
            return Ok(model);
        }
        catch (ArgumentOutOfRangeException e)
        {
            return BadRequest(e);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }
    
    [HttpPut("add")]
    [ProducesResponseType(typeof(EntryItem), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> AddEntry(
        [FromBody] EntryRequest entryRequest)
    {
        var error_message = string.Empty;
        if (string.IsNullOrEmpty(entryRequest.Name))
            error_message += $"Поле {nameof(entryRequest.Name)} не может быть пустым. \r\n";
        if (string.IsNullOrEmpty(entryRequest.Version))
            error_message += $"Поле {nameof(entryRequest.Version)} не может быть пустым. \r\n";
        if (string.IsNullOrEmpty(entryRequest.FileName))
             error_message += $"Поле {nameof(entryRequest.FileName)} не может быть пустым. \r\n";
        if (string.IsNullOrEmpty(entryRequest.PlistFileName))
            error_message += $"Поле {nameof(entryRequest.PlistFileName)} не может быть пустым. \r\n";
        
        var entryInDb = await _entryService.FindFirstEntryByFilterAsync(entryRequest);
        if (entryInDb != null)
            error_message += $"Пакет {entryRequest.Name} версии {entryRequest.Version} уже существует. \r\n";

        if (error_message.Length > 0)
            return BadRequest(error_message);

        try
        {
            var model = await _entryService.AddEntryToRepository(entryRequest);
            return Ok(model);
        }
        catch (ArgumentOutOfRangeException e)
        {
            return BadRequest(e);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }
}