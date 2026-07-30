using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure;

/// <summary>
/// glass_project_panels.glass_type_id carries no FK, so a hard delete used to leave orphan panels
/// that the cutting report, the BOM and the technical summary all SILENTLY skipped — the plan just
/// came back short with no warning anywhere.
/// </summary>
public class DeleteGlassTypeGuardTests
{
    private readonly IGlassTypeRepository _glass = Substitute.For<IGlassTypeRepository>();
    private readonly IGlassProjectPanelRepository _panels = Substitute.For<IGlassProjectPanelRepository>();

    private DeleteGlassTypeCommandHandler CreateSut() => new(_glass, _panels);

    private static GlassType Glass() =>
        new("CAM-8", "8 mm temperli", 8, GlassStructure.Tempered, 500m, 20m, 2000m, 6m, 5.6m, 32m);

    [Fact]
    public async Task Deleting_a_glass_type_that_panels_reference_is_rejected()
    {
        var glass = Glass();
        _glass.GetByIdAsync(glass.Id, Arg.Any<CancellationToken>()).Returns(glass);
        _panels.AnyUsesGlassTypeAsync(glass.Id, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateSut().Handle(new DeleteGlassTypeCommand(glass.Id), default);

        await act.Should().ThrowAsync<GlassTypeInUseException>();
        _glass.DidNotReceive().Remove(Arg.Any<GlassType>());
    }

    [Fact]
    public async Task An_unused_glass_type_still_deletes()
    {
        var glass = Glass();
        _glass.GetByIdAsync(glass.Id, Arg.Any<CancellationToken>()).Returns(glass);
        _panels.AnyUsesGlassTypeAsync(glass.Id, Arg.Any<CancellationToken>()).Returns(false);

        await CreateSut().Handle(new DeleteGlassTypeCommand(glass.Id), default);

        _glass.Received(1).Remove(glass);
    }

    [Fact]
    public async Task A_missing_glass_type_is_a_not_found_and_never_probes_panels()
    {
        _glass.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((GlassType?)null);

        var act = () => CreateSut().Handle(new DeleteGlassTypeCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<GlassEnclosureNotFoundException>();
        await _panels.DidNotReceive().AnyUsesGlassTypeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
