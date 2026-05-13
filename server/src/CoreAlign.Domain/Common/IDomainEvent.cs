using MediatR;

namespace CoreAlign.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredAtUtc { get; }
}
