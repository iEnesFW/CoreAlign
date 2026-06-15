using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Billing;

/// <summary>
/// Drains a <see cref="SubscriptionActivatedOutbox.MessageType"/> message: upserts
/// <see cref="TenantModule"/> rows for every line in the order, marks the order
/// Completed, and creates one in-app <see cref="Notification"/> per active
/// TenantAdmin. Idempotent: re-running on a Completed order is a no-op.
/// </summary>
public sealed class SubscriptionActivatedOutboxHandler : IOutboxMessageHandler
{
    private const string TenantAdminRole = "TenantAdmin";
    private const string NotificationType = "SubscriptionActivated";
    private const string NotificationEntityType = "SubscriptionOrder";

    public string MessageType => SubscriptionActivatedOutbox.MessageType;

    private readonly ISubscriptionOrderRepository _orders;
    private readonly ITenantModuleRepository _tenantModules;
    private readonly IModuleRepository _modules;
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public SubscriptionActivatedOutboxHandler(
        ISubscriptionOrderRepository orders,
        ITenantModuleRepository tenantModules,
        IModuleRepository modules,
        INotificationRepository notifications,
        IUserRepository users,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _orders = orders;
        _tenantModules = tenantModules;
        _modules = modules;
        _notifications = notifications;
        _users = users;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        SubscriptionActivatedPayload? payload;
        try
        {
            payload = SubscriptionActivatedOutbox.Deserialize(payloadJson);
        }
        catch (Exception ex)
        {
            return OutboxHandlerResult.Failed($"Payload deserialize failed: {ex.Message}");
        }
        if (payload is null)
        {
            return OutboxHandlerResult.Failed("Payload deserialized to null.");
        }

        var tenantId = _tenant.CurrentTenantId ?? payload.TenantId;
        if (tenantId == Guid.Empty)
        {
            return OutboxHandlerResult.Failed("Tenant id missing.");
        }
        using var tenantScope = _tenant.PushScope(tenantId);

        var order = await _orders.GetByIdWithDetailsAsync(payload.OrderId, cancellationToken);
        if (order is null)
        {
            return OutboxHandlerResult.Failed($"Order {payload.OrderId} not found.");
        }

        if (order.Status != SubscriptionOrderStatus.Paid)
        {
            if (order.CompletedAtUtc.HasValue)
            {
                return OutboxHandlerResult.Processed("AlreadyCompleted");
            }
            return OutboxHandlerResult.Failed($"Order is in status {order.Status}; cannot activate.");
        }

        if (order.CompletedAtUtc.HasValue)
        {
            return OutboxHandlerResult.Processed("AlreadyCompleted");
        }

        var now = DateTime.UtcNow;
        foreach (var item in order.Items)
        {
            var existing = await _tenantModules.GetByModuleIdAsync(item.ModuleId, cancellationToken);
            if (existing is null)
            {
                var newGrant = new TenantModule(item.ModuleId, now, now.AddDays(item.DurationDays), TenantModuleSource.Paid);
                await _tenantModules.AddAsync(newGrant, cancellationToken);
            }
            else
            {
                existing.Extend(item.DurationDays);
                existing.SetSource(TenantModuleSource.Paid);
                _tenantModules.Update(existing);
            }
        }

        order.MarkCompleted();
        _orders.Update(order);

        await NotifyTenantAdminsAsync(order, tenantId, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return OutboxHandlerResult.Processed($"Provisioned:{order.Items.Count}");
    }

    private async Task NotifyTenantAdminsAsync(SubscriptionOrder order, Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantUsers = await _users.ListByTenantAsync(tenantId, cancellationToken);
        var admins = tenantUsers
            .Where(u => u.IsActive && u.UserRoles.Any(r => string.Equals(r.Role?.Name, TenantAdminRole, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (admins.Count == 0) return;

        var moduleIds = order.Items.Select(i => i.ModuleId).Distinct().ToList();
        var moduleLookup = (await _modules.ListByIdsAsync(moduleIds, cancellationToken)).ToDictionary(m => m.Id, m => m.Name);

        var summary = string.Join(", ", order.Items.Select(i =>
            $"{ModuleNameFor(i, moduleLookup)} (+{i.DurationDays}d)"));
        var title = $"Subscription activated: {order.OrderNumber}";
        var body = $"Modules activated: {summary}.";

        var actorUserId = order.CreatedByUserId == Guid.Empty ? (Guid?)null : order.CreatedByUserId;
        var notifications = admins.Select(a => new Notification(
            a.Id,
            actorUserId,
            NotificationType,
            NotificationEntityType,
            order.Id,
            title,
            body)).ToList();
        await _notifications.AddRangeAsync(notifications, cancellationToken);
    }

    private static string ModuleNameFor(SubscriptionOrderItem item, IReadOnlyDictionary<Guid, string> lookup)
    {
        return lookup.TryGetValue(item.ModuleId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : item.ModuleName;
    }
}
