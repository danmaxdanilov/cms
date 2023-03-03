using Autofac;
using CMS.Agent.CommandHandlers;
using CMS.Agent.Services;
using CMS.Shared.Domain;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Commands;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CMS.Agent.UnitTests;

public class AddEntryCommandHandlerUnitTests
{
     [Fact]
     public async Task HandleAsync_Success()
     {
         //Arrange
         var configuretionMock = new Mock<IConfiguration>();
         var loggerMock = new Mock<ILogger<AddEntryCommandHandler>>();
         var kafkaSinkMock = new Mock<IKafkaSink<string, AddEntryResponseCommand>>();
         var entryServiceMock = new Mock<IEntrySevice>();
         var adminClientMock = new Mock<IAdminClient>();
         
         //Act
         var handler = new AddEntryCommandHandler(
             configuretionMock.Object,
             loggerMock.Object,
             entryServiceMock.Object,
             kafkaSinkMock.Object,
             adminClientMock.Object
         );
    
         var fakeRecord = new Tuple<string, AddEntryCommand>
         (
             "simple-message-key",
             new AddEntryCommand
             {
                 EntryId = "#123"
             }
         );
         
         await handler.HandleAsync(fakeRecord);
    
         //Assert
         
         entryServiceMock.Verify(m => m.AddNewEntry(fakeRecord.Item2), Times.Once);
         kafkaSinkMock.Verify(m => 
             m.SendAsync(
                 fakeRecord.Item1, 
                 It.Is<AddEntryResponseCommand>(x => x.CommandStatus == CommandStatus.Done),
                 CancellationToken.None));
     }
     
     [Fact]
     public async Task HandleAsync_Error()
     {
         //Arrange
         var configuretionMock = new Mock<IConfiguration>();
         var loggerMock = new Mock<ILogger<AddEntryCommandHandler>>();
         var kafkaSinkMock = new Mock<IKafkaSink<string, AddEntryResponseCommand>>();
         var entryServiceMock = new Mock<IEntrySevice>();
         entryServiceMock
             .Setup(x => x.AddNewEntry(It.IsAny<AddEntryCommand>()))
             .Throws<FileNotFoundException>();
         var adminClientMock = new Mock<IAdminClient>();
         
         //Act
         var handler = new AddEntryCommandHandler(
             configuretionMock.Object,
             loggerMock.Object,
             entryServiceMock.Object,
             kafkaSinkMock.Object,
             adminClientMock.Object
         );
    
         var fakeRecord = new Tuple<string, AddEntryCommand>
         (
             "simple-message-key",
             new AddEntryCommand
             {
                 EntryId = "#123"
             }
         );
         
         await handler.HandleAsync(fakeRecord);
    
         //Assert
         
         entryServiceMock.Verify(m => m.AddNewEntry(fakeRecord.Item2), Times.Once);
         loggerMock.Verify(x => x.Log(
                 LogLevel.Critical,
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((v, t) => true),
                 It.IsAny<FileNotFoundException>(),
                 It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), 
             Times.Once
         );
         kafkaSinkMock.Verify(m => 
             m.SendAsync(
                 fakeRecord.Item1, 
                 It.Is<AddEntryResponseCommand>(x => x.CommandStatus == CommandStatus.Error),
                 CancellationToken.None));
     }
}