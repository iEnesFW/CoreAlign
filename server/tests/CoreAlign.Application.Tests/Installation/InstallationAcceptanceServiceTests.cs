using CoreAlign.Application.Installation;
using CoreAlign.Application.Installation.Templates;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Entities.Installation;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Installation;

public class InstallationAcceptanceServiceTests
{
    private readonly IInstallationAcceptanceRepository _acceptances = Substitute.For<IInstallationAcceptanceRepository>();
    private readonly IPunchListRepository _punchList = Substitute.For<IPunchListRepository>();
    private readonly IGlassWorkOrderRepository _workOrders = Substitute.For<IGlassWorkOrderRepository>();
    private readonly IGlassProjectRepository _projects = Substitute.For<IGlassProjectRepository>();
    private readonly InstallationAcceptanceService _sut;

    public InstallationAcceptanceServiceTests()
    {
        _sut = new InstallationAcceptanceService(_acceptances, _punchList, _workOrders, _projects);
    }

    [Fact]
    public async Task StartAsync_creates_draft_acceptance_with_inspector_and_initial_checklist()
    {
        var workOrderId = Guid.NewGuid();
        var inspectorUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var workOrder = BuildWorkOrder();
        var project = BuildProject(customerId);

        _acceptances.GetByWorkOrderIdAsync(workOrderId, Arg.Any<CancellationToken>())
            .Returns((InstallationAcceptance?)null);
        _workOrders.GetByIdAsync(workOrderId, Arg.Any<CancellationToken>()).Returns(workOrder);
        _projects.GetByIdAsync(workOrder.ProjectId, Arg.Any<CancellationToken>()).Returns(project);

        var result = await _sut.StartAsync(workOrderId, inspectorUserId);

        result.Should().NotBeNull();
        result.Status.Should().Be(InstallationAcceptanceStatus.Draft);
        result.InspectorUserId.Should().Be(inspectorUserId);
        result.WorkOrderId.Should().Be(workOrderId);
        result.CustomerId.Should().Be(customerId);
        result.ChecklistJson.Should().NotBeNullOrEmpty();
        result.ChecklistJson.Should().NotBe("[]");
        await _acceptances.Received(1).AddAsync(result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_returns_existing_acceptance_when_already_started()
    {
        var workOrderId = Guid.NewGuid();
        var existing = BuildAcceptance(workOrderId);
        _acceptances.GetByWorkOrderIdAsync(workOrderId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.StartAsync(workOrderId, Guid.NewGuid());

        result.Should().BeSameAs(existing);
        await _acceptances.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task UpdateChecklistAsync_updates_targeted_item_in_checklist_json()
    {
        var acceptance = BuildAcceptance(Guid.NewGuid());
        _acceptances.GetByIdAsync(acceptance.Id, Arg.Any<CancellationToken>()).Returns(acceptance);

        await _sut.UpdateChecklistAsync(acceptance.Id, "Glass", "Glass.NoChips", InstallationChecklistResult.Pass, "OK");

        acceptance.ChecklistJson.Should().Contain("\"result\":\"Pass\"");
        acceptance.ChecklistJson.Should().Contain("\"notes\":\"OK\"");
        _acceptances.Received(1).Update(acceptance);
    }

    [Fact]
    public async Task CaptureSignatureAsync_sets_signature_file_and_customer_name()
    {
        var acceptance = BuildAcceptance(Guid.NewGuid());
        _acceptances.GetByIdAsync(acceptance.Id, Arg.Any<CancellationToken>()).Returns(acceptance);
        var fileId = Guid.NewGuid();

        await _sut.CaptureSignatureAsync(acceptance.Id, fileId, "John Doe");

        acceptance.CustomerSignatureFileId.Should().Be(fileId);
        acceptance.CustomerName.Should().Be("John Doe");
        acceptance.Status.Should().Be(InstallationAcceptanceStatus.SignedByCustomer);
        _acceptances.Received(1).Update(acceptance);
    }

    [Fact]
    public async Task AcceptAsync_moves_to_accepted_and_raises_installation_accepted_event()
    {
        var acceptance = BuildAcceptance(Guid.NewGuid());
        acceptance.CaptureSignature(Guid.NewGuid(), "Jane Doe");
        _acceptances.GetByIdAsync(acceptance.Id, Arg.Any<CancellationToken>()).Returns(acceptance);

        await _sut.AcceptAsync(acceptance.Id, null);

        acceptance.Status.Should().Be(InstallationAcceptanceStatus.Accepted);
        acceptance.CompletedAtUtc.Should().NotBeNull();
        acceptance.DomainEvents.Should().Contain(e => e is InstallationAcceptedEvent);
        _acceptances.Received(1).Update(acceptance);
    }

    [Fact]
    public async Task RejectAsync_moves_to_rejected_and_records_reason()
    {
        var acceptance = BuildAcceptance(Guid.NewGuid());
        _acceptances.GetByIdAsync(acceptance.Id, Arg.Any<CancellationToken>()).Returns(acceptance);

        await _sut.RejectAsync(acceptance.Id, "Glass panel cracked");

        acceptance.Status.Should().Be(InstallationAcceptanceStatus.Rejected);
        acceptance.RejectionReason.Should().Be("Glass panel cracked");
        acceptance.DomainEvents.Should().Contain(e => e is InstallationRejectedEvent);
        _acceptances.Received(1).Update(acceptance);
    }

    private static InstallationAcceptance BuildAcceptance(Guid workOrderId)
    {
        return new InstallationAcceptance(
            workOrderId: workOrderId,
            projectId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            inspectorUserId: Guid.NewGuid(),
            initialChecklistJson: StandardChecklist.BuildInitialChecklistJson());
    }

    private static GlassWorkOrder BuildWorkOrder()
    {
        return new GlassWorkOrder(
            projectId: Guid.NewGuid(),
            scheduledStartDate: DateTime.UtcNow,
            scheduledEndDate: DateTime.UtcNow.AddDays(1),
            workloadM2: 10m);
    }

    private static GlassProject BuildProject(Guid customerId)
    {
        return new GlassProject(
            code: "PRJ-001",
            customerId: customerId,
            projectName: "Test Project",
            createdByUserId: Guid.NewGuid());
    }
}
