using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public sealed class SpiderFacadePreset : IEnclosurePreset
{
    public EnclosureSubtype Subtype => EnclosureSubtype.SpiderFacade;
    public EnclosureCategory Category => EnclosureCategory.Vertical;
    public GeometryMode DefaultGeometryMode => GeometryMode.Planar;
    public MountingTopology DefaultMountingTopology => MountingTopology.SpiderArm;
    public ConnectorKind DefaultConnectorKind => ConnectorKind.SpiderFitting;

    public EnclosureDefaults BuildDefaults() =>
        new(DefaultPanelWidthMm: 1500, DefaultPanelHeightMm: 2000, DefaultPanelCount: 4, DefaultRoofPitchDeg: null, Notes: "SpiderFacade.FourPointFixing");

    public EnclosureValidationResult Validate(EnclosureConfigurationInput input)
    {
        var issues = new List<EnclosureValidationIssue>();
        if (input.MountingTopology != MountingTopology.SpiderArm)
        {
            issues.Add(new EnclosureValidationIssue("MountingTopology", "SpiderFacade.MustUseSpiderArm", EnclosureValidationSeverity.Error));
        }
        return new EnclosureValidationResult(issues.All(i => i.Severity != EnclosureValidationSeverity.Error), issues);
    }
}
