using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectPanel : TenantEntity, IHasConcurrencyToken
{
    public Guid RunId { get; private set; }
    public int PanelIndex { get; private set; }
    public int WidthMm { get; private set; }
    public GlassOpeningType OpeningType { get; private set; } = GlassOpeningType.Fixed;
    public PanelKind PanelKind { get; private set; } = PanelKind.Rectangular;
    public Guid GlassTypeId { get; private set; }
    public bool HasHandle { get; private set; }
    public bool HasLock { get; private set; }
    public bool HasBrushSeal { get; private set; }
    public string? Notes { get; private set; }

    // Optional shape geometry (null = a plain rectangle). Persisted so custom panel
    // shapes survive a reload and feed the server-side cutting list.
    public int? HeightMm { get; private set; }
    public string? TopShape { get; private set; }
    public int? TopRightHeightMm { get; private set; }
    public int? ArchRiseMm { get; private set; }
    public int? CornerRadiusTlMm { get; private set; }
    public int? CornerRadiusTrMm { get; private set; }
    public int? CornerRadiusBrMm { get; private set; }
    public int? CornerRadiusBlMm { get; private set; }

    // Silhouette kind beyond a top-edge variation: null = rectangular (use TopShape),
    // "ellipse" = round/oval bounded by Width × Height, "polygon" = free outline in ShapePointsJson.
    public string? ShapeKind { get; private set; }
    public string? ShapePointsJson { get; private set; }

    // Catalog hardware placed on this panel in the 3D designer. Structural (FK to HardwareItem) so it
    // reaches the BOM/quote/cutting — unlike the render-only SceneHardwareItem blob.
    private readonly List<GlassProjectPanelHardware> _hardware = new();
    public IReadOnlyCollection<GlassProjectPanelHardware> Hardware => _hardware.AsReadOnly();

    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected GlassProjectPanel() { }

    public GlassProjectPanel(
        Guid runId,
        int panelIndex,
        int widthMm,
        GlassOpeningType openingType,
        Guid glassTypeId,
        bool hasHandle = false,
        bool hasLock = false,
        bool hasBrushSeal = false,
        string? notes = null)
    {
        RunId = runId;
        PanelIndex = panelIndex;
        WidthMm = widthMm;
        OpeningType = openingType;
        GlassTypeId = glassTypeId;
        HasHandle = hasHandle;
        HasLock = hasLock;
        HasBrushSeal = hasBrushSeal;
        Notes = notes;
    }

    public void ReplaceHardware(IEnumerable<(Guid HardwareItemId, decimal Quantity)> items)
    {
        _hardware.Clear();
        foreach (var (hardwareItemId, quantity) in items)
        {
            if (hardwareItemId != Guid.Empty && quantity > 0m)
            {
                _hardware.Add(new GlassProjectPanelHardware(Id, hardwareItemId, quantity));
            }
        }
    }

    public void Update(
        int widthMm,
        GlassOpeningType openingType,
        Guid glassTypeId,
        bool hasHandle,
        bool hasLock,
        bool hasBrushSeal,
        string? notes)
    {
        WidthMm = widthMm;
        OpeningType = openingType;
        GlassTypeId = glassTypeId;
        HasHandle = hasHandle;
        HasLock = hasLock;
        HasBrushSeal = hasBrushSeal;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateShape(
        int? heightMm,
        string? topShape,
        int? topRightHeightMm,
        int? archRiseMm,
        int? cornerRadiusTlMm,
        int? cornerRadiusTrMm,
        int? cornerRadiusBrMm,
        int? cornerRadiusBlMm,
        string? shapeKind = null,
        string? shapePointsJson = null)
    {
        HeightMm = heightMm;
        TopShape = string.IsNullOrWhiteSpace(topShape) ? null : topShape;
        TopRightHeightMm = topRightHeightMm;
        ArchRiseMm = archRiseMm;
        CornerRadiusTlMm = cornerRadiusTlMm;
        CornerRadiusTrMm = cornerRadiusTrMm;
        CornerRadiusBrMm = cornerRadiusBrMm;
        CornerRadiusBlMm = cornerRadiusBlMm;
        ShapeKind = string.IsNullOrWhiteSpace(shapeKind) ? null : shapeKind;
        ShapePointsJson = string.IsNullOrWhiteSpace(shapePointsJson) ? null : shapePointsJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reindex(int panelIndex)
    {
        PanelIndex = panelIndex;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPanelKind(PanelKind kind)
    {
        PanelKind = kind;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
