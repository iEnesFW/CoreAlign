using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.B2B;

public class DealerOrderApprovalEntityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private static Order BuildOrder()
    {
        var order = new Order("ORD-DEALER-1", CustomerId, DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new OrderLine(ProductId, "SKU-A", "Widget", 2m, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        order.ReplaceLines(new[] { line });
        return order;
    }

    [Fact]
    public void MarkOrigin_with_dealer_sets_fields_atomically_and_pending_approval()
    {
        var order = BuildOrder();
        var dealerAccountId = Guid.NewGuid();
        var dealerUserId = Guid.NewGuid();

        order.MarkOrigin(OrderOriginPersona.Dealer, customerUserId: null, dealerAccountId: dealerAccountId, dealerUserId: dealerUserId);

        order.OriginPersona.Should().Be(OrderOriginPersona.Dealer);
        order.OriginDealerAccountId.Should().Be(dealerAccountId);
        order.OriginDealerUserId.Should().Be(dealerUserId);
        order.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.PendingCustomerApproval);
        order.IsDealerOrder.Should().BeTrue();
        order.IsPendingDealerApproval.Should().BeTrue();
    }

    [Fact]
    public void MarkOrigin_with_tenant_does_not_set_approval_status()
    {
        var order = BuildOrder();
        order.MarkOrigin(OrderOriginPersona.Tenant, null, null, null);
        order.DealerApprovalStatus.Should().BeNull();
    }

    [Fact]
    public void MarkOrigin_after_submit_throws()
    {
        var order = BuildOrder();
        order.Submit();
        var act = () => order.MarkOrigin(OrderOriginPersona.Dealer, null, Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<InvalidOrderApprovalStateException>();
    }

    [Fact]
    public void ApproveDealerSubmission_only_valid_in_pending_state()
    {
        var order = BuildOrder();
        var act = () => order.ApproveDealerSubmission(Guid.NewGuid());
        act.Should().Throw<InvalidOrderApprovalStateException>();
    }

    [Fact]
    public void ApproveDealerSubmission_records_approver_and_timestamp()
    {
        var order = BuildOrder();
        order.MarkOrigin(OrderOriginPersona.Dealer, null, Guid.NewGuid(), Guid.NewGuid());

        var customerUserId = Guid.NewGuid();
        order.ApproveDealerSubmission(customerUserId);

        order.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.Approved);
        order.DealerApprovedByUserId.Should().Be(customerUserId);
        order.DealerApprovedAtUtc.Should().NotBeNull();
        order.DealerRejectionReason.Should().BeNull();
    }

    [Fact]
    public void ApproveDealerSubmission_twice_throws()
    {
        var order = BuildOrder();
        order.MarkOrigin(OrderOriginPersona.Dealer, null, Guid.NewGuid(), Guid.NewGuid());
        order.ApproveDealerSubmission(Guid.NewGuid());

        var act = () => order.ApproveDealerSubmission(Guid.NewGuid());
        act.Should().Throw<InvalidOrderApprovalStateException>();
    }

    [Fact]
    public void RejectDealerSubmission_requires_reason()
    {
        var order = BuildOrder();
        order.MarkOrigin(OrderOriginPersona.Dealer, null, Guid.NewGuid(), Guid.NewGuid());

        var act = () => order.RejectDealerSubmission(Guid.NewGuid(), "   ");
        act.Should().Throw<InvalidOrderApprovalStateException>();
    }

    [Fact]
    public void RejectDealerSubmission_records_reason_and_marks_state_rejected()
    {
        var order = BuildOrder();
        order.MarkOrigin(OrderOriginPersona.Dealer, null, Guid.NewGuid(), Guid.NewGuid());
        var customerUserId = Guid.NewGuid();

        order.RejectDealerSubmission(customerUserId, "Tutar yüksek geldi");

        order.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.Rejected);
        order.DealerApprovedByUserId.Should().Be(customerUserId);
        order.DealerRejectionReason.Should().Be("Tutar yüksek geldi");
    }

    [Fact]
    public void RejectDealerSubmission_after_approve_throws()
    {
        var order = BuildOrder();
        order.MarkOrigin(OrderOriginPersona.Dealer, null, Guid.NewGuid(), Guid.NewGuid());
        order.ApproveDealerSubmission(Guid.NewGuid());

        var act = () => order.RejectDealerSubmission(Guid.NewGuid(), "too late");
        act.Should().Throw<InvalidOrderApprovalStateException>();
    }
}
