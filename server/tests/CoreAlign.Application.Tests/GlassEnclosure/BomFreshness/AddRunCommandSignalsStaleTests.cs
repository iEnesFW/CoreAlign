using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.BomFreshness;

public class AddRunCommandSignalsStaleTests
{
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly IGlassProjectRunRepository _runRepo = Substitute.For<IGlassProjectRunRepository>();
    private readonly IGlassProjectPanelRepository _panelRepo = Substitute.For<IGlassProjectPanelRepository>();
    private readonly IProfileSystemRepository _profileSystemRepo = Substitute.For<IProfileSystemRepository>();
    private readonly IGlassTypeRepository _glassTypeRepo = Substitute.For<IGlassTypeRepository>();
    private readonly IBomStaleSignal _bomStaleSignal = Substitute.For<IBomStaleSignal>();

    [Fact]
    public async Task AddRunCommandHandler_signals_run_changed_on_success()
    {
        var project = BuildProject();
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var handler = new AddRunCommandHandler(_projectRepo, _runRepo, _profileSystemRepo, _glassTypeRepo, _bomStaleSignal);
        var dto = new AddRunDto(
            LengthMm: 2000,
            HeightMm: 2100,
            ProfileSystemId: Guid.NewGuid(),
            OriginX: 0m,
            OriginY: 0m,
            RotationDeg: 0m,
            Label: "R1",
            ColorId: null,
            HasTopDrip: false,
            HasBottomThreshold: false,
            Notes: null);

        await handler.Handle(new AddRunCommand(project.Id, dto), default);

        await _bomStaleSignal.Received(1).SignalStaleAsync(project.Id, BomStaleReason.RunChanged, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPanelCommandHandler_signals_panel_changed_on_success()
    {
        var projectId = Guid.NewGuid();
        var run = new GlassProjectRun(projectId, orderIndex: 0, label: "R1",
            lengthMm: 2000, heightMm: 2100, profileSystemId: Guid.NewGuid());
        _runRepo.GetByIdWithPanelsAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var handler = new AddPanelCommandHandler(_runRepo, _panelRepo, _bomStaleSignal);
        var dto = new AddPanelDto(
            WidthMm: 500,
            OpeningType: GlassOpeningType.Fixed,
            GlassTypeId: Guid.NewGuid(),
            HasHandle: false,
            HasLock: false,
            HasBrushSeal: false,
            Notes: null);

        await handler.Handle(new AddPanelCommand(projectId, run.Id, dto), default);

        await _bomStaleSignal.Received(1).SignalStaleAsync(projectId, BomStaleReason.PanelChanged, Arg.Any<CancellationToken>());
    }

    private static GlassProject BuildProject()
    {
        return new GlassProject(
            code: "PRJ-1",
            customerId: Guid.NewGuid(),
            projectName: "Signal Stale",
            createdByUserId: Guid.NewGuid());
    }
}
