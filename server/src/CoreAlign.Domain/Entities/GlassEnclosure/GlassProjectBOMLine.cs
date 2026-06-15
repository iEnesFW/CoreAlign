using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectBOMLine : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public GlassBOMLineKind Kind { get; private set; } = GlassBOMLineKind.ProfileCut;

    /// <summary>
    /// Legacy catalog reference id. Use <see cref="ProductId"/> instead for stock and pricing operations.
    /// Retained for backwards compatibility and audit/traceability.
    /// </summary>
    public Guid? RefId { get; private set; }

    /// <summary>
    /// Canonical link to the unified Product catalog. Required for non-service lines so that
    /// stock movements, pricing and downstream order conversion can resolve a single source of truth.
    /// Nullable during F1.3 backfill window; will be enforced NOT NULL after backfill completes.
    /// </summary>
    public Guid? ProductId { get; private set; }

    /// <summary>
    /// True when the line represents a non-stock cost element (labor, transport, installation, etc.).
    /// Service lines are exempt from <see cref="ProductId"/> requirement and do not generate stock movements.
    /// </summary>
    public bool IsService { get; private set; }

    /// <summary>
    /// Optional per-piece cut specification (e.g. profile/glass piece dimensions) used in F2 PerPiece flow.
    /// Free-form JSON kept as text for flexibility; not stored as jsonb.
    /// </summary>
    public string? CutSpecJson { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = "Piece";
    public decimal UnitCost { get; private set; }
    public decimal? UnitPriceOverride { get; private set; }
    public decimal LineCost { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public string? Source { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsManual { get; private set; }

    protected GlassProjectBOMLine() { }

    public GlassProjectBOMLine(
        Guid projectId,
        GlassBOMLineKind kind,
        string description,
        decimal quantity,
        string unit,
        decimal unitCost,
        string currency,
        Guid? refId = null,
        string? source = null,
        int sortOrder = 0,
        Guid? productId = null,
        bool isService = false,
        string? cutSpecJson = null,
        bool isManual = false)
    {
        ProjectId = projectId;
        Kind = kind;
        Description = description;
        Quantity = quantity;
        Unit = unit;
        UnitCost = unitCost;
        LineCost = decimal.Round(quantity * unitCost, 4);
        Currency = currency;
        RefId = refId;
        Source = source;
        SortOrder = sortOrder;
        ProductId = productId;
        IsService = isService;
        CutSpecJson = cutSpecJson;
        IsManual = isManual;
    }

    public decimal EffectiveUnitCost => UnitPriceOverride ?? UnitCost;

    public void ApplyUnitPriceOverride(decimal? unitPriceOverride)
    {
        UnitPriceOverride = unitPriceOverride;
        LineCost = decimal.Round(Quantity * EffectiveUnitCost, 4);
    }

    public void AdoptOverrideAsUnitCost()
    {
        if (!UnitPriceOverride.HasValue) return;
        UnitCost = UnitPriceOverride.Value;
        UnitPriceOverride = null;
        LineCost = decimal.Round(Quantity * UnitCost, 4);
    }

    /// <summary>
    /// Associates this BOM line with a canonical <c>Product</c>. Used by BOMComposer after
    /// resolving the linked product via <c>ICatalogProductLinker.EnsureLinkedAsync</c>, and by
    /// the F1.3 backfill command for historical lines whose only reference was <see cref="RefId"/>.
    /// </summary>
    public void UpdateProductLink(Guid productId)
    {
        ProductId = productId;
    }

    /// <summary>
    /// Flags this line as a non-stock service entry (labor, transport, installation, etc.).
    /// </summary>
    public void MarkAsService()
    {
        IsService = true;
    }

    /// <summary>
    /// Sets or clears the per-piece cut specification JSON used by F2 PerPiece cutting flows.
    /// </summary>
    public void SetCutSpec(string? cutSpecJson)
    {
        CutSpecJson = cutSpecJson;
    }
}
