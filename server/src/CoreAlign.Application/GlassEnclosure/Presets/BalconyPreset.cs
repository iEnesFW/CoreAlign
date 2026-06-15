using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public sealed class BalconyPreset : IEnclosurePreset
{
    public EnclosureSubtype Subtype => EnclosureSubtype.Balcony;
    public EnclosureCategory Category => EnclosureCategory.Vertical;
    public GeometryMode DefaultGeometryMode => GeometryMode.Planar;
    public MountingTopology DefaultMountingTopology => MountingTopology.ProfileFramed;
    public ConnectorKind DefaultConnectorKind => ConnectorKind.Profile;

    public EnclosureDefaults BuildDefaults() =>
        new(DefaultPanelWidthMm: 800, DefaultPanelHeightMm: 2400, DefaultPanelCount: 4, DefaultRoofPitchDeg: null, Notes: null);

    public EnclosureValidationResult Validate(EnclosureConfigurationInput input)
    {
        var issues = new List<EnclosureValidationIssue>();
        if (input.GeometryMode != GeometryMode.Planar && input.GeometryMode != GeometryMode.MultiLevel)
        {
            issues.Add(new EnclosureValidationIssue("GeometryMode", "Balcony.GeometryMustBePlanarOrMultiLevel", EnclosureValidationSeverity.Error));
        }
        return new EnclosureValidationResult(issues.All(i => i.Severity != EnclosureValidationSeverity.Error), issues);
    }
}
