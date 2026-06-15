using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Events;

public record MrpSuggestionsCreatedEvent(
    Guid TenantId,
    int RequisitionCount,
    int LineCount,
    DateTime AsOfDate,
    IReadOnlyList<Guid> RequisitionIds,
    DateTime OccurredAtUtc) : IDomainEvent;
