namespace CoreAlign.Domain.Exceptions;

public class GlassWorkOrderNotFoundException : NotFoundException
{
    public Guid WorkOrderId { get; }

    public GlassWorkOrderNotFoundException(Guid workOrderId)
        : base($"Glass work order '{workOrderId}' not found.")
    {
        WorkOrderId = workOrderId;
    }
}

public class GlassWorkOrderRevisionNotFoundException : NotFoundException
{
    public Guid RevisionId { get; }

    public GlassWorkOrderRevisionNotFoundException(Guid revisionId)
        : base($"Glass work order revision '{revisionId}' not found.")
    {
        RevisionId = revisionId;
    }
}

public class GlassWorkOrderRevisionMismatchException : ConflictException
{
    public Guid RevisionId { get; }
    public Guid ExpectedWorkOrderId { get; }
    public Guid ActualWorkOrderId { get; }

    public GlassWorkOrderRevisionMismatchException(Guid revisionId, Guid expectedWorkOrderId, Guid actualWorkOrderId)
        : base($"Revision '{revisionId}' does not belong to work order '{expectedWorkOrderId}'.")
    {
        RevisionId = revisionId;
        ExpectedWorkOrderId = expectedWorkOrderId;
        ActualWorkOrderId = actualWorkOrderId;
    }
}
