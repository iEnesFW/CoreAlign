using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public sealed class CurtainWallPreset : IEnclosurePreset
{
    public EnclosureSubtype Subtype => EnclosureSubtype.CurtainWall;
    public EnclosureCategory Category => EnclosureCategory.Vertical;
    public GeometryMode DefaultGeometryMode => GeometryMode.Planar;
    public MountingTopology DefaultMountingTopology => MountingTopology.ProfileFramed;
    public ConnectorKind DefaultConnectorKind => ConnectorKind.Profile;

    public EnclosureDefaults BuildDefaults() =>
        new(DefaultPanelWidthMm: 1500, DefaultPanelHeightMm: 3000, DefaultPanelCount: 8, DefaultRoofPitchDeg: null, Notes: "CurtainWall.CassetteSystem");

    public EnclosureValidationResult Validate(EnclosureConfigurationInput input)
    {
        var issues = new List<EnclosureValidationIssue>();
        if (input.GeometryMode != GeometryMode.Planar && input.GeometryMode != GeometryMode.MultiLevel)
        {
            issues.Add(new EnclosureValidationIssue("GeometryMode", "CurtainWall.GeometryMustBePlanarOrMultiLevel", EnclosureValidationSeverity.Error));
        }
        if (input.RidgeHeightMm is int height)
        {
            if (height < 2000)
            {
                issues.Add(new EnclosureValidationIssue("RidgeHeightMm", "CurtainWall.HeightMin2m", EnclosureValidationSeverity.Error));
            }
            if (height > 6000)
            {
                issues.Add(new EnclosureValidationIssue("RidgeHeightMm", "CurtainWall.HeightMax6m", EnclosureValidationSeverity.Warning));
            }
        }
        return new EnclosureValidationResult(issues.All(i => i.Severity != EnclosureValidationSeverity.Error), issues);
    }
}
