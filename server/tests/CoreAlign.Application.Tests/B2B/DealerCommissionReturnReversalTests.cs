using CoreAlign.Application;
using CoreAlign.Application.B2B.EventHandlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.B2B;

// Commission accrues on dispatch. Before this handler existed nothing ever moved an entry off
// Accrued — DealerCommissionLedgerEntry.Cancel() had zero callers — so a dealer stayed owed
// commission on goods the customer had sent back and the tenant had credited away.
public class DealerCommissionReturnReversalTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid DealerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IDealerCommissionLedgerRepository _ledger = Substitute.For<IDealerCommissionLedgerRepository>();
    private readonly DealerCommissionReturnReversalHandler _sut;

    public DealerCommissionReturnReversalTests()
    {
        _sut = new DealerCommissionReturnReversalHandler(
            _ledger, NullLogger<DealerCommissionReturnReversalHandler>.Instance);
    }

    private static DealerCommissionLedgerEntry Accrual(decimal basis, decimal percent, int accruedDaysAgo = 0) =>
        new(DealerId, OrderId, Guid.NewGuid(), CustomerId, "TRY", basis, percent,
            DateTime.UtcNow.AddDays(-accruedDaysAgo))
        { Id = Guid.NewGuid(), TenantId = TenantId };

    private static ReturnRequestReceivedEvent Received(decimal returnedLineNet, params ReturnRequestLineSnapshot[] lines) =>
        new(TenantId, Guid.NewGuid(), "RMA-1", OrderId, CustomerId, Guid.NewGuid(),
            lines, returnedLineNet, DateTime.UtcNow);

    [Fact]
    public async Task A_full_return_cancels_the_commission_accrued_on_the_shipment()
    {
        var entry = Accrual(basis: 1000m, percent: 5m);
        _ledger.ListAccruedByOrderAsync(OrderId, Arg.Any<CancellationToken>()).Returns(new[] { entry });

        await _sut.Handle(Received(1000m), default);

        entry.Status.Should().Be(DealerCommissionStatus.Cancelled);
        entry.OrderTotal.Should().Be(0m);
        entry.CommissionAmount.Should().Be(0m);
        entry.Notes.Should().Be("Reversed by return RMA-1");
        _ledger.Received(1).Update(entry);
    }

    [Fact]
    public async Task A_partial_return_leaves_commission_on_the_quantity_that_stayed_sold()
    {
        var entry = Accrual(basis: 1000m, percent: 5m);
        _ledger.ListAccruedByOrderAsync(OrderId, Arg.Any<CancellationToken>()).Returns(new[] { entry });

        await _sut.Handle(Received(400m), default);

        entry.Status.Should().Be(DealerCommissionStatus.Accrued);
        entry.OrderTotal.Should().Be(600m);
        entry.CommissionAmount.Should().Be(30m);
    }

    [Fact]
    public async Task A_return_spanning_several_shipments_consumes_the_oldest_accrual_first()
    {
        var first = Accrual(basis: 300m, percent: 10m, accruedDaysAgo: 2);
        var second = Accrual(basis: 500m, percent: 10m, accruedDaysAgo: 1);
        _ledger.ListAccruedByOrderAsync(OrderId, Arg.Any<CancellationToken>()).Returns(new[] { first, second });

        await _sut.Handle(Received(400m), default);

        first.Status.Should().Be(DealerCommissionStatus.Cancelled);
        first.CommissionAmount.Should().Be(0m);
        second.Status.Should().Be(DealerCommissionStatus.Accrued);
        second.OrderTotal.Should().Be(400m);
        second.CommissionAmount.Should().Be(40m);
    }

    // A damaged return never re-enters stock, so it is absent from the event's restockable Lines
    // snapshot — but the customer is still refunded, so the commission still has to come back.
    [Fact]
    public async Task A_damaged_return_reverses_commission_even_though_it_never_restocks()
    {
        var entry = Accrual(basis: 1000m, percent: 5m);
        _ledger.ListAccruedByOrderAsync(OrderId, Arg.Any<CancellationToken>()).Returns(new[] { entry });

        await _sut.Handle(Received(1000m, Array.Empty<ReturnRequestLineSnapshot>()), default);

        entry.Status.Should().Be(DealerCommissionStatus.Cancelled);
    }

    [Fact]
    public async Task An_already_paid_commission_is_not_silently_edited()
    {
        var entry = Accrual(basis: 1000m, percent: 5m);
        entry.MarkPaid(DateTime.UtcNow);
        _ledger.ListAccruedByOrderAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DealerCommissionLedgerEntry>());

        await _sut.Handle(Received(1000m), default);

        entry.Status.Should().Be(DealerCommissionStatus.Paid);
        entry.CommissionAmount.Should().Be(50m);
        _ledger.DidNotReceive().Update(Arg.Any<DealerCommissionLedgerEntry>());
    }

    [Fact]
    public async Task An_order_with_no_dealer_commission_is_a_no_op()
    {
        _ledger.ListAccruedByOrderAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DealerCommissionLedgerEntry>());

        await _sut.Handle(Received(1000m), default);

        _ledger.DidNotReceive().Update(Arg.Any<DealerCommissionLedgerEntry>());
    }

    [Fact]
    public void The_received_event_carries_every_line_in_its_commission_basis()
    {
        var order = new Order("SO-1", CustomerId, DateTime.UtcNow, "TRY") { Id = OrderId, TenantId = TenantId };
        var restockable = new OrderLine(Guid.NewGuid(), "SKU-A", "Widget", 2m, 100m) { Id = Guid.NewGuid() };
        var damaged = new OrderLine(Guid.NewGuid(), "SKU-B", "Gadget", 1m, 250m) { Id = Guid.NewGuid() };
        order.Lines.Add(restockable);
        order.Lines.Add(damaged);
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);
        order.ChangeStatus(OrderStatus.Shipped);
        restockable.RecordShipment(2m);
        damaged.RecordShipment(1m);

        var request = new ReturnRequest("RMA-9", order, ReturnReasonCode.DamagedInTransit, null, null, null, null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        request.ReplaceLines(new[]
        {
            new ReturnRequestLine(restockable, 2m, restockable: true, lineNotes: null),
            new ReturnRequestLine(damaged, 1m, restockable: false, lineNotes: null),
        });
        request.Approve(Guid.NewGuid());

        request.MarkReceived(Guid.NewGuid(), Guid.NewGuid());

        var received = request.DomainEvents.OfType<ReturnRequestReceivedEvent>().Single();
        received.Lines.Should().HaveCount(1, "only restockable lines re-enter stock");
        received.ReturnedLineNet.Should().Be(450m, "commission reverses on the refunded value, damaged included");
    }

    // A notification handler that MediatR never discovers is silently inert — the exact failure
    // mode that left Cancel() with zero callers in the first place.
    [Fact]
    public void The_reversal_handler_is_discovered_by_mediatr()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        services
            .Where(d => d.ServiceType == typeof(INotificationHandler<ReturnRequestReceivedEvent>))
            .Select(d => d.ImplementationType)
            .Should().Contain(typeof(DealerCommissionReturnReversalHandler));
    }
}
