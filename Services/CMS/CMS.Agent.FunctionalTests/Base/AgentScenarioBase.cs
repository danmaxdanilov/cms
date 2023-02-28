using System.Reflection;
using CMS.Agent.Repositories;
using CMS.Agent.Services;
using CMS.Agent.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Agent.FunctionalTests.Base;

public class AgentScenarioBase
{
    public TestServer CreateServer()
    {
        var path = Assembly.GetAssembly(typeof(AgentScenarioBase))
            ?.Location;

        var configuration = GetConfiguration();
        
        IConfiguration GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        var hostBuilder = new WebHostBuilder()
            .UseContentRoot(Path.GetDirectoryName(path))
            //.UseSerilog()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration(cb =>
            {
                cb.AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((_, services) =>
            {
                services.Configure<AgentSettings>(configuration);
                
                var dbContextOptionsBuilder = new DbContextOptionsBuilder<LiteDataContext>()
                    .UseInMemoryDatabase("LiteDatabase")
                    .Options;
                services.AddSingleton<LiteDataContext>(
                    provider => new LiteDataContext(dbContextOptionsBuilder));

                //services.AddSingleton<IContextFactory<LiteDataContext>>(
                //    provider => new LiteDataContextFactory(configuration["SqLiteConnectionString"]));

                services.AddTransient<IFileRepository, FileRepository>();
                services.AddTransient<IEntryRepository, EntryRepository>();

                services.AddTransient<IEntrySevice, EntrySevice>();
            })
            .UseStartup<AgentStartup>();;
        
        return new TestServer(hostBuilder);
    }
}