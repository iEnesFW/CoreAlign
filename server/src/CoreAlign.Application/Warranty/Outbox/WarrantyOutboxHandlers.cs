using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Warranty.Outbox;

public sealed class WarrantyActivatedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "WarrantyActivated";
    public string MessageType => MessageTypeKey;
    private readonly ILogger<WarrantyActivatedOutboxHandler> _logger;
    public WarrantyActivatedOutboxHandler(ILogger<WarrantyActivatedOutboxHandler> logger) => _logger = logger;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        WarrantyActivatedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WarrantyActivatedEvent>(payloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex) { return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}")); }
        if (payload is null) return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));

        _logger.LogInformation(
            "Warranty contract {ContractId} ({Number}) activated for tenant {TenantId}, customer {CustomerId}, valid {Start:o} -> {End:o}.",
            payload.WarrantyContractId, payload.Number, payload.TenantId, payload.CustomerId, payload.StartDate, payload.EndDate);
        return Task.FromResult(OutboxHandlerResult.Processed($"Activated:{payload.Number}"));
    }
}

public sealed class WarrantyExpiredOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "WarrantyExpired";
    public string MessageType => MessageTypeKey;
    private readonly ILogger<WarrantyExpiredOutboxHandler> _logger;
    public WarrantyExpiredOutboxHandler(ILogger<WarrantyExpiredOutboxHandler> logger) => _logger = logger;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        WarrantyExpiredEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WarrantyExpiredEvent>(payloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex) { return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}")); }
        if (payload is null) return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));

        _logger.LogInformation(
            "Warranty contract {ContractId} ({Number}) expired for tenant {TenantId} on {EndDate:o}.",
            payload.WarrantyContractId, payload.Number, payload.TenantId, payload.EndDate);
        return Task.FromResult(OutboxHandlerResult.Processed($"Expired:{payload.Number}"));
    }
}

public sealed class WarrantyExpiringNotificationOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "WarrantyExpiringSoon";
    public string MessageType => MessageTypeKey;
    private readonly ILogger<WarrantyExpiringNotificationOutboxHandler> _logger;
    public WarrantyExpiringNotificationOutboxHandler(ILogger<WarrantyExpiringNotificationOutboxHandler> logger) => _logger = logger;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        WarrantyExpiringSoonEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WarrantyExpiringSoonEvent>(payloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex) { return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}")); }
        if (payload is null) return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));

        _logger.LogInformation(
            "Warranty {ContractId} ({Number}) expires in {DaysRemaining} days for tenant {TenantId}, customer {CustomerId}.",
            payload.WarrantyContractId, payload.Number, payload.DaysRemaining, payload.TenantId, payload.CustomerId);
        return Task.FromResult(OutboxHandlerResult.Processed($"ExpiringSoon:{payload.Number}:{payload.DaysRemaining}d"));
    }
}

public sealed class ServiceTicketOpenedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "ServiceTicketOpened";
    public string MessageType => MessageTypeKey;
    private readonly ILogger<ServiceTicketOpenedOutboxHandler> _logger;
    public ServiceTicketOpenedOutboxHandler(ILogger<ServiceTicketOpenedOutboxHandler> logger) => _logger = logger;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        ServiceTicketOpenedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ServiceTicketOpenedEvent>(payloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex) { return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}")); }
        if (payload is null) return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));

        _logger.LogInformation(
            "Service ticket {TicketId} opened for tenant {TenantId}, customer {CustomerId}, priority {Priority}, under warranty: {IsUnderWarranty}.",
            payload.ServiceTicketId, payload.TenantId, payload.CustomerId, payload.Priority, payload.IsUnderWarranty);
        return Task.FromResult(OutboxHandlerResult.Processed($"Opened:{payload.ServiceTicketId}"));
    }
}

public sealed class ServiceTicketResolvedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "ServiceTicketResolved";
    public string MessageType => MessageTypeKey;
    private readonly ILogger<ServiceTicketResolvedOutboxHandler> _logger;
    public ServiceTicketResolvedOutboxHandler(ILogger<ServiceTicketResolvedOutboxHandler> logger) => _logger = logger;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        ServiceTicketResolvedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ServiceTicketResolvedEvent>(payloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex) { return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}")); }
        if (payload is null) return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));

        _logger.LogInformation(
            "Service ticket {TicketId} resolved for tenant {TenantId}, customer {CustomerId}, chargeable: {Chargeable}.",
            payload.ServiceTicketId, payload.TenantId, payload.CustomerId, payload.ChargeableAmount);
        return Task.FromResult(OutboxHandlerResult.Processed($"Resolved:{payload.ServiceTicketId}"));
    }
}

public sealed class WarrantyExtendedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "WarrantyExtended";
    public string MessageType => MessageTypeKey;
    private readonly ILogger<WarrantyExtendedOutboxHandler> _logger;
    public WarrantyExtendedOutboxHandler(ILogger<WarrantyExtendedOutboxHandler> logger) => _logger = logger;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        WarrantyExtendedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WarrantyExtendedEvent>(payloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex) { return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}")); }
        if (payload is null) return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));

        _logger.LogInformation(
            "Warranty contract {ContractId} extended by {AddedMonths} months for tenant {TenantId}; new end {NewEndDate:o}.",
            payload.WarrantyContractId, payload.AddedMonths, payload.TenantId, payload.NewEndDate);
        return Task.FromResult(OutboxHandlerResult.Processed($"Extended:{payload.WarrantyContractId}:+{payload.AddedMonths}m"));
    }
}

public sealed class ServiceTicketAssignedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "ServiceTicketAssigned";
    public string MessageType => MessageTypeKey;
    private readonly ILogger<ServiceTicketAssignedOutboxHandler> _logger;
    public ServiceTicketAssignedOutboxHandler(ILogger<ServiceTicketAssignedOutboxHandler> logger) => _logger = logger;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        ServiceTicketAssignedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ServiceTicketAssignedEvent>(payloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex) { return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}")); }
        if (payload is null) return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));

        _logger.LogInformation(
            "Service ticket {TicketId} assigned to user {AssignedToUserId} for tenant {TenantId}.",
            payload.ServiceTicketId, payload.AssignedToUserId, payload.TenantId);
        return Task.FromResult(OutboxHandlerResult.Processed($"Assigned:{payload.ServiceTicketId}"));
    }
}
