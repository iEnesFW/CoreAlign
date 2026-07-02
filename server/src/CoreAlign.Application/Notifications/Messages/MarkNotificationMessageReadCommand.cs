using CoreAlign.Application.Common;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Notifications.Messages;

public sealed record MarkNotificationMessageReadCommand(Guid MessageId, Guid CurrentUserId)
    : IRequest<Unit>, ITransactionalRequest;

public sealed class MarkNotificationMessageReadHandler
    : IRequestHandler<MarkNotificationMessageReadCommand, Unit>
{
    private readonly ITenantContext _tenantContext;
    private readonly INotificationMessageRepository _messages;

    public MarkNotificationMessageReadHandler(
        ITenantContext tenantContext,
        INotificationMessageRepository messages)
    {
        _tenantContext = tenantContext;
        _messages = messages;
    }

    public async Task<Unit> Handle(MarkNotificationMessageReadCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var message = await _messages.GetByIdAsync(tenantId, request.MessageId, cancellationToken)
            ?? throw new NotificationMessageNotFoundException(request.MessageId);

        if (message.UserId != request.CurrentUserId)
            throw new NotificationMessageAccessForbiddenException(request.MessageId);

        message.MarkRead(DateTime.UtcNow);
        return Unit.Value;
    }
}
