using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class StockCount : TenantEntity
{
    public string CountNumber { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public string WarehouseCode { get; private set; } = string.Empty;
    public string WarehouseName { get; private set; } = string.Empty;
    public StockCountStatus Status { get; private set; } = StockCountStatus.Plan;
    public DateTime PlannedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? CountingStartedAtUtc { get; private set; }
    public DateTime? ReconciledAtUtc { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? PlannedByUserId { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<StockCountLine> Lines { get; private set; } = new List<StockCountLine>();

    public decimal TotalVarianceQuantity => Lines.Sum(l => l.VarianceQuantity);
    public decimal TotalVarianceCost => Lines.Sum(l => l.VarianceCost);

    protected StockCount() { }

    public StockCount(
        string countNumber,
        Guid warehouseId,
        string warehouseCode,
        string warehouseName,
        DateTime plannedAtUtc,
        Guid? plannedByUserId = null,
        string? notes = null)
    {
        CountNumber = countNumber;
        WarehouseId = warehouseId;
        WarehouseCode = warehouseCode;
        WarehouseName = warehouseName;
        PlannedAtUtc = plannedAtUtc;
        PlannedByUserId = plannedByUserId;
        Notes = notes;
    }

    public void ReplaceLines(IEnumerable<StockCountLine> lines)
    {
        EnsurePlan();
        Lines.Clear();
        foreach (var line in lines)
        {
            Lines.Add(line);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void BeginCounting()
    {
        if (Status != StockCountStatus.Plan)
        {
            throw new InvalidStockCountStateException(Status.ToString(), nameof(BeginCounting));
        }
        if (Lines.Count == 0)
        {
            throw new InvalidStockCountStateException(Status.ToString(), "BeginCounting (no lines)");
        }
        Status = StockCountStatus.Counting;
        CountingStartedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CountingStartedAtUtc.Value;
    }

    public StockCountLine RecordLineCount(Guid lineId, decimal countedQuantity, Guid? countedByUserId, string? lineNotes)
    {
        if (Status != StockCountStatus.Counting && Status != StockCountStatus.Reconciliation)
        {
            throw new InvalidStockCountStateException(Status.ToString(), nameof(RecordLineCount));
        }
        var line = Lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new StockCountLineNotFoundException();
        line.RecordCount(countedQuantity, countedByUserId, lineNotes);
        UpdatedAtUtc = DateTime.UtcNow;
        return line;
    }

    public void Reconcile(string? notes)
    {
        if (Status != StockCountStatus.Counting)
        {
            throw new InvalidStockCountStateException(Status.ToString(), nameof(Reconcile));
        }
        if (Lines.Any(l => !l.IsCounted))
        {
            throw new InvalidStockCountStateException(Status.ToString(), "Reconcile (uncounted lines)");
        }
        Status = StockCountStatus.Reconciliation;
        ReconciledAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes;
        }
        UpdatedAtUtc = ReconciledAtUtc.Value;
    }

    public void MarkPosted(Guid? postedByUserId)
    {
        if (Status != StockCountStatus.Reconciliation)
        {
            throw new InvalidStockCountStateException(Status.ToString(), nameof(MarkPosted));
        }
        Status = StockCountStatus.Posted;
        PostedAtUtc = DateTime.UtcNow;
        PostedByUserId = postedByUserId;
        UpdatedAtUtc = PostedAtUtc.Value;
    }

    public void Cancel()
    {
        if (Status == StockCountStatus.Posted)
        {
            throw new InvalidStockCountStateException(Status.ToString(), nameof(Cancel));
        }
        Status = StockCountStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsurePlan()
    {
        if (Status != StockCountStatus.Plan)
        {
            throw new InvalidStockCountStateException(Status.ToString(), "ReplaceLines");
        }
    }
}
