using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Manufacturing;

public class ProductionJobLog : TenantEntity
{
    public Guid ProductionJobId { get; private set; }
    public Guid ProductionJobStepId { get; private set; }
    public Guid OperatorId { get; private set; }
    public string EventType { get; private set; } = string.Empty; // Start, Pause, Resume, Finish, Scrap
    public DateTime EventTimeUtc { get; private set; }
    public string? Reason { get; private set; }
    public decimal? Quantity { get; private set; }
    public int? DurationMinutes { get; private set; }

    protected ProductionJobLog() { }

    public ProductionJobLog(Guid jobId, Guid stepId, Guid operatorId, string eventType, DateTime eventTimeUtc, string? reason = null, decimal? quantity = null, int? durationMinutes = null)
    {
        ProductionJobId = jobId;
        ProductionJobStepId = stepId;
        OperatorId = operatorId;
        EventType = eventType;
        EventTimeUtc = eventTimeUtc;
        Reason = reason;
        Quantity = quantity;
        DurationMinutes = durationMinutes;
    }
}
