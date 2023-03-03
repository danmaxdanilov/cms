using System.Collections.Generic;
using System.Threading.Tasks;
using CMS.API.Models.DomainModels;

namespace CMS.API.Infrastructure.Repositories;

public interface ICommandRepository
{
    Task<Command> AddCommandAsync(Command command);
}

public class CommandRepository : ICommandRepository
{
    private readonly PgDbContext _context;

    public CommandRepository(PgDbContext context)
    {
        _context = context;
    }

    public async Task<Command> AddCommandAsync(Command command)
    {
        await _context.AddAsync(command);
        await _context.SaveChangesAsync();
        return command;
    }

    public async Task UpdateCommandAsync(Command command)
    {
        var oldCommand = await _context.Commands.FindAsync(command);
        if (oldCommand == null)
            throw new KeyNotFoundException(string.Format("Command #{0} not found. Nothing to update", command.Id));
        oldCommand.Status = command.Status;
        oldCommand.ErrorMessage = command.ErrorMessage;
        oldCommand.EventDate = command.EventDate;
        _context.Update(oldCommand);
        await _context.SaveChangesAsync();
    }
}