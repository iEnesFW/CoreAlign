using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Notifications.Outbox;

public sealed class NotificationDispatchOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeName = "NotificationDispatch";

    public string MessageType => MessageTypeName;

    private readonly INotificationDispatcher _dispatcher;

    public NotificationDispatchOutboxHandler(INotificationDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        NotificationDispatchPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<NotificationDispatchPayload>(payloadJson);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }

        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        try
        {
            var channels = payload.Channels is { Count: > 0 } ? payload.Channels : null;
            var request = new NotificationRequest(
                payload.TenantId,
                payload.UserId,
                payload.CustomerId,
                payload.CategoryKey,
                payload.TemplateKey,
                payload.Locale,
                payload.Payload ?? new Dictionary<string, object?>(),
                channels);
            await _dispatcher.DispatchAsync(request, cancellationToken);
            return OutboxHandlerResult.Processed("Dispatched");
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed(ex.Message);
        }
    }
}

public sealed class NotificationDispatchPayload
{
    public Guid TenantId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? CustomerId { get; init; }
    public string CategoryKey { get; init; } = string.Empty;
    public string TemplateKey { get; init; } = string.Empty;
    public string Locale { get; init; } = "en";
    public Dictionary<string, object?>? Payload { get; init; }
    public IReadOnlyList<NotificationChannel>? Channels { get; init; }
}
