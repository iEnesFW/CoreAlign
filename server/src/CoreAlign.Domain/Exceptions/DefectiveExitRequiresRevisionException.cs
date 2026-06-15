namespace CoreAlign.Domain.Exceptions;

public class DefectiveExitRequiresRevisionException : DomainException
{
    public Guid WorkOrderId { get; }

    public DefectiveExitRequiresRevisionException(Guid workOrderId)
        : base("GlassEnclosure.WorkOrder.DefectiveExitRequiresRevision")
    {
        WorkOrderId = workOrderId;
    }
}
