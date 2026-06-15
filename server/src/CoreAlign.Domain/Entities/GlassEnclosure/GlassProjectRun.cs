using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectRun : TenantEntity, IHasConcurrencyToken
{
    public Guid ProjectId { get; private set; }
    public int OrderIndex { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public int LengthMm { get; private set; }
    public int HeightMm { get; private set; }
    public decimal OriginX { get; private set; }
    public decimal OriginY { get; private set; }
    public decimal RotationDeg { get; private set; }
    public Guid ProfileSystemId { get; private set; }
    public Guid? ColorId { get; private set; }
    public bool HasTopDrip { get; private set; }
    public bool HasBottomThreshold { get; private set; }
    public string? Notes { get; private set; }
    public int? GeomZ { get; private set; }
    public decimal? GeomTiltDeg { get; private set; }
    public int? GeomArcRadiusMm { get; private set; }
    public decimal? GeomArcSweepDeg { get; private set; }
    public bool ArcGlassBent { get; private set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    private readonly List<GlassProjectPanel> _panels = new();
    public IReadOnlyCollection<GlassProjectPanel> Panels => _panels;

    protected GlassProjectRun() { }

    public GlassProjectRun(
        Guid projectId,
        int orderIndex,
        string label,
        int lengthMm,
        int heightMm,
        Guid profileSystemId,
        decimal originX = 0m,
        decimal originY = 0m,
        decimal rotationDeg = 0m,
        Guid? colorId = null,
        bool hasTopDrip = false,
        bool hasBottomThreshold = false,
        string? notes = null)
    {
        ProjectId = projectId;
        OrderIndex = orderIndex;
        Label = label;
        LengthMm = lengthMm;
        HeightMm = heightMm;
        ProfileSystemId = profileSystemId;
        OriginX = originX;
        OriginY = originY;
        RotationDeg = rotationDeg;
        ColorId = colorId;
        HasTopDrip = hasTopDrip;
        HasBottomThreshold = hasBottomThreshold;
        Notes = notes;
    }

    public void UpdateGeometry(
        int lengthMm,
        int heightMm,
        decimal originX,
        decimal originY,
        decimal rotationDeg)
    {
        LengthMm = lengthMm;
        HeightMm = heightMm;
        OriginX = originX;
        OriginY = originY;
        RotationDeg = rotationDeg;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateConfiguration(
        string label,
        Guid profileSystemId,
        Guid? colorId,
        bool hasTopDrip,
        bool hasBottomThreshold,
        string? notes)
    {
        Label = label;
        ProfileSystemId = profileSystemId;
        ColorId = colorId;
        HasTopDrip = hasTopDrip;
        HasBottomThreshold = hasBottomThreshold;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reorder(int orderIndex)
    {
        OrderIndex = orderIndex;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateGeometry3D(int? z, decimal? tiltDeg, int? arcRadiusMm, decimal? arcSweepDeg, bool arcGlassBent = false)
    {
        GeomZ = z;
        GeomTiltDeg = tiltDeg;
        GeomArcRadiusMm = arcRadiusMm;
        GeomArcSweepDeg = arcSweepDeg;
        ArcGlassBent = arcGlassBent;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddPanel(GlassProjectPanel panel) => _panels.Add(panel);
    public void RemovePanel(Guid panelId)
    {
        var panel = _panels.FirstOrDefault(p => p.Id == panelId);
        if (panel is not null) _panels.Remove(panel);
    }

    public void ReplacePanels(IEnumerable<GlassProjectPanel> panels)
    {
        _panels.Clear();
        _panels.AddRange(panels);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
