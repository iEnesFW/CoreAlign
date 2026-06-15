using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.Commands;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.WorkOrderRevisions;

public class WorkOrderRevisionCommandTests
{
    private readonly IWorkOrderRevisionService _service = Substitute.For<IWorkOrderRevisionService>();
    private readonly IGlassWorkOrderRevisionRepository _revisions = Substitute.For<IGlassWorkOrderRevisionRepository>();

    private static GlassWorkOrderRevision BuildRevision(Guid workOrderId)
    {
        return new GlassWorkOrderRevision(
            workOrderId,
            revisionNumber: 1,
            previousSnapshotJson: null,
            newSnapshotJson: "{}",
            deltaJson: "{}",
            deltaPercent: 7m,
            reason: "auto",
            status: WorkOrderRevisionStatus.PendingApproval,
            createdByUserId: Guid.NewGuid());
    }

    [Fact]
    public async Task Approve_handler_calls_service_when_ParentWorkOrderId_matches()
    {
        var workOrderId = Guid.NewGuid();
        var revision = BuildRevision(workOrderId);
        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);

        var handler = new ApproveWorkOrderRevisionCommandHandler(_service, _revisions);
        var command = new ApproveWorkOrderRevisionCommand(revision.Id, null) { ParentWorkOrderId = workOrderId };

        await handler.Handle(command, CancellationToken.None);

        await _service.Received(1).ApproveRevisionAsync(revision.Id, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_handler_throws_when_ParentWorkOrderId_does_not_match()
    {
        var revisionWorkOrderId = Guid.NewGuid();
        var foreignWorkOrderId = Guid.NewGuid();
        var revision = BuildRevision(revisionWorkOrderId);
        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);

        var handler = new ApproveWorkOrderRevisionCommandHandler(_service, _revisions);
        var command = new ApproveWorkOrderRevisionCommand(revision.Id, null) { ParentWorkOrderId = foreignWorkOrderId };

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<GlassWorkOrderRevisionMismatchException>();
        await _service.DidNotReceive().ApproveRevisionAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_handler_throws_when_revision_missing_with_ParentWorkOrderId_provided()
    {
        var revisionId = Guid.NewGuid();
        _revisions.GetByIdAsync(revisionId, Arg.Any<CancellationToken>()).Returns((GlassWorkOrderRevision?)null);

        var handler = new ApproveWorkOrderRevisionCommandHandler(_service, _revisions);
        var command = new ApproveWorkOrderRevisionCommand(revisionId, null) { ParentWorkOrderId = Guid.NewGuid() };

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<GlassWorkOrderRevisionNotFoundException>();
    }

    [Fact]
    public async Task Approve_handler_skips_lookup_when_ParentWorkOrderId_not_supplied()
    {
        var revisionId = Guid.NewGuid();

        var handler = new ApproveWorkOrderRevisionCommandHandler(_service, _revisions);
        var command = new ApproveWorkOrderRevisionCommand(revisionId, "override");

        await handler.Handle(command, CancellationToken.None);

        await _revisions.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _service.Received(1).ApproveRevisionAsync(revisionId, "override", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_handler_throws_when_ParentWorkOrderId_does_not_match()
    {
        var revisionWorkOrderId = Guid.NewGuid();
        var foreignWorkOrderId = Guid.NewGuid();
        var revision = BuildRevision(revisionWorkOrderId);
        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);

        var handler = new RejectWorkOrderRevisionCommandHandler(_service, _revisions);
        var command = new RejectWorkOrderRevisionCommand(revision.Id, "no") { ParentWorkOrderId = foreignWorkOrderId };

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<GlassWorkOrderRevisionMismatchException>();
        await _service.DidNotReceive().RejectRevisionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_handler_calls_service_when_ParentWorkOrderId_matches()
    {
        var workOrderId = Guid.NewGuid();
        var revision = BuildRevision(workOrderId);
        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);

        var handler = new RejectWorkOrderRevisionCommandHandler(_service, _revisions);
        var command = new RejectWorkOrderRevisionCommand(revision.Id, "bad") { ParentWorkOrderId = workOrderId };

        await handler.Handle(command, CancellationToken.None);

        await _service.Received(1).RejectRevisionAsync(revision.Id, "bad", Arg.Any<CancellationToken>());
    }
}
