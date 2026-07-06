using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Common.Email;

public sealed record EmailQueuedPayload(
    string To,
    string TemplateCode,
    string Locale,
    Guid TenantId,
    string? ReplyTo,
    Dictionary<string, object?> Context,
    EmailAttachmentPayload? Attachment = null);

public sealed record EmailAttachmentPayload(
    string FileName,
    string ContentType,
    string ContentBase64);

public interface IEmailQueuedOutbox
{
    Task EnqueueAsync(EmailQueuedPayload payload, CancellationToken cancellationToken = default);
}

public sealed class EmailQueuedOutbox : IEmailQueuedOutbox
{
    public const string MessageType = "EmailQueued";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public EmailQueuedOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueAsync(EmailQueuedPayload payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payload.To))
        {
            return;
        }
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(MessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static EmailQueuedPayload? Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<EmailQueuedPayload>(payloadJson, JsonOptions);
}
