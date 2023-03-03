namespace CMS.Shared.Kafka.Commands;

public interface ICommandHandler<TMessageKey, TMessageValue>
    where TMessageValue : IntegrationCommand
{
    Task HandleAsync(Tuple<TMessageKey, TMessageValue> record);
}