namespace CoreAlign.Domain.Exceptions;

public class ConcurrentRevisionInsertException : ConflictException
{
    public Guid WorkOrderId { get; }
    public int AttemptedRevisionNumber { get; }

    public ConcurrentRevisionInsertException(Guid workOrderId, int attemptedRevisionNumber)
        : base($"Concurrent revision insert detected for work order {workOrderId} at revision {attemptedRevisionNumber}.")
    {
        WorkOrderId = workOrderId;
        AttemptedRevisionNumber = attemptedRevisionNumber;
    }
}
