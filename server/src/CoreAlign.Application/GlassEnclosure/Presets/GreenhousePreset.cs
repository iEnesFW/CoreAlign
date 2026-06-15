using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public sealed class GreenhousePreset : IEnclosurePreset
{
    public EnclosureSubtype Subtype => EnclosureSubtype.Greenhouse;
    public EnclosureCategory Category => EnclosureCategory.HorizontalOrPitched;
    public GeometryMode DefaultGeometryMode => GeometryMode.Pitched;
    public MountingTopology DefaultMountingTopology => MountingTopology.RoofAnchored;
    public ConnectorKind DefaultConnectorKind => ConnectorKind.Profile;

    public EnclosureDefaults BuildDefaults() =>
        new(DefaultPanelWidthMm: 1000, DefaultPanelHeightMm: 2000, DefaultPanelCount: 6, DefaultRoofPitchDeg: 15m, Notes: "Pitched.Notes");

    public EnclosureValidationResult Validate(EnclosureConfigurationInput input)
    {
        var issues = new List<EnclosureValidationIssue>();
        var isPitched = input.GeometryMode == GeometryMode.Pitched;

        if (isPitched && !input.RoofPitchDeg.HasValue)
        {
            issues.Add(new EnclosureValidationIssue("RoofPitchDeg", "Greenhouse.RoofPitchRequired", EnclosureValidationSeverity.Error));
        }
        if (input.RoofPitchDeg is decimal pitch)
        {
            if (pitch < 5m || pitch > 45m)
            {
                issues.Add(new EnclosureValidationIssue("RoofPitchDeg", "Greenhouse.PitchRange5to45", EnclosureValidationSeverity.Warning));
            }
            if (isPitched && (pitch < 10m || pitch > 45m))
            {
                issues.Add(new EnclosureValidationIssue("RoofPitchDeg", "Greenhouse.PitchedRange10to45", EnclosureValidationSeverity.Error));
            }
        }

        if (isPitched)
        {
            if (!input.RidgeHeightMm.HasValue)
            {
                issues.Add(new EnclosureValidationIssue("RidgeHeightMm", "Greenhouse.RidgeHeightRequired", EnclosureValidationSeverity.Error));
            }
            if (!input.EaveHeightMm.HasValue)
            {
                issues.Add(new EnclosureValidationIssue("EaveHeightMm", "Greenhouse.EaveHeightRequired", EnclosureValidationSeverity.Error));
            }
            if (input.RidgeHeightMm is int ridge && input.EaveHeightMm is int eave)
            {
                if (ridge <= eave)
                {
                    issues.Add(new EnclosureValidationIssue("RidgeHeightMm", "Greenhouse.RidgeMustExceedEave", EnclosureValidationSeverity.Error));
                }
            }
        }

        return new EnclosureValidationResult(issues.All(i => i.Severity != EnclosureValidationSeverity.Error), issues);
    }
}
