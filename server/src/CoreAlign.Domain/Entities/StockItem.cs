using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class StockItem : TenantEntity
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? LotId { get; private set; }
    public string? BinLocation { get; private set; }
    public decimal OnHand { get; private set; }
    public decimal Reserved { get; private set; }
    public decimal AvgCost { get; private set; }
    public DateTime? LastMovementAtUtc { get; private set; }

    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Lot? Lot { get; set; }

    public decimal AvailableToPromise => OnHand - Reserved;

    protected StockItem() { }

    public StockItem(Guid productId, Guid warehouseId, Guid? lotId = null, string? binLocation = null)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        LotId = lotId;
        BinLocation = binLocation;
    }

    public void ApplyReceipt(decimal quantity, decimal unitCost, DateTime occurredAtUtc)
    {
        if (quantity <= 0m)
        {
            throw new StockMovementValidationException("Receipt quantity must be positive.");
        }
        var prevValue = AvgCost * OnHand;
        var addedValue = unitCost * quantity;
        OnHand += quantity;
        if (OnHand > 0m)
        {
            AvgCost = Math.Round((prevValue + addedValue) / OnHand, 4);
        }
        LastMovementAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void ApplyIssue(decimal quantity, DateTime occurredAtUtc, bool allowNegative = false)
    {
        if (quantity <= 0m)
        {
            throw new StockMovementValidationException("Issue quantity must be positive.");
        }
        if (!allowNegative && OnHand - quantity < 0m)
        {
            throw new StockMovementValidationException(
                $"Issue would result in negative stock (OnHand={OnHand}, requested={quantity}).");
        }
        OnHand -= quantity;
        LastMovementAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void ApplyAdjustment(decimal delta, decimal? unitCost, DateTime occurredAtUtc, bool allowNegative = false)
    {
        if (delta == 0m) return;
        if (!allowNegative && OnHand + delta < 0m)
        {
            throw new StockMovementValidationException(
                $"Adjustment would result in negative stock (OnHand={OnHand}, delta={delta}).");
        }
        if (delta > 0m && unitCost.HasValue)
        {
            ApplyReceipt(delta, unitCost.Value, occurredAtUtc);
            return;
        }
        OnHand += delta;
        LastMovementAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void Reserve(decimal quantity, DateTime occurredAtUtc)
    {
        if (quantity <= 0m)
        {
            throw new StockMovementValidationException("Reserve quantity must be positive.");
        }
        if (AvailableToPromise < quantity)
        {
            throw new InsufficientAvailableStockException(
                Product?.Sku ?? string.Empty,
                Warehouse?.Code ?? string.Empty,
                quantity,
                AvailableToPromise);
        }
        Reserved += quantity;
        LastMovementAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void Release(decimal quantity, DateTime occurredAtUtc)
    {
        if (quantity <= 0m) return;
        Reserved = Math.Max(0m, Reserved - quantity);
        LastMovementAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void ConsumeReservation(decimal quantity, DateTime occurredAtUtc)
    {
        if (quantity <= 0m) return;
        Release(quantity, occurredAtUtc);
        ApplyIssue(quantity, occurredAtUtc);
    }

    public void SeedOpeningBalance(decimal quantity, decimal unitCost, DateTime occurredAtUtc)
    {
        OnHand = quantity;
        AvgCost = unitCost;
        LastMovementAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void UpdateBinLocation(string? bin)
    {
        BinLocation = bin;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
