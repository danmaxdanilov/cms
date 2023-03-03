using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.API.Infrastructure.Repositories;
using CMS.API.Models.DomainModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CMS.FunctionalTests.Base;

public static class PgDbContextSeed
{
    public static async Task SeedDatabaseAsync(PgDbContext context, ILogger<PgDbContext> logger)
    {
        var policy = CreatePolicy(logger, nameof(PgDbContext));

        await policy.ExecuteAsync(async () =>
        {
            await context.Database.MigrateAsync();
            
            if (!context.Entries.Any())
            {
                var entries = GenerateEntries();
                await context.AddRangeAsync(entries);
                await context.SaveChangesAsync();
            }
            
            if (!context.Commands.Any())
            {
                var entries = await context.Entries.ToListAsync();
                foreach (var entry in entries)
                {
                    var commands = GenerateCommandsByEntry(entry);
                    await context.AddRangeAsync(commands);
                }
                await context.SaveChangesAsync();
            }
            
        });
    }

    private static List<Entry> GenerateEntries()
    {
        return new List<Entry>
        {
            new Entry
            {
                Name = "mc",
                Version = "1.27"
            },
            new Entry
            {
                Name = "task-sell",
                Version = "6.28"
            }
        };
    }

    private static List<Command> GenerateCommandsByEntry(Entry entry)
    {
        return new List<Command>
        {
            new Command
            { 
                Status = "Создан",
                EventDate = DateTime.UtcNow - TimeSpan.FromDays(1),
                Entry = entry
            },
            new Command
            { 
                Status = "Готово",
                EventDate = DateTime.UtcNow,
                Entry = entry
            },
        };
    }

    private static AsyncRetryPolicy CreatePolicy(ILogger<PgDbContext> logger, string prefix, int retries = 3)
    {
        return Policy.Handle<SqlException>().
            WaitAndRetryAsync(
                retryCount: retries,
                sleepDurationProvider: retry => TimeSpan.FromSeconds(5),
                onRetry: (exception, timeSpan, retry, ctx) =>
                {
                    logger.LogWarning(exception, "[{prefix}] Exception {ExceptionType} with message {Message} detected on attempt {retry} of {retries}", prefix, exception.GetType().Name, exception.Message, retry, retries);
                }
            );
    }
}