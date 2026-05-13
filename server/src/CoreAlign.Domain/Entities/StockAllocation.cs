using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class StockAllocation : TenantEntity
{
    public Guid OrderId { get; private set; }
    public Guid OrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? LotId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal QuantityConsumed { get; private set; }
    public AllocationStatus Status { get; private set; } = AllocationStatus.Active;
    public DateTime AllocatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? ReleasedAtUtc { get; private set; }

    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Lot? Lot { get; set; }

    public decimal Remaining => Quantity - QuantityConsumed;

    protected StockAllocation() { }

    public StockAllocation(Guid orderId, Guid orderLineId, Guid productId, Guid warehouseId, decimal quantity, Guid? lotId = null)
    {
        OrderId = orderId;
        OrderLineId = orderLineId;
        ProductId = productId;
        WarehouseId = warehouseId;
        Quantity = quantity;
        LotId = lotId;
    }

    public void Consume(decimal qty, DateTime occurredAtUtc)
    {
        if (qty <= 0m) return;
        if (qty > Remaining)
        {
            qty = Remaining;
        }
        QuantityConsumed += qty;
        Status = QuantityConsumed >= Quantity ? AllocationStatus.Consumed : AllocationStatus.PartiallyConsumed;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void Release(DateTime occurredAtUtc)
    {
        Status = AllocationStatus.Released;
        ReleasedAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void IncreaseQuantity(decimal extra, DateTime occurredAtUtc)
    {
        if (extra <= 0m) return;
        Quantity += extra;
        UpdatedAtUtc = occurredAtUtc;
    }
}
