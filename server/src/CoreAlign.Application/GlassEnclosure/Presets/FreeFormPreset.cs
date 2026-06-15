using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public sealed class FreeFormPreset : IEnclosurePreset
{
    public EnclosureSubtype Subtype => EnclosureSubtype.FreeForm;
    public EnclosureCategory Category => EnclosureCategory.Special;
    public GeometryMode DefaultGeometryMode => GeometryMode.FreeForm;
    public MountingTopology DefaultMountingTopology => MountingTopology.SelfSupporting;
    public ConnectorKind DefaultConnectorKind => ConnectorKind.StructuralSilicone;

    public EnclosureDefaults BuildDefaults() =>
        new(DefaultPanelWidthMm: null, DefaultPanelHeightMm: null, DefaultPanelCount: null, DefaultRoofPitchDeg: null, Notes: "FreeForm.UserDefined");

    public EnclosureValidationResult Validate(EnclosureConfigurationInput input)
    {
        var issues = new List<EnclosureValidationIssue>();
        if (input.GeometryMode != GeometryMode.FreeForm)
        {
            issues.Add(new EnclosureValidationIssue("GeometryMode", "FreeForm.GeometryMustBeFreeForm", EnclosureValidationSeverity.Warning));
        }
        return new EnclosureValidationResult(issues.All(i => i.Severity != EnclosureValidationSeverity.Error), issues);
    }
}
