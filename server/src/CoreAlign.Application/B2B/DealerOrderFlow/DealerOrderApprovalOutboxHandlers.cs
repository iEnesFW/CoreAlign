using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.B2B.DealerOrderFlow;

public sealed class DealerOrderSubmittedForApprovalOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => DealerOrderApprovalOutbox.SubmittedForApprovalMessageType;
    public const string NotificationType = "DealerOrderPendingApproval";

    private readonly INotificationRepository _notifications;
    private readonly ICustomerUserRepository _customerUsers;
    private readonly IUserRepository _users;
    private readonly IEmailService _emailService;

    public DealerOrderSubmittedForApprovalOutboxHandler(
        INotificationRepository notifications,
        ICustomerUserRepository customerUsers,
        IUserRepository users,
        IEmailService emailService)
    {
        _notifications = notifications;
        _customerUsers = customerUsers;
        _users = users;
        _emailService = emailService;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        DealerOrderSubmittedForApprovalPayload? payload;
        try
        {
            payload = DealerOrderApprovalOutbox.Deserialize<DealerOrderSubmittedForApprovalPayload>(payloadJson);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        var members = await _customerUsers.ListByCustomerAsync(payload.CustomerId, cancellationToken);
        var active = members.Where(m => m.Status == MembershipStatus.Active).ToList();
        if (active.Count == 0) return OutboxHandlerResult.Processed("NoRecipients");

        var title = $"Bayi onay isteği: {payload.DealerName}";
        var body = $"{payload.LineCount} satır · {FormatTotal(payload.Total, payload.Currency)} — Müşteri onayınızı bekliyor.";

        var created = 0;
        foreach (var member in active)
        {
            if (await _notifications.ExistsForRecipientAsync(member.UserId, "Order", payload.OrderId, NotificationType, cancellationToken))
            {
                continue;
            }
            var notification = new Notification(
                recipientUserId: member.UserId,
                actorUserId: payload.DealerUserId,
                type: NotificationType,
                entityType: "Order",
                entityId: payload.OrderId,
                title: title,
                body: body);
            await _notifications.AddIfNotExistsAsync(notification, cancellationToken);

            var user = await _users.GetByIdAsync(member.UserId, cancellationToken);
            if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailService.SendDealerOrderPendingApprovalAsync(
                    user.Email,
                    payload.DealerName,
                    payload.LineCount,
                    payload.Total,
                    payload.Currency,
                    cancellationToken);
            }

            created++;
        }

        if (created == 0) return OutboxHandlerResult.Processed("AlreadyProcessed");

        return OutboxHandlerResult.Processed($"FannedOut:{created}");
    }

    private static string FormatTotal(decimal total, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return total.ToString("0.##");
        return $"{total:0.##} {currency}";
    }
}

public sealed class DealerOrderApprovedByCustomerOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => DealerOrderApprovalOutbox.ApprovedByCustomerMessageType;
    public const string NotificationType = "DealerOrderApproved";

    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public DealerOrderApprovedByCustomerOutboxHandler(
        INotificationRepository notifications,
        IUserRepository users,
        IRoleRepository roles)
    {
        _notifications = notifications;
        _users = users;
        _roles = roles;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        DealerOrderApprovedByCustomerPayload? payload;
        try
        {
            payload = DealerOrderApprovalOutbox.Deserialize<DealerOrderApprovedByCustomerPayload>(payloadJson);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        var recipients = new HashSet<Guid>();

        if (payload.DealerUserId is Guid duid)
        {
            recipients.Add(duid);
        }

        var adminRole = await _roles.GetByNameAsync("TenantAdmin", cancellationToken);
        if (adminRole is not null)
        {
            var tenantUsers = await _users.ListByTenantAsync(payload.TenantId, cancellationToken);
            foreach (var user in tenantUsers.Where(u => u.IsActive))
            {
                if (user.UserRoles.Any(ur => ur.RoleId == adminRole.Id))
                {
                    recipients.Add(user.Id);
                }
            }
        }

        if (recipients.Count == 0) return OutboxHandlerResult.Processed("NoRecipients");

        var title = $"{payload.CustomerName}: bayi siparişi onaylandı";
        var body = $"{payload.DealerName} bayisinin {payload.LineCount} satırlık {FormatTotal(payload.Total, payload.Currency)} siparişi müşteri tarafından onaylandı.";

        var created = 0;
        foreach (var recipient in recipients)
        {
            if (await _notifications.ExistsForRecipientAsync(recipient, "Order", payload.OrderId, NotificationType, cancellationToken))
            {
                continue;
            }
            var notification = new Notification(
                recipientUserId: recipient,
                actorUserId: payload.ApprovedByUserId,
                type: NotificationType,
                entityType: "Order",
                entityId: payload.OrderId,
                title: title,
                body: body);
            await _notifications.AddIfNotExistsAsync(notification, cancellationToken);
            created++;
        }

        if (created == 0) return OutboxHandlerResult.Processed("AlreadyProcessed");

        return OutboxHandlerResult.Processed($"FannedOut:{created}");
    }

    private static string FormatTotal(decimal total, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return total.ToString("0.##");
        return $"{total:0.##} {currency}";
    }
}

public sealed class DealerOrderRejectedByCustomerOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => DealerOrderApprovalOutbox.RejectedByCustomerMessageType;
    public const string NotificationType = "DealerOrderRejected";

    private readonly INotificationRepository _notifications;

    public DealerOrderRejectedByCustomerOutboxHandler(
        INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        DealerOrderRejectedByCustomerPayload? payload;
        try
        {
            payload = DealerOrderApprovalOutbox.Deserialize<DealerOrderRejectedByCustomerPayload>(payloadJson);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        if (payload.DealerUserId is not Guid duid) return OutboxHandlerResult.Processed("NoRecipient");

        if (await _notifications.ExistsForRecipientAsync(duid, "Order", payload.OrderId, NotificationType, cancellationToken))
        {
            return OutboxHandlerResult.Processed("AlreadyProcessed");
        }

        var title = $"{payload.CustomerName}: bayi siparişi reddedildi";
        var body = string.IsNullOrWhiteSpace(payload.Reason)
            ? "Müşteri sipariş talebinizi reddetti."
            : $"Müşteri sipariş talebinizi reddetti. Sebep: {payload.Reason}";

        var notification = new Notification(
            recipientUserId: duid,
            actorUserId: payload.RejectedByUserId,
            type: NotificationType,
            entityType: "Order",
            entityId: payload.OrderId,
            title: title,
            body: body);

        await _notifications.AddIfNotExistsAsync(notification, cancellationToken);

        return OutboxHandlerResult.Processed("FannedOut:1");
    }
}
