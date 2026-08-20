using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.B2B;

// The customer's pending-approval queue keys only on DealerApprovalStatus. Cancelling left it at
// PendingCustomerApproval, so a dealer-cancelled order sat in that queue forever and acting on it
// failed inside Submit() with a status error the customer could not interpret.
public class DealerApprovalQueueOnCancelTests
{
    private static Order DealerOrderAwaitingApproval()
    {
        var order = new Order("ORD-1", Guid.NewGuid(), DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        order.Lines.Add(new OrderLine(Guid.NewGuid(), "SKU", "Item", 2m, 50m));
        order.MarkOrigin(OrderOriginPersona.Dealer, null, Guid.NewGuid(), Guid.NewGuid());
        return order;
    }

    [Fact]
    public void Cancelling_closes_the_pending_dealer_approval()
    {
        var order = DealerOrderAwaitingApproval();
        order.IsPendingDealerApproval.Should().BeTrue();

        order.Cancel("Dealer cancelled before approval.");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.IsPendingDealerApproval.Should().BeFalse();
        order.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.Cancelled);
    }

    [Fact]
    public void Cancelling_through_ChangeStatus_closes_the_approval_too()
    {
        var order = DealerOrderAwaitingApproval();

        order.ChangeStatus(OrderStatus.Cancelled);

        order.IsPendingDealerApproval.Should().BeFalse();
        order.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.Cancelled);
    }

    [Fact]
    public void The_customer_gets_a_clear_refusal_instead_of_a_status_error_from_submit()
    {
        var order = DealerOrderAwaitingApproval();
        order.Cancel("Dealer cancelled before approval.");

        var approve = () => order.ApproveDealerSubmission(Guid.NewGuid());
        var reject = () => order.RejectDealerSubmission(Guid.NewGuid(), "not needed");

        approve.Should().Throw<InvalidOrderApprovalStateException>();
        reject.Should().Throw<InvalidOrderApprovalStateException>();
    }

    [Fact]
    public void An_already_approved_order_keeps_its_approval_record_when_cancelled()
    {
        var order = DealerOrderAwaitingApproval();
        order.ApproveDealerSubmission(Guid.NewGuid());

        order.Cancel("changed mind");

        order.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.Approved);
    }

    [Fact]
    public void A_non_dealer_order_is_unaffected_by_cancellation()
    {
        var order = new Order("ORD-2", Guid.NewGuid(), DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        order.Lines.Add(new OrderLine(Guid.NewGuid(), "SKU", "Item", 1m, 10m));

        order.Cancel("no longer needed");

        order.DealerApprovalStatus.Should().BeNull();
    }
}
