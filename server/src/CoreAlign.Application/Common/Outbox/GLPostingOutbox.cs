using System.Text.Json;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Common.Outbox;

/// <summary>
/// Enqueues a GL posting onto the transactional outbox. The message is appended
/// to the ambient unit of work, so it commits atomically with the business
/// change and is drained only after that commit succeeds.
/// </summary>
public interface IGLPostingOutbox
{
    Task EnqueueAsync(GLPostingRequest request, CancellationToken cancellationToken = default);
}

public sealed class GLPostingOutbox : IGLPostingOutbox
{
    public const string MessageType = "GLPosting";

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public GLPostingOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueAsync(GLPostingRequest request, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(request, OutboxJson.Options);
        await _outbox.AddAsync(new OutboxMessage(MessageType, payload), cancellationToken);
        _signal.MarkPending();
    }
}

/// <summary>
/// Outbox dispatcher for GL postings. Deserializes the payload and delegates to
/// <see cref="IGLPostingService"/>; classifies the result so the processor can
/// mark the message Processed/Deferred/Failed appropriately.
/// </summary>
public sealed class GLPostingOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => GLPostingOutbox.MessageType;

    private readonly IGLPostingService _gl;

    public GLPostingOutboxHandler(IGLPostingService gl)
    {
        _gl = gl;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        GLPostingRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<GLPostingRequest>(payloadJson, OutboxJson.Options);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }
        if (request is null)
        {
            return OutboxHandlerResult.Failed("Payload deserialized to null.");
        }

        var result = await _gl.PostAsync(request, cancellationToken);
        return result switch
        {
            GLPostingResult.SkippedClosedPeriod or GLPostingResult.SkippedUnmapped
                => OutboxHandlerResult.Deferred(result.ToString()),
            _ => OutboxHandlerResult.Processed(result.ToString()),
        };
    }
}
