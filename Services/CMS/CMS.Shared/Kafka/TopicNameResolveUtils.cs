using System.Diagnostics.CodeAnalysis;
using CMS.Shared.Kafka.Events;

namespace CMS.Shared.Kafka;

public static class TopicNameResolveUtils
{
    public static string ResolveName<TMessageValue>() where TMessageValue : IntegrationEvent
    {
        return $"{typeof(TMessageValue).Name}Topic";
    }

    public static string ResolveName([NotNull] Type type)
    {
        return $"{type.Name}Topic";
    }
}