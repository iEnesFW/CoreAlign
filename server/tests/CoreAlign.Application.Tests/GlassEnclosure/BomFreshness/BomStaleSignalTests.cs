using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.GlassEnclosure.BomFreshness;

public class BomStaleSignalTests
{
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly BomStaleSignal _sut;

    public BomStaleSignalTests()
    {
        _sut = new BomStaleSignal(_projectRepo, NullLogger<BomStaleSignal>.Instance);
    }

    [Fact]
    public async Task SignalStaleAsync_sets_isBomStale_reason_and_stale_since()
    {
        var project = BuildProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        await _sut.SignalStaleAsync(project.Id, BomStaleReason.RunChanged);

        project.IsBomStale.Should().BeTrue();
        project.BomStaleReason.Should().Be(BomStaleReason.RunChanged.ToString());
        project.StaleSinceUtc.Should().NotBeNull();
        _projectRepo.Received(1).Update(project);
    }

    [Fact]
    public async Task SignalStaleAsync_when_project_not_found_does_not_throw()
    {
        var missingId = Guid.NewGuid();
        _projectRepo.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((GlassProject?)null);

        var act = async () => await _sut.SignalStaleAsync(missingId, BomStaleReason.PanelChanged);

        await act.Should().NotThrowAsync();
        _projectRepo.DidNotReceive().Update(Arg.Any<GlassProject>());
    }

    [Fact]
    public async Task SignalFreshAsync_clears_stale_flags()
    {
        var project = BuildProject();
        project.MarkBomStale(BomStaleReason.HardwareChanged.ToString(), DateTime.UtcNow);
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        await _sut.SignalFreshAsync(project.Id);

        project.IsBomStale.Should().BeFalse();
        project.BomStaleReason.Should().BeNull();
        project.StaleSinceUtc.Should().BeNull();
        _projectRepo.Received(1).Update(project);
    }

    [Fact]
    public async Task SignalStaleAsync_is_idempotent_for_stale_since_utc()
    {
        var project = BuildProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        await _sut.SignalStaleAsync(project.Id, BomStaleReason.RunChanged);
        var firstStaleSince = project.StaleSinceUtc;

        await Task.Delay(5);
        await _sut.SignalStaleAsync(project.Id, BomStaleReason.PanelChanged);

        project.StaleSinceUtc.Should().Be(firstStaleSince);
        project.BomStaleReason.Should().Be(BomStaleReason.PanelChanged.ToString());
    }

    private static GlassProject BuildProject()
    {
        return new GlassProject(
            code: "PRJ-1",
            customerId: Guid.NewGuid(),
            projectName: "Stale Test",
            createdByUserId: Guid.NewGuid());
    }
}
