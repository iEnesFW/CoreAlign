using CoreAlign.Application.GlassEnclosure.Presets;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.GlassEnclosure.Presets;

public class TemplateRegistryTests
{
    private static ITemplateRegistry BuildRegistry() =>
        new TemplateRegistry(new IEnclosurePreset[]
        {
            new BalconyPreset(),
            new GreenhousePreset(),
            new ShowerCabinPreset(),
            new BalustradePreset(),
            new FramelessDoorPreset()
        });

    [Fact]
    public void All_returns_five_registered_presets()
    {
        var registry = BuildRegistry();

        registry.All.Should().HaveCount(5);
    }

    [Fact]
    public void Resolve_returns_matching_preset_for_balcony()
    {
        var registry = BuildRegistry();

        var preset = registry.Resolve(EnclosureSubtype.Balcony);

        preset.Should().BeOfType<BalconyPreset>();
        preset.Subtype.Should().Be(EnclosureSubtype.Balcony);
    }

    [Fact]
    public void Resolve_throws_for_unregistered_subtype()
    {
        var registry = BuildRegistry();

        var act = () => registry.Resolve(EnclosureSubtype.SpiderFacade);

        act.Should().Throw<EnclosurePresetNotFoundException>()
            .Which.Subtype.Should().Be(EnclosureSubtype.SpiderFacade);
    }

    [Fact]
    public void ListByCategory_Functional_returns_three_presets()
    {
        var registry = BuildRegistry();

        var functional = registry.ListByCategory(EnclosureCategory.Functional);

        functional.Should().HaveCount(3);
        functional.Select(p => p.Subtype).Should().BeEquivalentTo(new[]
        {
            EnclosureSubtype.ShowerCabin,
            EnclosureSubtype.Balustrade,
            EnclosureSubtype.FramelessDoor
        });
    }

    [Fact]
    public void ListByCategory_Vertical_returns_only_balcony()
    {
        var registry = BuildRegistry();

        var vertical = registry.ListByCategory(EnclosureCategory.Vertical);

        vertical.Should().ContainSingle()
            .Which.Subtype.Should().Be(EnclosureSubtype.Balcony);
    }
}
