namespace CoreAlign.Domain.Common;

public interface IAuditableMutation
{
    Guid AggregateId { get; }
    string AggregateType { get; }
}
