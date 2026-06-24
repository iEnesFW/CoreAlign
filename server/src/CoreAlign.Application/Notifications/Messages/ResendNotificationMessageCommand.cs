using CoreAlign.Application.Notifications.Delivery;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Notifications.Messages;

public sealed record ResendNotificationMessageCommand(Guid MessageId) : IRequest<Unit>;

public sealed class ResendNotificationMessageHandler : IRequestHandler<ResendNotificationMessageCommand, Unit>
{
    private readonly ITenantContext _tenantContext;
    private readonly INotificationMessageRepository _messages;
    private readonly INotificationDeliveryQueue _deliveryQueue;
    private readonly IUnitOfWork _unitOfWork;

    public ResendNotificationMessageHandler(
        ITenantContext tenantContext,
        INotificationMessageRepository messages,
        INotificationDeliveryQueue deliveryQueue,
        IUnitOfWork unitOfWork)
    {
        _tenantContext = tenantContext;
        _messages = messages;
        _deliveryQueue = deliveryQueue;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ResendNotificationMessageCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var message = await _messages.GetByIdAsync(tenantId, request.MessageId, cancellationToken)
            ?? throw new NotificationMessageNotFoundException(request.MessageId);

        if (message.Channel == NotificationChannel.InApp)
        {
            return Unit.Value;
        }

        var utcNow = DateTime.UtcNow;
        message.MarkQueued(utcNow);
        await _messages.UpsertAsync(message, cancellationToken);

        await _deliveryQueue.EnqueueChannelSendAsync(
            new NotificationChannelSendPayload(
                tenantId,
                message.Id,
                message.Channel,
                message.RecipientAddress,
                message.Subject,
                message.BodyMarkdown,
                message.BodyMarkdown),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
