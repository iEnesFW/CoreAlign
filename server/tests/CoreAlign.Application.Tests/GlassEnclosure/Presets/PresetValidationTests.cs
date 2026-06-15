using CoreAlign.Application.GlassEnclosure.Presets;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.GlassEnclosure.Presets;

public class PresetValidationTests
{
    [Fact]
    public void Greenhouse_pitched_without_roof_pitch_is_invalid()
    {
        var preset = new GreenhousePreset();
        var input = new EnclosureConfigurationInput(
            Category: EnclosureCategory.HorizontalOrPitched,
            Subtype: EnclosureSubtype.Greenhouse,
            GeometryMode: GeometryMode.Pitched,
            MountingTopology: MountingTopology.RoofAnchored,
            RoofPitchDeg: null,
            RidgeHeightMm: 3000,
            EaveHeightMm: 2200);

        var result = preset.Validate(input);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().ContainSingle(i =>
            i.MessageKey == "Greenhouse.RoofPitchRequired" &&
            i.Severity == EnclosureValidationSeverity.Error);
    }

    [Fact]
    public void Greenhouse_pitched_with_valid_roof_pitch_is_valid()
    {
        var preset = new GreenhousePreset();
        var input = new EnclosureConfigurationInput(
            Category: EnclosureCategory.HorizontalOrPitched,
            Subtype: EnclosureSubtype.Greenhouse,
            GeometryMode: GeometryMode.Pitched,
            MountingTopology: MountingTopology.RoofAnchored,
            RoofPitchDeg: 15m,
            RidgeHeightMm: 3000,
            EaveHeightMm: 2200);

        var result = preset.Validate(input);

        result.IsValid.Should().BeTrue();
        result.Issues.Should().NotContain(i => i.Severity == EnclosureValidationSeverity.Error);
    }

    [Fact]
    public void Greenhouse_pitch_out_of_range_emits_warning()
    {
        var preset = new GreenhousePreset();
        var input = new EnclosureConfigurationInput(
            Category: EnclosureCategory.HorizontalOrPitched,
            Subtype: EnclosureSubtype.Greenhouse,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.RoofAnchored,
            RoofPitchDeg: 50m,
            RidgeHeightMm: null,
            EaveHeightMm: null);

        var result = preset.Validate(input);

        result.IsValid.Should().BeTrue();
        result.Issues.Should().Contain(i =>
            i.MessageKey == "Greenhouse.PitchRange5to45" &&
            i.Severity == EnclosureValidationSeverity.Warning);
    }

    [Fact]
    public void Greenhouse_pitched_with_ridge_below_eave_is_invalid()
    {
        var preset = new GreenhousePreset();
        var input = new EnclosureConfigurationInput(
            Category: EnclosureCategory.HorizontalOrPitched,
            Subtype: EnclosureSubtype.Greenhouse,
            GeometryMode: GeometryMode.Pitched,
            MountingTopology: MountingTopology.RoofAnchored,
            RoofPitchDeg: 15m,
            RidgeHeightMm: 2200,
            EaveHeightMm: 2500);

        var result = preset.Validate(input);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(i =>
            i.MessageKey == "Greenhouse.RidgeMustExceedEave" &&
            i.Severity == EnclosureValidationSeverity.Error);
    }

    [Fact]
    public void FramelessDoor_with_profile_framed_is_invalid()
    {
        var preset = new FramelessDoorPreset();
        var input = new EnclosureConfigurationInput(
            Category: EnclosureCategory.Functional,
            Subtype: EnclosureSubtype.FramelessDoor,
            GeometryMode: GeometryMode.Planar,
            MountingTopology: MountingTopology.ProfileFramed,
            RoofPitchDeg: null,
            RidgeHeightMm: null,
            EaveHeightMm: null);

        var result = preset.Validate(input);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().ContainSingle(i =>
            i.MessageKey == "FramelessDoor.NotProfileFramed" &&
            i.Severity == EnclosureValidationSeverity.Error);
    }

    [Fact]
    public void Balcony_with_curved_geometry_is_invalid()
    {
        var preset = new BalconyPreset();
        var input = new EnclosureConfigurationInput(
            Category: EnclosureCategory.Vertical,
            Subtype: EnclosureSubtype.Balcony,
            GeometryMode: GeometryMode.Curved,
            MountingTopology: MountingTopology.ProfileFramed,
            RoofPitchDeg: null,
            RidgeHeightMm: null,
            EaveHeightMm: null);

        var result = preset.Validate(input);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().ContainSingle(i =>
            i.MessageKey == "Balcony.GeometryMustBePlanarOrMultiLevel" &&
            i.Severity == EnclosureValidationSeverity.Error);
    }
}
