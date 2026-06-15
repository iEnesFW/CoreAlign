using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public sealed class FramelessDoorPreset : IEnclosurePreset
{
    public EnclosureSubtype Subtype => EnclosureSubtype.FramelessDoor;
    public EnclosureCategory Category => EnclosureCategory.Functional;
    public GeometryMode DefaultGeometryMode => GeometryMode.Planar;
    public MountingTopology DefaultMountingTopology => MountingTopology.PatchFitting;
    public ConnectorKind DefaultConnectorKind => ConnectorKind.PatchFitting;

    public EnclosureDefaults BuildDefaults() =>
        new(DefaultPanelWidthMm: 900, DefaultPanelHeightMm: 2100, DefaultPanelCount: 1, DefaultRoofPitchDeg: null, Notes: null);

    public EnclosureValidationResult Validate(EnclosureConfigurationInput input)
    {
        var issues = new List<EnclosureValidationIssue>();
        if (input.MountingTopology == MountingTopology.ProfileFramed)
        {
            issues.Add(new EnclosureValidationIssue("MountingTopology", "FramelessDoor.NotProfileFramed", EnclosureValidationSeverity.Error));
        }
        return new EnclosureValidationResult(issues.All(i => i.Severity != EnclosureValidationSeverity.Error), issues);
    }
}
