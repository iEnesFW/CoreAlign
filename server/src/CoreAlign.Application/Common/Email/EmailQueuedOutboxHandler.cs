using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Common.Email;

public sealed class EmailQueuedOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => EmailQueuedOutbox.MessageType;

    private readonly IEmailTemplateRepository _templates;
    private readonly IEmailRenderer _renderer;
    private readonly IEmailSender _sender;
    private readonly ILogger<EmailQueuedOutboxHandler> _logger;

    public EmailQueuedOutboxHandler(
        IEmailTemplateRepository templates,
        IEmailRenderer renderer,
        IEmailSender sender,
        ILogger<EmailQueuedOutboxHandler> logger)
    {
        _templates = templates;
        _renderer = renderer;
        _sender = sender;
        _logger = logger;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = EmailQueuedOutbox.Deserialize(payloadJson);
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");
        if (string.IsNullOrWhiteSpace(payload.To)) return OutboxHandlerResult.Processed("NoRecipient");

        var locale = string.IsNullOrWhiteSpace(payload.Locale) ? "tr-TR" : payload.Locale;
        var template = await _templates.GetByCodeAsync(payload.TemplateCode, locale, cancellationToken);
        if (template is null && !string.Equals(locale, "tr-TR", StringComparison.OrdinalIgnoreCase))
        {
            template = await _templates.GetByCodeAsync(payload.TemplateCode, "tr-TR", cancellationToken);
        }

        string subject;
        string body;
        if (template is null)
        {
            _logger.LogWarning(
                "EmailTemplate code={Code} locale={Locale} not found — falling back to plain rendering.",
                payload.TemplateCode, locale);
            subject = $"[{payload.TemplateCode}]";
            body = BuildFallbackBody(payload.TemplateCode, payload.Context);
        }
        else
        {
            var rendered = _renderer.Render(template.Subject, template.Body, payload.Context);
            subject = rendered.Subject;
            body = rendered.BodyHtml;
        }

        var message = new EmailMessage(
            To: payload.To,
            Subject: subject,
            BodyHtml: body,
            BodyText: null,
            ReplyTo: payload.ReplyTo,
            TenantId: payload.TenantId);

        await _sender.SendAsync(message, cancellationToken);
        return OutboxHandlerResult.Processed($"Sent:{payload.TemplateCode}");
    }

    private static string BuildFallbackBody(string templateCode, IReadOnlyDictionary<string, object?> context)
    {
        var lines = context
            .Where(kv => kv.Value is not null)
            .Select(kv => $"<li><strong>{kv.Key}</strong>: {kv.Value}</li>");
        return $"<p>Template <code>{templateCode}</code> was not configured.</p><ul>{string.Join(string.Empty, lines)}</ul>";
    }
}
