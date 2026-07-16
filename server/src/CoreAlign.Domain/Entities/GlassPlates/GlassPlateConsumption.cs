using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassPlates;

public class GlassPlateConsumption : TenantEntity
{
    public Guid GlassPlateId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }

    public Guid? OrderLineId { get; private set; }
    public Guid? JobId { get; private set; }

    public decimal CutAreaMm2 { get; private set; }
    public int Pieces { get; private set; }
    public decimal? CutWidthMm { get; private set; }
    public decimal? CutHeightMm { get; private set; }

    public Guid? ResultingRemnantPlateId { get; private set; }
    public decimal ScrappedAreaMm2 { get; private set; }
    public Guid? ScrapReasonCodeId { get; private set; }

    public Guid? WorkCenterId { get; private set; }
    public Guid? OperatorId { get; private set; }
    public Guid? StockMovementId { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }
    public Guid PostedByUserId { get; private set; }

    public GlassPlate GlassPlate { get; set; } = null!;

    protected GlassPlateConsumption() { }

    public GlassPlateConsumption(
        Guid glassPlateId,
        Guid productId,
        Guid warehouseId,
        decimal cutAreaMm2,
        int pieces,
        DateTime occurredAtUtc,
        Guid postedByUserId,
        Guid? orderLineId = null,
        Guid? jobId = null,
        decimal? cutWidthMm = null,
        decimal? cutHeightMm = null,
        Guid? resultingRemnantPlateId = null,
        decimal scrappedAreaMm2 = 0m,
        Guid? scrapReasonCodeId = null,
        Guid? workCenterId = null,
        Guid? operatorId = null,
        Guid? stockMovementId = null)
    {
        GlassPlateId = glassPlateId;
        ProductId = productId;
        WarehouseId = warehouseId;
        CutAreaMm2 = cutAreaMm2;
        Pieces = pieces;
        OrderLineId = orderLineId;
        JobId = jobId;
        CutWidthMm = cutWidthMm;
        CutHeightMm = cutHeightMm;
        ResultingRemnantPlateId = resultingRemnantPlateId;
        ScrappedAreaMm2 = scrappedAreaMm2;
        ScrapReasonCodeId = scrapReasonCodeId;
        WorkCenterId = workCenterId;
        OperatorId = operatorId;
        StockMovementId = stockMovementId;
        OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        PostedByUserId = postedByUserId;
    }
}
