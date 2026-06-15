using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public sealed class BalustradePreset : IEnclosurePreset
{
    public EnclosureSubtype Subtype => EnclosureSubtype.Balustrade;
    public EnclosureCategory Category => EnclosureCategory.Functional;
    public GeometryMode DefaultGeometryMode => GeometryMode.Planar;
    public MountingTopology DefaultMountingTopology => MountingTopology.ChannelBase;
    public ConnectorKind DefaultConnectorKind => ConnectorKind.GlassClamp;

    public EnclosureDefaults BuildDefaults() =>
        new(DefaultPanelWidthMm: 1200, DefaultPanelHeightMm: 1100, DefaultPanelCount: 5, DefaultRoofPitchDeg: null, Notes: "Balustrade.HeightLimitNote");

    public EnclosureValidationResult Validate(EnclosureConfigurationInput input)
    {
        var issues = new List<EnclosureValidationIssue>();
        return new EnclosureValidationResult(true, issues);
    }
}
