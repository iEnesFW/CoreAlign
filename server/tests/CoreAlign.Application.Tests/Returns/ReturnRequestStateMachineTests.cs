using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Returns;

public class ReturnRequestStateMachineTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    [Fact]
    public void Approve_transitions_to_approved_and_emits_event()
    {
        var entity = BuildRequestedReturn();
        var approver = Guid.NewGuid();

        entity.Approve(approver);

        entity.Status.Should().Be(ReturnRequestStatus.Approved);
        entity.ApprovedByUserId.Should().Be(approver);
        entity.DomainEvents.OfType<ReturnRequestApprovedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Approve_from_non_requested_throws()
    {
        var entity = BuildRequestedReturn();
        entity.Reject(Guid.NewGuid(), "n/a");

        var act = () => entity.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    [Fact]
    public void Reject_transitions_to_rejected_with_reason()
    {
        var entity = BuildRequestedReturn();

        entity.Reject(Guid.NewGuid(), "Out of policy");

        entity.Status.Should().Be(ReturnRequestStatus.Rejected);
        entity.RejectionReason.Should().Be("Out of policy");
        entity.DomainEvents.OfType<ReturnRequestRejectedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void MarkReceived_requires_approved_first()
    {
        var entity = BuildRequestedReturn();

        var act = () => entity.MarkReceived(Guid.NewGuid(), WarehouseId);

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    [Fact]
    public void MarkReceived_emits_received_event_with_line_snapshot()
    {
        var entity = BuildRequestedReturn();
        entity.Approve(Guid.NewGuid());

        entity.MarkReceived(Guid.NewGuid(), WarehouseId);

        entity.Status.Should().Be(ReturnRequestStatus.Received);
        var ev = entity.DomainEvents.OfType<ReturnRequestReceivedEvent>().Single();
        ev.WarehouseId.Should().Be(WarehouseId);
        ev.Lines.Should().HaveCount(entity.Lines.Count);
        ev.Lines.Sum(l => l.QuantityReturned).Should().Be(entity.Lines.Sum(l => l.QuantityReturned));
    }

    [Fact]
    public void AttachCreditNote_only_allowed_after_received()
    {
        var entity = BuildRequestedReturn();
        entity.Approve(Guid.NewGuid());

        var act = () => entity.AttachCreditNote(Guid.NewGuid());

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    [Fact]
    public void AttachCreditNote_after_received_transitions_to_credit_noted()
    {
        var entity = BuildRequestedReturn();
        entity.Approve(Guid.NewGuid());
        entity.MarkReceived(Guid.NewGuid(), WarehouseId);
        var creditNoteId = Guid.NewGuid();

        entity.AttachCreditNote(creditNoteId);

        entity.Status.Should().Be(ReturnRequestStatus.CreditNoted);
        entity.CreditNoteId.Should().Be(creditNoteId);
        entity.DomainEvents.OfType<ReturnRequestCreditNotedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void AttachCreditNote_is_idempotent_once_set()
    {
        var entity = BuildRequestedReturn();
        entity.Approve(Guid.NewGuid());
        entity.MarkReceived(Guid.NewGuid(), WarehouseId);
        entity.AttachCreditNote(Guid.NewGuid());

        var act = () => entity.AttachCreditNote(Guid.NewGuid());

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    [Fact]
    public void Cancel_from_terminal_states_throws()
    {
        var entity = BuildRequestedReturn();
        entity.Reject(Guid.NewGuid(), "n/a");

        var act = () => entity.Cancel();

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    [Fact]
    public void Cancel_from_received_throws_to_force_compensating_workflow()
    {
        var entity = BuildRequestedReturn();
        entity.Approve(Guid.NewGuid());
        entity.MarkReceived(Guid.NewGuid(), WarehouseId);

        var act = () => entity.Cancel();

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    [Fact]
    public void Line_quantity_cannot_exceed_shipped_minus_already_returned()
    {
        var order = BuildShippedOrder(shippedQty: 5m);
        var orderLine = order.Lines.First();
        orderLine.RecordReturn(2m);

        var act = () => new ReturnRequestLine(orderLine, quantityReturned: 4m, restockable: true, lineNotes: null);

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    [Fact]
    public void Line_quantity_must_be_positive()
    {
        var order = BuildShippedOrder(shippedQty: 5m);
        var orderLine = order.Lines.First();

        var act = () => new ReturnRequestLine(orderLine, quantityReturned: 0m, restockable: true, lineNotes: null);

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    [Fact]
    public void Creation_on_order_in_draft_status_is_rejected()
    {
        var order = new Order("ORD-NEW", CustomerId, DateTime.UtcNow, "TRY")
        {
            Id = OrderId,
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId },
        };

        var act = () => new ReturnRequest(
            "RMA-1", order, ReturnReasonCode.Defective, null,
            requestedByUserId: null, sourceInvoiceId: null, customerNotes: null);

        act.Should().Throw<InvalidReturnRequestStateException>();
    }

    private static ReturnRequest BuildRequestedReturn()
    {
        var order = BuildShippedOrder(shippedQty: 4m);
        var entity = new ReturnRequest(
            "RMA-1", order, ReturnReasonCode.Defective, "broken",
            requestedByUserId: null, sourceInvoiceId: null, customerNotes: null);
        var line = new ReturnRequestLine(order.Lines.First(), 2m, restockable: true, lineNotes: null);
        entity.ReplaceLines(new[] { line });
        return entity;
    }

    private static Order BuildShippedOrder(decimal shippedQty)
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY")
        {
            Id = OrderId,
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId },
        };
        var line = new OrderLine(Guid.NewGuid(), "SKU-1", "Widget", shippedQty, 25m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        order.ReplaceLines(new[] { line });
        order.ChangeStatus(OrderStatus.Confirmed);
        line.RecordShipment(shippedQty);
        order.ChangeStatus(OrderStatus.Shipped);
        return order;
    }
}
