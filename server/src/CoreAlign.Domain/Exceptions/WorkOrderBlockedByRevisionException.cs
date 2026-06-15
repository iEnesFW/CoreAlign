namespace CoreAlign.Domain.Exceptions;

public class WorkOrderBlockedByRevisionException : DomainException
{
    public Guid WorkOrderId { get; }

    public WorkOrderBlockedByRevisionException(Guid workOrderId)
        : base("GlassEnclosure.WorkOrder.BlockedByRevision")
    {
        WorkOrderId = workOrderId;
    }
}
