using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.B2B.PortalComments;

public sealed class OrderCommentPostedOutboxHandler : IOutboxMessageHandler
{
    public const string NotificationType = "OrderCommentPosted";

    public string MessageType => OrderCommentPostedOutbox.MessageType;

    private readonly INotificationRepository _notifications;
    private readonly ICustomerUserRepository _customerUsers;
    private readonly IDealerUserRepository _dealerUsers;
    private readonly IUserRepository _users;
    private readonly IEmailService _emailService;

    public OrderCommentPostedOutboxHandler(
        INotificationRepository notifications,
        ICustomerUserRepository customerUsers,
        IDealerUserRepository dealerUsers,
        IUserRepository users,
        IEmailService emailService)
    {
        _notifications = notifications;
        _customerUsers = customerUsers;
        _dealerUsers = dealerUsers;
        _users = users;
        _emailService = emailService;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        OrderCommentPostedPayload? payload;
        try
        {
            payload = OrderCommentPostedOutbox.Deserialize(payloadJson);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        var recipients = await ResolveRecipientsAsync(payload, cancellationToken);
        if (recipients.Count == 0) return OutboxHandlerResult.Processed("NoRecipients");

        var title = payload.AuthorPersona switch
        {
            "customer" => "Sipariş üzerinde müşteri yorumu",
            "dealer" => "Sipariş üzerinde bayi yorumu",
            _ => "Sipariş üzerinde yeni yorum",
        };
        var body = string.IsNullOrWhiteSpace(payload.Excerpt) ? "Yeni bir yorum eklendi." : payload.Excerpt;

        var created = 0;
        foreach (var recipient in recipients)
        {
            if (recipient == payload.AuthorUserId) continue;
            if (await _notifications.ExistsForRecipientAsync(recipient, "Order", payload.OrderId, NotificationType, cancellationToken))
            {
                continue;
            }
            var notification = new Notification(
                recipientUserId: recipient,
                actorUserId: payload.AuthorUserId,
                type: NotificationType,
                entityType: "Order",
                entityId: payload.OrderId,
                title: title,
                body: body);
            await _notifications.AddIfNotExistsAsync(notification, cancellationToken);

            var user = await _users.GetByIdAsync(recipient, cancellationToken);
            if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailService.SendOrderCommentPostedAsync(user.Email, payload.AuthorPersona, body, cancellationToken);
            }

            created++;
        }

        if (created == 0) return OutboxHandlerResult.Processed("AlreadyProcessed");
        return OutboxHandlerResult.Processed($"FannedOut:{created}");
    }

    private async Task<HashSet<Guid>> ResolveRecipientsAsync(OrderCommentPostedPayload payload, CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid>();

        if (string.Equals(payload.AuthorPersona, "customer", StringComparison.OrdinalIgnoreCase))
        {
            if (payload.OriginDealerAccountId is not Guid dealerId) return recipients;
            var dealerMembers = await _dealerUsers.ListByDealerAsync(dealerId, cancellationToken);
            foreach (var member in dealerMembers.Where(m => m.Status == MembershipStatus.Active))
            {
                recipients.Add(member.UserId);
            }
            return recipients;
        }

        if (string.Equals(payload.AuthorPersona, "dealer", StringComparison.OrdinalIgnoreCase))
        {
            var customerMembers = await _customerUsers.ListByCustomerAsync(payload.CustomerId, cancellationToken);
            foreach (var member in customerMembers.Where(m => m.Status == MembershipStatus.Active))
            {
                recipients.Add(member.UserId);
            }
            return recipients;
        }

        return recipients;
    }
}
