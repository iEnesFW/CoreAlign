using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public sealed class ShowerCabinPreset : IEnclosurePreset
{
    public EnclosureSubtype Subtype => EnclosureSubtype.ShowerCabin;
    public EnclosureCategory Category => EnclosureCategory.Functional;
    public GeometryMode DefaultGeometryMode => GeometryMode.Planar;
    public MountingTopology DefaultMountingTopology => MountingTopology.ChannelBase;
    public ConnectorKind DefaultConnectorKind => ConnectorKind.GlassClamp;

    public EnclosureDefaults BuildDefaults() =>
        new(DefaultPanelWidthMm: 900, DefaultPanelHeightMm: 1950, DefaultPanelCount: 2, DefaultRoofPitchDeg: null, Notes: null);

    public EnclosureValidationResult Validate(EnclosureConfigurationInput input)
    {
        var issues = new List<EnclosureValidationIssue>();
        if (input.MountingTopology != MountingTopology.ChannelBase && input.MountingTopology != MountingTopology.PatchFitting)
        {
            issues.Add(new EnclosureValidationIssue("MountingTopology", "ShowerCabin.UseChannelOrPatch", EnclosureValidationSeverity.Warning));
        }
        return new EnclosureValidationResult(issues.All(i => i.Severity != EnclosureValidationSeverity.Error), issues);
    }
}
