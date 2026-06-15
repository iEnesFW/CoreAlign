using CoreAlign.Application.Warranty;
using CoreAlign.Application.Warranty.Subscribers;
using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Warranty;

public class InstallationAcceptedWarrantyActivatorTests
{
    private readonly IWarrantyContractRepository _contracts = Substitute.For<IWarrantyContractRepository>();
    private readonly IWarrantyContractService _service = Substitute.For<IWarrantyContractService>();
    private readonly InstallationAcceptedWarrantyActivator _sut;

    public InstallationAcceptedWarrantyActivatorTests()
    {
        _sut = new InstallationAcceptedWarrantyActivator(
            _contracts,
            _service,
            NullLogger<InstallationAcceptedWarrantyActivator>.Instance);
    }

    [Fact]
    public async Task Handle_activates_linked_warranty_contract_with_acceptance_date()
    {
        var workOrderId = Guid.NewGuid();
        var contract = BuildContract(workOrderId);
        _contracts.GetByWorkOrderIdAsync(workOrderId, Arg.Any<CancellationToken>()).Returns(contract);

        var acceptedAt = new DateTime(2026, 06, 04, 9, 0, 0, DateTimeKind.Utc);
        var notification = new InstallationAcceptedEvent(
            TenantId: Guid.NewGuid(),
            AcceptanceId: Guid.NewGuid(),
            WorkOrderId: workOrderId,
            ProjectId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            AcceptedAtUtc: acceptedAt,
            OccurredAtUtc: DateTime.UtcNow);

        await _sut.Handle(notification, CancellationToken.None);

        await _service.Received(1).ActivateAsync(contract.Id, acceptedAt, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_is_noop_when_no_warranty_linked_to_work_order()
    {
        var workOrderId = Guid.NewGuid();
        _contracts.GetByWorkOrderIdAsync(workOrderId, Arg.Any<CancellationToken>()).Returns((WarrantyContract?)null);

        var notification = new InstallationAcceptedEvent(
            TenantId: Guid.NewGuid(),
            AcceptanceId: Guid.NewGuid(),
            WorkOrderId: workOrderId,
            ProjectId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            AcceptedAtUtc: DateTime.UtcNow,
            OccurredAtUtc: DateTime.UtcNow);

        await _sut.Handle(notification, CancellationToken.None);

        await _service.DidNotReceiveWithAnyArgs().ActivateAsync(default, default, default);
    }

    private static WarrantyContract BuildContract(Guid workOrderId)
    {
        var contract = new WarrantyContract(
            orderId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            number: "WC-2026-00099",
            coverageType: WarrantyCoverageType.FullService,
            startDate: DateTime.UtcNow,
            warrantyMonths: 24,
            termsJson: "{}",
            workOrderId: workOrderId);
        contract.Id = Guid.NewGuid();
        return contract;
    }
}
