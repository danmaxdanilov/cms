﻿using CMS.Agent.FunctionalTests.Base;
using CMS.Agent.Models.Domain;
using CMS.Agent.Repositories;
using CMS.Agent.Services;
using CMS.Agent.Utils;
using CMS.Shared.Kafka.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Agent.FunctionalTests;

public class AgentScenario : AgentScenarioBase
{
    [Fact]
    public async Task EntryRepository_AddEntry_Success()
    {
        using (var server = CreateServer())
        {
            var ctx = server.Host.Services.GetRequiredService<LiteDataContext>();
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.EnsureCreatedAsync();
            
            var repository = server.Host.Services.GetRequiredService<IEntryRepository>();

            var fakeEntry = new EntryPackage
            {
                Name = "mc",
                Version = "1.0",
                Path = "",
                PlistPath = ""
            };
            await repository.Add(fakeEntry);
            
            //Assert
            var result = ctx.EntryPackages.FirstOrDefault(x => x.Name == fakeEntry.Name);
            Assert.NotNull(result);
            Assert.Equal(fakeEntry.Version, result.Version);
            Assert.Equal(fakeEntry.Path, result.Path);
        }
    }
    
    [Fact]
    public async Task EntryService_AddNewEntry_Success()
    {
        using (var server = CreateServer())
        {
            var ctx = server.Host.Services.GetRequiredService<LiteDataContext>();
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.EnsureCreatedAsync();
            
            var repository = server.Host.Services.GetRequiredService<IFileRepository>();
            repository.ShutDownDirectories();
            repository.InitDirectories();

            await File.WriteAllTextAsync(Path.Combine("import", "mc.pkg"), "very interesting magic string");
            await File.WriteAllTextAsync(Path.Combine("import", "mc.plist"), "very interesting magic config string");

            var service = server.Host.Services.GetRequiredService<IEntrySevice>();

            var fakeEntry = new AddEntryCommand
            {
                PackageName = "mc",
                PackageVersion = "1.28",
                PackageFileName = "mc.pkg",
                PlistFileName = "mc.plist"
            };
            await service.AddNewEntry(fakeEntry);
            
            //Assert
            var result = await ctx.EntryPackages.FirstOrDefaultAsync(x => x.Name == fakeEntry.PackageName);
            Assert.NotNull(result);
            Assert.Equal(fakeEntry.PackageVersion, result.Version);
            
            var expectedPath = Path.Combine("repository","mc@1.28","mc.pkg");
            Assert.Equal(expectedPath, result.Path);
            Assert.True(File.Exists(expectedPath));
            
            var expectedPlistPath = Path.Combine("repository","mc@1.28","mc.plist");
            Assert.Equal(expectedPlistPath, result.PlistPath);
            Assert.True(File.Exists(expectedPlistPath));
            
            repository.ShutDownDirectories();
        }
    }
    
    [Fact]
    public async Task EntryService_RemoveNewEntry_Success()
    {
        throw new NotImplementedException();
    }
}