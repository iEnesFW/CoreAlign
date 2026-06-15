using CoreAlign.Domain.Common;

namespace CoreAlign.Application.Fx.Events;

public sealed record FxRatesUpdatedEvent(
    DateTime EffectiveDate,
    int RateCount,
    string Source,
    DateTime FetchedAtUtc,
    DateTime OccurredAtUtc) : IDomainEvent;
