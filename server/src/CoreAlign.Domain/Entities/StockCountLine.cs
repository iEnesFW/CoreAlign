using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class StockCountLine : TenantEntity
{
    public Guid StockCountId { get; internal set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public Guid? LotId { get; private set; }
    public string? LotNumber { get; private set; }
    public string? BinLocation { get; private set; }
    public decimal ExpectedQuantity { get; private set; }
    public decimal? CountedQuantity { get; private set; }
    public decimal VarianceQuantity { get; private set; }
    public decimal SnapshotUnitCost { get; private set; }
    public decimal VarianceCost { get; private set; }
    public DateTime? CountedAtUtc { get; private set; }
    public Guid? CountedByUserId { get; private set; }
    public string? LineNotes { get; private set; }

    public bool IsCounted => CountedQuantity.HasValue;

    public StockCount StockCount { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Lot? Lot { get; set; }

    protected StockCountLine() { }

    public StockCountLine(
        Guid productId,
        string productSku,
        string productName,
        decimal expectedQuantity,
        decimal snapshotUnitCost,
        Guid? lotId = null,
        string? lotNumber = null,
        string? binLocation = null)
    {
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        ExpectedQuantity = Math.Round(expectedQuantity, 4);
        SnapshotUnitCost = Math.Round(snapshotUnitCost, 4);
        LotId = lotId;
        LotNumber = lotNumber;
        BinLocation = binLocation;
    }

    public void RecordCount(decimal countedQuantity, Guid? countedByUserId, string? lineNotes)
    {
        if (countedQuantity < 0m)
        {
            throw new InvalidStockCountStateException("Counting", "RecordCount (negative)");
        }
        CountedQuantity = Math.Round(countedQuantity, 4);
        VarianceQuantity = Math.Round(CountedQuantity.Value - ExpectedQuantity, 4);
        VarianceCost = Math.Round(VarianceQuantity * SnapshotUnitCost, 4);
        CountedAtUtc = DateTime.UtcNow;
        CountedByUserId = countedByUserId;
        if (!string.IsNullOrWhiteSpace(lineNotes))
        {
            LineNotes = lineNotes;
        }
        UpdatedAtUtc = CountedAtUtc.Value;
    }
}
