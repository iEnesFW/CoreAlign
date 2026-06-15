using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public interface IEnclosurePreset
{
    EnclosureSubtype Subtype { get; }
    EnclosureCategory Category { get; }
    GeometryMode DefaultGeometryMode { get; }
    MountingTopology DefaultMountingTopology { get; }
    ConnectorKind DefaultConnectorKind { get; }

    EnclosureDefaults BuildDefaults();
    EnclosureValidationResult Validate(EnclosureConfigurationInput input);
}

public sealed record EnclosureDefaults(
    int? DefaultPanelWidthMm,
    int? DefaultPanelHeightMm,
    int? DefaultPanelCount,
    decimal? DefaultRoofPitchDeg,
    string? Notes);

public sealed record EnclosureConfigurationInput(
    EnclosureCategory Category,
    EnclosureSubtype Subtype,
    GeometryMode GeometryMode,
    MountingTopology MountingTopology,
    decimal? RoofPitchDeg,
    int? RidgeHeightMm,
    int? EaveHeightMm);

public sealed record EnclosureValidationResult(
    bool IsValid,
    IReadOnlyList<EnclosureValidationIssue> Issues);

public sealed record EnclosureValidationIssue(
    string FieldKey,
    string MessageKey,
    EnclosureValidationSeverity Severity);

public enum EnclosureValidationSeverity
{
    Warning,
    Error
}
