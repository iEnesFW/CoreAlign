using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Feedback.Notifications;

public sealed class FeedbackNotificationOutboxHandler : IOutboxMessageHandler
{
    private const string PlatformAdminRole = "PlatformAdmin";
    private const string TenantAdminRole = "TenantAdmin";

    public string MessageType => FeedbackNotificationOutbox.MessageType;

    private readonly INotificationDispatcher _dispatcher;
    private readonly IUserRepository _users;
    private readonly ITenantContext _tenant;
    private readonly ILogger<FeedbackNotificationOutboxHandler> _logger;

    public FeedbackNotificationOutboxHandler(
        INotificationDispatcher dispatcher,
        IUserRepository users,
        ITenantContext tenant,
        ILogger<FeedbackNotificationOutboxHandler> logger)
    {
        _dispatcher = dispatcher;
        _users = users;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        FeedbackNotificationPayload? payload;
        try
        {
            payload = FeedbackNotificationOutbox.Deserialize(payloadJson);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        return payload.Kind switch
        {
            FeedbackNotificationKinds.Created => await NotifyOwnersAsync(payload, cancellationToken),
            FeedbackNotificationKinds.StatusChanged => await NotifyReporterAsync(
                payload,
                FeedbackTemplateKeys.StatusChanged,
                StatusPayload(payload),
                cancellationToken),
            FeedbackNotificationKinds.CommentAdded => await NotifyReporterAsync(
                payload,
                FeedbackTemplateKeys.CommentAdded,
                CommentPayload(payload),
                cancellationToken),
            _ => OutboxHandlerResult.Failed($"Unknown feedback notification kind '{payload.Kind}'."),
        };
    }

    private static Dictionary<string, string> BasePayload(FeedbackNotificationPayload p) => new()
    {
        ["ticketId"] = p.TicketId.ToString("N"),
        ["title"] = p.Title,
        ["type"] = p.Type.ToString(),
        ["priority"] = p.Priority.ToString(),
        ["module"] = p.Module ?? string.Empty,
    };

    private static Dictionary<string, string> CreatedPayload(FeedbackNotificationPayload p)
    {
        var d = BasePayload(p);
        d["ticketTenantId"] = p.TenantId.ToString("N");
        return d;
    }

    private static Dictionary<string, string> StatusPayload(FeedbackNotificationPayload p)
    {
        var d = BasePayload(p);
        d["status"] = p.Status.ToString();
        // WHY: the revision discriminates Open→InProgress→Open→InProgress. Without it the second
        // pass hashes identically to the first and the dispatcher silently swallows it as a dupe.
        d["revision"] = p.StatusChangeCount.ToString();
        return d;
    }

    private static Dictionary<string, string> CommentPayload(FeedbackNotificationPayload p)
    {
        var d = BasePayload(p);
        d["commentId"] = p.CommentId?.ToString("N") ?? string.Empty;
        d["authorName"] = p.CommentAuthorName ?? string.Empty;
        return d;
    }

    private async Task<OutboxHandlerResult> NotifyOwnersAsync(
        FeedbackNotificationPayload payload,
        CancellationToken ct)
    {
        var recipients = await _users.ListByRoleAsync(PlatformAdminRole, ct);
        if (recipients.Count == 0)
        {
            // The platform-owner role may be unassigned; fall back to the ticket tenant's admins so a
            // new report always reaches a human instead of disappearing.
            _logger.LogWarning(
                "No PlatformAdmin user found for feedback {TicketId}; falling back to tenant admins.",
                payload.TicketId);
            recipients = await TenantAdminsAsync(payload.TenantId, ct);
        }
        if (recipients.Count == 0)
        {
            _logger.LogWarning("Feedback {TicketId} has no notifiable recipient.", payload.TicketId);
            return OutboxHandlerResult.Processed("NoRecipients");
        }

        var body = CreatedPayload(payload);
        var sent = 0;
        foreach (var user in recipients)
        {
            // WHY: file the message under the RECIPIENT's own tenant — the bell reads
            // notification_messages inside the reader's tenant scope, so a row stamped with the
            // ticket's tenant would be invisible to a platform admin who lives elsewhere.
            if (await DispatchAsync(user.TenantId, user.Id, FeedbackTemplateKeys.Created, body, ct))
            {
                sent += 1;
            }
        }
        return OutboxHandlerResult.Processed($"FannedOut:{sent}");
    }

    private async Task<OutboxHandlerResult> NotifyReporterAsync(
        FeedbackNotificationPayload payload,
        string templateKey,
        Dictionary<string, string> body,
        CancellationToken ct)
    {
        if (payload.CreatedByUserId is null)
        {
            _logger.LogWarning(
                "Feedback {TicketId} has no CreatedByUserId; cannot notify the reporter.",
                payload.TicketId);
            return OutboxHandlerResult.Processed("NoRecipients");
        }
        var ok = await DispatchAsync(
            payload.TenantId,
            payload.CreatedByUserId.Value,
            templateKey,
            body,
            ct);
        return OutboxHandlerResult.Processed(ok ? "Notified" : "DispatchFailed");
    }

    private async Task<IReadOnlyList<User>> TenantAdminsAsync(Guid tenantId, CancellationToken ct)
    {
        var users = await _users.ListByTenantAsync(tenantId, ct);
        return users
            .Where(u => u.IsActive && u.UserRoles.Any(r => r.Role != null && r.Role.Name == TenantAdminRole))
            .ToList();
    }

    private async Task<bool> DispatchAsync(
        Guid tenantId,
        Guid userId,
        string templateKey,
        Dictionary<string, string> body,
        CancellationToken ct)
    {
        try
        {
            // The drain has no ambient tenant, so the dispatcher's own dedup read would be filtered to
            // Guid.Empty and the filtered unique index would then throw 23505 on the insert.
            using var scope = _tenant.PushScope(tenantId);
            await _dispatcher.DispatchAsync(
                new NotificationRequest(
                    tenantId,
                    userId,
                    null,
                    FeedbackTemplateKeys.CategoryKey,
                    templateKey,
                    "tr",
                    body,
                    ChannelsOverride: [NotificationChannel.InApp]),
                ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Feedback notification '{TemplateKey}' failed for user {UserId}.",
                templateKey,
                userId);
            return false;
        }
    }
}
