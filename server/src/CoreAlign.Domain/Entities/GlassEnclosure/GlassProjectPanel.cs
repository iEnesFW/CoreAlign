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
