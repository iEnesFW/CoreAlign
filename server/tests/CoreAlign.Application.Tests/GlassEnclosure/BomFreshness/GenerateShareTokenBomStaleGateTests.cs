using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.BomFreshness;

public class GenerateShareTokenBomStaleGateTests
{
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly IGlassProjectSceneRepository _sceneRepo = Substitute.For<IGlassProjectSceneRepository>();
    private readonly IGlassProjectShareTokenRepository _tokenRepo = Substitute.For<IGlassProjectShareTokenRepository>();
    private readonly IGlassEnclosureSettingsRepository _settingsRepo = Substitute.For<IGlassEnclosureSettingsRepository>();
    private readonly IShareTokenService _generator = Substitute.For<IShareTokenService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IStockAvailabilityService _availabilityService = Substitute.For<IStockAvailabilityService>();
    private readonly GenerateShareTokenCommandHandler _sut;

    public GenerateShareTokenBomStaleGateTests()
    {
        _sceneRepo.GetLatestAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GlassProjectScene?)null);
        _settingsRepo.GetOrCreateForCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new GlassEnclosureSettings(Guid.NewGuid()));
        _generator.GenerateToken().Returns("token-abc-123");
        _currentUser.UserId.Returns(Guid.NewGuid());
        _availabilityService.CheckAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        _sut = new GenerateShareTokenCommandHandler(
            _projectRepo, _sceneRepo, _tokenRepo, _settingsRepo, _generator, _currentUser, _availabilityService);
    }

    [Fact]
    public async Task Throws_when_bom_is_stale_and_force_flag_is_false()
    {
        var project = BuildProjectWithScene();
        project.MarkBomStale(BomStaleReason.GlassChanged.ToString(), DateTime.UtcNow);
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var act = async () => await _sut.Handle(
            new GenerateShareTokenCommand(project.Id, new GenerateShareTokenDto(null), ForceWithShortage: false, ForceWithStaleBom: false),
            default);

        var ex = await act.Should().ThrowAsync<BomStaleBlocksShareException>();
        ex.Which.StaleReason.Should().Be(BomStaleReason.GlassChanged.ToString());
    }

    [Fact]
    public async Task Succeeds_when_bom_is_stale_and_force_flag_is_true()
    {
        var project = BuildProjectWithScene();
        project.MarkBomStale(BomStaleReason.HardwareChanged.ToString(), DateTime.UtcNow);
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var result = await _sut.Handle(
            new GenerateShareTokenCommand(project.Id, new GenerateShareTokenDto(null), ForceWithShortage: false, ForceWithStaleBom: true),
            default);

        result.Should().NotBeNull();
        result.Token.Should().Be("token-abc-123");
    }

    [Fact]
    public async Task Succeeds_when_bom_is_fresh()
    {
        var project = BuildProjectWithScene();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var result = await _sut.Handle(
            new GenerateShareTokenCommand(project.Id, new GenerateShareTokenDto(null), ForceWithShortage: false, ForceWithStaleBom: false),
            default);

        result.Should().NotBeNull();
        result.Token.Should().Be("token-abc-123");
    }

    private static GlassProject BuildProjectWithScene()
    {
        var project = new GlassProject(
            code: "PRJ-1",
            customerId: Guid.NewGuid(),
            projectName: "Share Stale Gate",
            createdByUserId: Guid.NewGuid());
        project.AdvanceSceneVersion(1);
        project.TransitionTo(GlassProjectStatus.Quoted, Guid.NewGuid());
        return project;
    }
}
