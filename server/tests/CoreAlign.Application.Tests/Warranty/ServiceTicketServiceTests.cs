using CoreAlign.Application.Warranty;
using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Warranty;

public class ServiceTicketServiceTests
{
    private readonly IServiceTicketRepository _tickets = Substitute.For<IServiceTicketRepository>();
    private readonly IWarrantyContractRepository _contracts = Substitute.For<IWarrantyContractRepository>();
    private readonly ServiceTicketService _sut;

    public ServiceTicketServiceTests()
    {
        _sut = new ServiceTicketService(_tickets, _contracts);
    }

    [Fact]
    public async Task OpenAsync_under_active_warranty_marks_ticket_as_under_warranty()
    {
        var contract = BuildActiveContract();
        _contracts.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var ticket = await _sut.OpenAsync(
            customerId: Guid.NewGuid(),
            type: ServiceTicketType.WarrantyClaim,
            priority: ServiceTicketPriority.High,
            title: "Hinge failure",
            descriptionMd: "Door does not close.",
            warrantyContractId: contract.Id);

        ticket.IsUnderWarranty.Should().BeTrue();
        ticket.ChargeableAmount.Should().BeNull();
        ticket.Status.Should().Be(ServiceTicketStatus.Open);
        await _tickets.Received(1).AddAsync(ticket, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenAsync_out_of_warranty_marks_ticket_as_not_under_warranty()
    {
        var expired = BuildExpiredContract();
        _contracts.GetByIdAsync(expired.Id, Arg.Any<CancellationToken>()).Returns(expired);

        var ticket = await _sut.OpenAsync(
            customerId: Guid.NewGuid(),
            type: ServiceTicketType.OutOfWarrantyRepair,
            priority: ServiceTicketPriority.Normal,
            title: "Out of warranty repair",
            descriptionMd: "Customer requests paid repair.",
            warrantyContractId: expired.Id);

        ticket.IsUnderWarranty.Should().BeFalse();
        ticket.WarrantyContractId.Should().Be(expired.Id);

        _tickets.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        await _sut.ResolveAsync(ticket.Id, "Resolved with paid repair", workOrderId: null, chargeableAmount: 1500m);

        ticket.ChargeableAmount.Should().Be(1500m);
    }

    [Fact]
    public async Task AssignAsync_transitions_status_from_open_to_assigned()
    {
        var ticket = BuildOpenTicket();
        _tickets.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        await _sut.AssignAsync(ticket.Id, Guid.NewGuid());

        ticket.Status.Should().Be(ServiceTicketStatus.Assigned);
        _tickets.Received(1).Update(ticket);
    }

    [Fact]
    public async Task ResolveAsync_transitions_from_in_progress_to_resolved_with_notes()
    {
        var ticket = BuildOpenTicket();
        ticket.Assign(Guid.NewGuid());
        ticket.StartWork();
        ticket.Status.Should().Be(ServiceTicketStatus.InProgress);
        _tickets.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        await _sut.ResolveAsync(ticket.Id, resolutionNotesMd: "Hinge replaced under warranty.", workOrderId: null, chargeableAmount: null);

        ticket.Status.Should().Be(ServiceTicketStatus.Resolved);
        ticket.ResolutionNotesMd.Should().Be("Hinge replaced under warranty.");
        ticket.ResolvedAtUtc.Should().NotBeNull();
        _tickets.Received(1).Update(ticket);
    }

    [Fact]
    public async Task OpenAsync_without_contract_treats_ticket_as_not_under_warranty()
    {
        var ticket = await _sut.OpenAsync(
            customerId: Guid.NewGuid(),
            type: ServiceTicketType.Inspection,
            priority: ServiceTicketPriority.Low,
            title: "Inspection only",
            descriptionMd: "No active warranty.",
            warrantyContractId: null);

        ticket.IsUnderWarranty.Should().BeFalse();
        ticket.WarrantyContractId.Should().BeNull();
    }

    private static WarrantyContract BuildActiveContract()
    {
        var c = new WarrantyContract(
            orderId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            number: "WC-2026-00010",
            coverageType: WarrantyCoverageType.FullService,
            startDate: DateTime.UtcNow.AddDays(-30),
            warrantyMonths: 24,
            termsJson: "{}");
        c.Id = Guid.NewGuid();
        return c;
    }

    private static WarrantyContract BuildExpiredContract()
    {
        var c = new WarrantyContract(
            orderId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            number: "WC-2024-00001",
            coverageType: WarrantyCoverageType.Limited,
            startDate: DateTime.UtcNow.AddYears(-3),
            warrantyMonths: 12,
            termsJson: "{}");
        c.Id = Guid.NewGuid();
        return c;
    }

    private static ServiceTicket BuildOpenTicket()
    {
        var ticket = new ServiceTicket(
            customerId: Guid.NewGuid(),
            type: ServiceTicketType.WarrantyClaim,
            priority: ServiceTicketPriority.Normal,
            title: "Sample ticket",
            descriptionMd: "Issue body",
            isUnderWarranty: true);
        ticket.Id = Guid.NewGuid();
        return ticket;
    }

}
