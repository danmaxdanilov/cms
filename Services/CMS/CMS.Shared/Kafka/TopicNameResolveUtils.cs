﻿using System.Diagnostics.CodeAnalysis;

namespace CMS.Shared.Kafka;

public static class TopicNameResolveUtils
{
    public static string ResolveName<TMessageValue>() where TMessageValue : BusRecordBase
    {
        return $"{typeof(TMessageValue).Name}Topic";
    }

    public static string ResolveName([NotNull] Type type)
    {
        return $"{type.Name}Topic";
    }
}