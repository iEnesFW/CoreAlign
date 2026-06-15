using CoreAlign.Application.GlassEnclosure.Presets;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.GlassEnclosure.Presets;

public class PresetDefaultsTests
{
    [Fact]
    public void Balcony_defaults_match_snapshot()
    {
        var preset = new BalconyPreset();

        var defaults = preset.BuildDefaults();

        preset.Subtype.Should().Be(EnclosureSubtype.Balcony);
        preset.Category.Should().Be(EnclosureCategory.Vertical);
        preset.DefaultGeometryMode.Should().Be(GeometryMode.Planar);
        preset.DefaultMountingTopology.Should().Be(MountingTopology.ProfileFramed);
        preset.DefaultConnectorKind.Should().Be(ConnectorKind.Profile);
        defaults.DefaultPanelWidthMm.Should().Be(800);
        defaults.DefaultPanelHeightMm.Should().Be(2400);
        defaults.DefaultPanelCount.Should().Be(4);
        defaults.DefaultRoofPitchDeg.Should().BeNull();
    }

    [Fact]
    public void Greenhouse_defaults_match_snapshot()
    {
        var preset = new GreenhousePreset();

        var defaults = preset.BuildDefaults();

        preset.Subtype.Should().Be(EnclosureSubtype.Greenhouse);
        preset.Category.Should().Be(EnclosureCategory.HorizontalOrPitched);
        preset.DefaultGeometryMode.Should().Be(GeometryMode.Pitched);
        preset.DefaultMountingTopology.Should().Be(MountingTopology.RoofAnchored);
        preset.DefaultConnectorKind.Should().Be(ConnectorKind.Profile);
        defaults.DefaultPanelWidthMm.Should().Be(1000);
        defaults.DefaultPanelHeightMm.Should().Be(2000);
        defaults.DefaultPanelCount.Should().Be(6);
        defaults.DefaultRoofPitchDeg.Should().Be(15m);
    }

    [Fact]
    public void ShowerCabin_defaults_match_snapshot()
    {
        var preset = new ShowerCabinPreset();

        var defaults = preset.BuildDefaults();

        preset.Subtype.Should().Be(EnclosureSubtype.ShowerCabin);
        preset.Category.Should().Be(EnclosureCategory.Functional);
        preset.DefaultGeometryMode.Should().Be(GeometryMode.Planar);
        preset.DefaultMountingTopology.Should().Be(MountingTopology.ChannelBase);
        preset.DefaultConnectorKind.Should().Be(ConnectorKind.GlassClamp);
        defaults.DefaultPanelWidthMm.Should().Be(900);
        defaults.DefaultPanelHeightMm.Should().Be(1950);
        defaults.DefaultPanelCount.Should().Be(2);
        defaults.DefaultRoofPitchDeg.Should().BeNull();
    }

    [Fact]
    public void Balustrade_defaults_match_snapshot()
    {
        var preset = new BalustradePreset();

        var defaults = preset.BuildDefaults();

        preset.Subtype.Should().Be(EnclosureSubtype.Balustrade);
        preset.Category.Should().Be(EnclosureCategory.Functional);
        preset.DefaultGeometryMode.Should().Be(GeometryMode.Planar);
        preset.DefaultMountingTopology.Should().Be(MountingTopology.ChannelBase);
        preset.DefaultConnectorKind.Should().Be(ConnectorKind.GlassClamp);
        defaults.DefaultPanelWidthMm.Should().Be(1200);
        defaults.DefaultPanelHeightMm.Should().Be(1100);
        defaults.DefaultPanelCount.Should().Be(5);
        defaults.DefaultRoofPitchDeg.Should().BeNull();
    }

    [Fact]
    public void FramelessDoor_defaults_match_snapshot()
    {
        var preset = new FramelessDoorPreset();

        var defaults = preset.BuildDefaults();

        preset.Subtype.Should().Be(EnclosureSubtype.FramelessDoor);
        preset.Category.Should().Be(EnclosureCategory.Functional);
        preset.DefaultGeometryMode.Should().Be(GeometryMode.Planar);
        preset.DefaultMountingTopology.Should().Be(MountingTopology.PatchFitting);
        preset.DefaultConnectorKind.Should().Be(ConnectorKind.PatchFitting);
        defaults.DefaultPanelWidthMm.Should().Be(900);
        defaults.DefaultPanelHeightMm.Should().Be(2100);
        defaults.DefaultPanelCount.Should().Be(1);
        defaults.DefaultRoofPitchDeg.Should().BeNull();
    }
}
