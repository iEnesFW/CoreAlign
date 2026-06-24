using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Notifications;
using CoreAlign.Application.Notifications.Delivery;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Documents.Forwarding;

public sealed record ForwardDocumentContext(
    Guid TenantId,
    ForwardableDocumentType DocumentType,
    Guid DocumentId,
    string RecipientEmail,
    Guid IdempotencyKey,
    Guid? UserId,
    Guid? CustomerId,
    string ReplyToEmail,
    string ForwardedByName,
    DocumentResult Pdf);

public interface IForwardDocumentService
{
    Task EnsureWithinLimitAsync(Guid tenantId, Guid forwardingUserId, CancellationToken cancellationToken = default);
    Task<ForwardDocumentResult> ForwardAsync(ForwardDocumentContext context, CancellationToken cancellationToken = default);
}

public sealed class ForwardDocumentService : IForwardDocumentService
{
    private const string CategoryKey = "DocumentForward";
    private const string RateLimitProvider = "forward";
    private const string DefaultLocale = "tr";

    private readonly INotificationDispatcher _dispatcher;
    private readonly INotificationRateLimiter _rateLimiter;
    private readonly IAuditContext _audit;

    public ForwardDocumentService(
        INotificationDispatcher dispatcher,
        INotificationRateLimiter rateLimiter,
        IAuditContext audit)
    {
        _dispatcher = dispatcher;
        _rateLimiter = rateLimiter;
        _audit = audit;
    }

    public async Task EnsureWithinLimitAsync(Guid tenantId, Guid forwardingUserId, CancellationToken cancellationToken = default)
    {
        var decision = await _rateLimiter.TryAcquireAsync(tenantId, RateLimitProvider, forwardingUserId.ToString("N"), cancellationToken);
        if (!decision.Allowed)
        {
            throw new DocumentForwardRateLimitExceededException();
        }
    }

    public async Task<ForwardDocumentResult> ForwardAsync(ForwardDocumentContext context, CancellationToken cancellationToken = default)
    {
        var documentNumber = Path.GetFileNameWithoutExtension(context.Pdf.FileName);
        var templateKey = context.DocumentType switch
        {
            ForwardableDocumentType.Invoice => "DocumentForward.Invoice",
            ForwardableDocumentType.Order => "DocumentForward.Order",
            _ => throw new ArgumentOutOfRangeException(nameof(context)),
        };

        var payload = new Dictionary<string, object?>
        {
            ["documentNumber"] = documentNumber,
            ["forwardedByName"] = context.ForwardedByName,
            ["idempotencyKey"] = context.IdempotencyKey.ToString("N"),
        };

        var attachment = new EmailAttachment(context.Pdf.FileName, context.Pdf.ContentType, context.Pdf.Content);

        var request = new NotificationRequest(
            context.TenantId,
            context.UserId,
            context.CustomerId,
            CategoryKey,
            templateKey,
            DefaultLocale,
            payload,
            ChannelsOverride: new[] { NotificationChannel.Email },
            RecipientEmailOverride: context.RecipientEmail,
            ReplyToOverride: context.ReplyToEmail,
            Attachments: new[] { attachment });

        var results = await _dispatcher.DispatchAsync(request, cancellationToken);

        _audit.CaptureCustom(
            context.DocumentId,
            context.DocumentType.ToString(),
            "DocumentForwarded",
            $"recipient={context.RecipientEmail};document={documentNumber}");

        var queued = results.Any(r => r.Success);
        return new ForwardDocumentResult(queued, queued ? "Queued" : "Failed");
    }
}
