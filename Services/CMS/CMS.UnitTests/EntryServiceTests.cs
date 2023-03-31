using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CMS.API.Infrastructure.Repositories;
using CMS.API.Infrastructure.Services;
using CMS.API.Models.DomainModels;
using CMS.API.Models.ViewModels;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Commands;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CMS.UnitTests;

public class EntryServiceTests
{
    private readonly Mock<ILogger<IEntryService>> _loggerMock;
    private readonly Mock<IEntryRepository> _entryRepositoryMock;
    private readonly Mock<ICommandRepository> _commandRepositoryMock;
    private readonly Mock<IKafkaSink<string, AddEntryCommand>> _kafkaAddEntryMock;
    private readonly Mock<IKafkaSink<string, RemoveEntryCommand>> _kafkaRemoveEntryMock;
    private readonly IMapper _mapper;

    public EntryServiceTests()
    {
        _loggerMock = new Mock<ILogger<IEntryService>>();
        _entryRepositoryMock = new Mock<IEntryRepository>();
        _commandRepositoryMock = new Mock<ICommandRepository>();
        _kafkaAddEntryMock = new Mock<IKafkaSink<string, AddEntryCommand>>();
        _kafkaRemoveEntryMock = new Mock<IKafkaSink<string, RemoveEntryCommand>>();
    }

    [Fact]
    public async Task RemoveEntry_Success()
    {
        //Arrange
        var fakeRemoveEntryRequest = new RemoveEntryRequest
        {
            EntryId = "fakeID",
            Reason = "fake reason to delete entry"
        };
        
        //Setup
        var fakeEntry = new Entry
        {
            Id = fakeRemoveEntryRequest.EntryId
        };
        _entryRepositoryMock
            .Setup(x => x.FindEntryById(It.IsAny<string>()))
            .Returns(Task.FromResult(fakeEntry));
        
        //Act
        var service = new EntryService(
            _entryRepositoryMock.Object,
            _loggerMock.Object,
            _mapper,
            _commandRepositoryMock.Object,
            _kafkaAddEntryMock.Object,
            _kafkaRemoveEntryMock.Object
        );

        var result = await service.RemoveEntryFromRepository(fakeRemoveEntryRequest);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(fakeRemoveEntryRequest.EntryId, result);
        _commandRepositoryMock
            .Verify(x => x.AddCommandAsync(
                It.Is<Command>(c => c.Status.Contains("В процессе удаления"))
                ), Times.Once());
        _commandRepositoryMock
            .Verify(x => x.AddCommandAsync(
                It.Is<Command>(c => c.Status.Contains("Ошибка"))
            ), Times.Never());
    }
    
    [Fact]
    public async Task RemoveEntry_Fail()
    {
        //Arrange
        var fakeRemoveEntryRequest = new RemoveEntryRequest
        {
            EntryId = "fakeID",
            Reason = "fake reason to delete entry"
        };
        
        //Setup
        var fakeEntry = new Entry
        {
            Id = fakeRemoveEntryRequest.EntryId
        };
        _entryRepositoryMock
            .Setup(x => x.FindEntryById(It.IsAny<string>()))
            .Returns(Task.FromResult(fakeEntry));

        _kafkaRemoveEntryMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(), It.IsAny<RemoveEntryCommand>(), CancellationToken.None))
            .ThrowsAsync(new Exception());
        
        //Act
        var service = new EntryService(
            _entryRepositoryMock.Object,
            _loggerMock.Object,
            _mapper,
            _commandRepositoryMock.Object,
            _kafkaAddEntryMock.Object,
            _kafkaRemoveEntryMock.Object
        );

        var result = await service.RemoveEntryFromRepository(fakeRemoveEntryRequest);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(fakeRemoveEntryRequest.EntryId, result);
        _commandRepositoryMock
            .Verify(x => x.AddCommandAsync(
                It.Is<Command>(c => c.Status.Contains("В процессе удаления"))
            ), Times.Once());
        _commandRepositoryMock
            .Verify(x => x.AddCommandAsync(
                It.Is<Command>(c => c.Status.Contains("Ошибка"))
            ), Times.Once());
    }
}