using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.GlassPlates;

public class GlassPlate : TenantEntity, IHasConcurrencyToken
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public Guid? LotId { get; private set; }

    public string PlateNumber { get; private set; } = string.Empty;
    public PlateKind Kind { get; private set; }
    public GlassPlateStatus Status { get; private set; } = GlassPlateStatus.Available;

    public decimal WidthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public decimal ThicknessMm { get; private set; }
    public decimal OriginalAreaMm2 { get; private set; }
    public decimal RemainingAreaMm2 { get; private set; }

    public Guid? ParentPlateId { get; private set; }
    public Guid? SourceReceiptMovementId { get; private set; }
    public Guid? ReservedByJobId { get; private set; }
    public PlateCondition Condition { get; private set; } = PlateCondition.Good;

    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public StorageLocation? StorageLocation { get; set; }

    protected GlassPlate() { }

    public GlassPlate(
        Guid productId,
        Guid warehouseId,
        string plateNumber,
        decimal widthMm,
        decimal heightMm,
        decimal thicknessMm,
        PlateKind kind,
        DateTime receivedAtUtc,
        Guid? storageLocationId = null,
        Guid? lotId = null,
        Guid? parentPlateId = null,
        Guid? sourceReceiptMovementId = null,
        PlateCondition condition = PlateCondition.Good,
        string? notes = null)
    {
        if (widthMm <= 0m) throw new ArgumentOutOfRangeException(nameof(widthMm), "Width must be positive.");
        if (heightMm <= 0m) throw new ArgumentOutOfRangeException(nameof(heightMm), "Height must be positive.");
        if (thicknessMm < 0m) throw new ArgumentOutOfRangeException(nameof(thicknessMm), "Thickness cannot be negative.");

        ProductId = productId;
        WarehouseId = warehouseId;
        StorageLocationId = storageLocationId;
        LotId = lotId;
        PlateNumber = plateNumber.Trim();
        Kind = kind;
        WidthMm = widthMm;
        HeightMm = heightMm;
        ThicknessMm = thicknessMm;
        OriginalAreaMm2 = widthMm * heightMm;
        RemainingAreaMm2 = OriginalAreaMm2;
        ParentPlateId = parentPlateId;
        SourceReceiptMovementId = sourceReceiptMovementId;
        Condition = condition;
        ReceivedAtUtc = DateTime.SpecifyKind(receivedAtUtc, DateTimeKind.Utc);
        Notes = notes;
    }

    public GlassPlate CreateRemnant(string plateNumber, decimal widthMm, decimal heightMm, DateTime receivedAtUtc) =>
        new(
            ProductId,
            WarehouseId,
            plateNumber,
            widthMm,
            heightMm,
            ThicknessMm,
            PlateKind.Remnant,
            receivedAtUtc,
            StorageLocationId,
            LotId,
            parentPlateId: Id,
            condition: Condition);

    public void Reserve(Guid jobId)
    {
        EnsureTransitionAllowed(GlassPlateStatus.Reserved);
        Status = GlassPlateStatus.Reserved;
        ReservedByJobId = jobId;
        Touch();
    }

    public void Release()
    {
        if (Status != GlassPlateStatus.Reserved)
        {
            throw new InvalidGlassPlateTransitionException(Status.ToString(), GlassPlateStatus.Available.ToString());
        }
        Status = GlassPlateStatus.Available;
        ReservedByJobId = null;
        Touch();
    }

    public void MarkInUse()
    {
        EnsureTransitionAllowed(GlassPlateStatus.InUse);
        Status = GlassPlateStatus.InUse;
        Touch();
    }

    public void ConsumeArea(decimal cutAreaMm2, DateTime occurredAtUtc)
    {
        if (cutAreaMm2 <= 0m) throw new ArgumentOutOfRangeException(nameof(cutAreaMm2), "Cut area must be positive.");
        if (IsTerminal())
        {
            throw new InvalidGlassPlateTransitionException(Status.ToString(), GlassPlateStatus.Consumed.ToString());
        }
        if (cutAreaMm2 > RemainingAreaMm2 + AreaEpsilon)
        {
            throw new GlassPlateAreaExceededException(cutAreaMm2, RemainingAreaMm2);
        }
        RemainingAreaMm2 = Math.Max(0m, RemainingAreaMm2 - cutAreaMm2);
        Touch();
    }

    public void MarkConsumed(DateTime occurredAtUtc)
    {
        EnsureTransitionAllowed(GlassPlateStatus.Consumed);
        Status = GlassPlateStatus.Consumed;
        ConsumedAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        ReservedByJobId = null;
        Touch();
    }

    public void Scrap(DateTime occurredAtUtc)
    {
        EnsureTransitionAllowed(GlassPlateStatus.Scrapped);
        Status = GlassPlateStatus.Scrapped;
        ConsumedAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        ReservedByJobId = null;
        Touch();
    }

    public void MoveTo(Guid warehouseId, Guid? storageLocationId)
    {
        WarehouseId = warehouseId;
        StorageLocationId = storageLocationId;
        Touch();
    }

    private const decimal AreaEpsilon = 1m;

    private bool IsTerminal() => Status is GlassPlateStatus.Consumed or GlassPlateStatus.Scrapped;

    private bool IsTransitionAllowed(GlassPlateStatus target) => Status switch
    {
        GlassPlateStatus.Available => target is GlassPlateStatus.Reserved or GlassPlateStatus.InUse
            or GlassPlateStatus.Consumed or GlassPlateStatus.Scrapped,
        GlassPlateStatus.Reserved => target is GlassPlateStatus.Available or GlassPlateStatus.InUse
            or GlassPlateStatus.Consumed or GlassPlateStatus.Scrapped,
        GlassPlateStatus.InUse => target is GlassPlateStatus.Available or GlassPlateStatus.Consumed
            or GlassPlateStatus.Scrapped,
        _ => false
    };

    private void EnsureTransitionAllowed(GlassPlateStatus target)
    {
        if (!IsTransitionAllowed(target))
        {
            throw new InvalidGlassPlateTransitionException(Status.ToString(), target.ToString());
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
