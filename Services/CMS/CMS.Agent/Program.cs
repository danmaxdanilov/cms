﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using CMS.Agent;
using CMS.Agent.CommandHandlers;
using CMS.Agent.Repositories;
using CMS.Agent.Services;
using CMS.Agent.Utils;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

var configuration = GetConfiguration();

Log.Logger = CreateSerilogLogger(configuration);

try
{
    Log.Information("Configuring web host ({ApplicationContext})...", Program.AppName);
    var host = BuildHost(configuration, args);

    Log.Information("Starting web host ({ApplicationContext})...", Program.AppName);
    await host.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", Program.AppName);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

IHost BuildHost(IConfiguration configuration, string[] args) =>
    new HostBuilder()
        //.UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .UseSerilog()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<AgentSettings>(configuration);
            
            var connectionString = configuration["SqLiteConnectionString"];
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<LiteDataContext>()
                .UseSqlite(connectionString);
            services.AddSingleton<LiteDataContext>(
                provider => new LiteDataContext(dbContextOptionsBuilder.Options));
            
            //services.AddSingleton<IContextFactory<LiteDataContext>>(
            //    provider => new LiteDataContextFactory(configuration["SqLiteConnectionString"]));
            
            services.AddTransient<IFileRepository, FileRepository>();
            services.AddTransient<IEntryRepository, EntryRepository>();
            
            services.AddTransient<IEntrySevice, EntrySevice>();

            services.AddKafka(configuration["Kafka:BootstrapServers"], configuration["Kafka:GroupId"]);
            services.AddKafkaProducer<string, AddEntryResponseCommand>();

            services.AddHostedService<AddEntryCommandHandler>();
        })
        .Build();

Serilog.ILogger CreateSerilogLogger(IConfiguration configuration)
{
    var logstashUrl = configuration["Serilog:LogstashgUrl"];
    return new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .Enrich.WithProperty("ApplicationContext", Program.AppName)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Http(string.IsNullOrWhiteSpace(logstashUrl) ? "http://logstash:8080" : logstashUrl, null)
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
}

IConfiguration GetConfiguration()
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    return builder.Build();
}

public partial class Program
{
    public static string Namespace = typeof(AgentSettings).Namespace;
    public static string AppName = Namespace.Substring(Namespace.LastIndexOf('.', Namespace.LastIndexOf('.') - 1) + 1);
}