using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class RemoveRunHandlerTests
{
    private readonly IGlassProjectRunRepository _runRepo = Substitute.For<IGlassProjectRunRepository>();
    private readonly IRunConnectionRepository _connectionRepo = Substitute.For<IRunConnectionRepository>();
    private readonly IBomStaleSignal _bomStaleSignal = Substitute.For<IBomStaleSignal>();

    private RemoveRunCommandHandler CreateSut() => new(_runRepo, _connectionRepo, _bomStaleSignal);

    [Fact]
    public async Task Removing_missing_run_succeeds_without_mutation()
    {
        _runRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GlassProjectRun?)null);

        var act = () => CreateSut().Handle(new RemoveRunCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().NotThrowAsync();
        _runRepo.DidNotReceive().Remove(Arg.Any<GlassProjectRun>());
        await _connectionRepo.DidNotReceive().ListByProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Removing_run_deletes_only_connections_referencing_it()
    {
        var projectId = Guid.NewGuid();
        var run = new GlassProjectRun(projectId, 0, "Run", 3000, 2400, Guid.NewGuid());
        var referencing = new RunConnection(projectId, run.Id, Guid.NewGuid(), 90m, 45m, true);
        var unrelated = new RunConnection(projectId, Guid.NewGuid(), Guid.NewGuid(), 90m, 45m, true);
        _runRepo.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        _connectionRepo.ListByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new List<RunConnection> { referencing, unrelated });

        await CreateSut().Handle(new RemoveRunCommand(projectId, run.Id), default);

        _connectionRepo.Received(1).Remove(referencing);
        _connectionRepo.DidNotReceive().Remove(unrelated);
        _runRepo.Received(1).Remove(run);
    }
}
