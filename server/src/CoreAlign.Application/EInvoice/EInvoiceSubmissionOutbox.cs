using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.EInvoice;

public record EInvoiceSubmissionRequestedPayload(
    Guid TenantId,
    Guid InvoiceId);

public interface IEInvoiceSubmissionOutbox
{
    Task EnqueueSubmissionAsync(EInvoiceSubmissionRequestedPayload payload, CancellationToken cancellationToken = default);
}

public sealed class EInvoiceSubmissionOutbox : IEInvoiceSubmissionOutbox
{
    public const string SubmissionMessageType = "EInvoiceSubmissionRequested";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public EInvoiceSubmissionOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueSubmissionAsync(EInvoiceSubmissionRequestedPayload payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(SubmissionMessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static T? Deserialize<T>(string payloadJson) where T : class =>
        JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
}
