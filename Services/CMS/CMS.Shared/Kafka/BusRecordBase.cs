namespace CMS.Shared.Kafka;

public abstract class BusRecordBase
{
    protected BusRecordBase(Guid correlationId, Guid traceId) : this(correlationId)
    {
        TraceId = traceId;
    }

    protected BusRecordBase(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public BusRecordBase()
    {
    }

    public Guid CorrelationId { get; set; }
    public Guid TraceId { get; set; }
}