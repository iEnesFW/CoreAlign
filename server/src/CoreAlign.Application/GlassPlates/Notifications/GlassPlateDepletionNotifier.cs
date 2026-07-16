using CoreAlign.Application.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassPlates.Notifications;

public interface IGlassPlateDepletionNotifier
{
    Task NotifyIfDepletedAsync(
        Guid tenantId,
        Product product,
        Guid warehouseId,
        int availableCountAfter,
        Guid actingUserId,
        CancellationToken cancellationToken = default);
}

// WHY: dispatched INLINE from the consume/scrap handler (not a domain-event subscriber) — domain
// events fire before the flush and the dispatcher itself calls SaveChanges, so a subscriber would
// re-enter SaveChanges and read a stale available-count. The stable payload (no varying count) keeps
// the dispatcher's dedup to one notification per (product, warehouse, level).
public class GlassPlateDepletionNotifier : IGlassPlateDepletionNotifier
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly IWarehouseRepository _warehouses;

    public GlassPlateDepletionNotifier(INotificationDispatcher dispatcher, IWarehouseRepository warehouses)
    {
        _dispatcher = dispatcher;
        _warehouses = warehouses;
    }

    public async Task NotifyIfDepletedAsync(
        Guid tenantId,
        Product product,
        Guid warehouseId,
        int availableCountAfter,
        Guid actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (actingUserId == Guid.Empty) return;

        string? templateKey = null;
        if (availableCountAfter <= 0)
        {
            templateKey = "GlassPlateDepleted";
        }
        else if (product.MinPlateCount is int min && min > 0 && availableCountAfter <= min)
        {
            templateKey = "GlassPlateLow";
        }
        if (templateKey is null) return;

        var warehouse = await _warehouses.GetByIdAsync(warehouseId, cancellationToken);
        var warehouseName = warehouse?.Name ?? string.Empty;

        await _dispatcher.DispatchAsync(new NotificationRequest(
            TenantId: tenantId,
            UserId: actingUserId,
            CustomerId: null,
            CategoryKey: "GlassPlate",
            TemplateKey: templateKey,
            Locale: "tr",
            Payload: new
            {
                productId = product.Id,
                sku = product.Sku,
                warehouseId,
                warehouse = warehouseName,
                level = templateKey,
            },
            ChannelsOverride: new[] { NotificationChannel.InApp }), cancellationToken);
    }
}
