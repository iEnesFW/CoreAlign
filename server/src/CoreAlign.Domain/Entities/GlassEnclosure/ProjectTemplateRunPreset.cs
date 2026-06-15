using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class ProjectTemplateRunPreset : TenantEntity, IHasConcurrencyToken, IGlobalReadable
{
    public Guid TemplateId { get; private set; }
    public int OrderIndex { get; private set; }
    public string LabelKey { get; private set; } = string.Empty;
    public int LengthMm { get; private set; }
    public int HeightMm { get; private set; }
    public decimal OriginX { get; private set; }
    public decimal OriginY { get; private set; }
    public decimal RotationDeg { get; private set; }
    public int DefaultPanelCount { get; private set; }
    public int DefaultPanelWidthMm { get; private set; }
    public GlassOpeningType DefaultOpeningType { get; private set; } = GlassOpeningType.Fixed;
    public bool HasTopDrip { get; private set; }
    public bool HasBottomThreshold { get; private set; }
    public bool ConnectsToPreviousAsCorner { get; private set; }
    public decimal? CornerJointAngleDeg { get; private set; }
    public bool CornerUsesPost { get; private set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected ProjectTemplateRunPreset() { }

    public ProjectTemplateRunPreset(
        Guid templateId,
        int orderIndex,
        string labelKey,
        int lengthMm,
        int heightMm,
        int defaultPanelCount,
        int defaultPanelWidthMm,
        GlassOpeningType defaultOpeningType,
        decimal originX = 0m,
        decimal originY = 0m,
        decimal rotationDeg = 0m,
        bool hasTopDrip = false,
        bool hasBottomThreshold = false,
        bool connectsToPreviousAsCorner = false,
        decimal? cornerJointAngleDeg = null,
        bool cornerUsesPost = false)
    {
        TemplateId = templateId;
        OrderIndex = orderIndex;
        LabelKey = labelKey;
        LengthMm = lengthMm;
        HeightMm = heightMm;
        DefaultPanelCount = defaultPanelCount;
        DefaultPanelWidthMm = defaultPanelWidthMm;
        DefaultOpeningType = defaultOpeningType;
        OriginX = originX;
        OriginY = originY;
        RotationDeg = rotationDeg;
        HasTopDrip = hasTopDrip;
        HasBottomThreshold = hasBottomThreshold;
        ConnectsToPreviousAsCorner = connectsToPreviousAsCorner;
        CornerJointAngleDeg = cornerJointAngleDeg;
        CornerUsesPost = cornerUsesPost;
    }
}
