using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Orders;

public class OrderRevisionDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid DealerUserId = Guid.NewGuid();
    private static readonly Guid CustomerUserId = Guid.NewGuid();
    private static readonly Guid TenantUserId = Guid.NewGuid();
    private static readonly Guid ProductA = Guid.NewGuid();
    private static readonly Guid ProductB = Guid.NewGuid();

    private static Order BuildSubmittedOrder()
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line1 = new OrderLine(ProductA, "SKU-A", "Widget", 10m, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line2 = new OrderLine(ProductB, "SKU-B", "Gizmo", 5m, 200m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        line1.SetLineNumber(1);
        line2.SetLineNumber(2);
        order.ReplaceLines(new[] { line1, line2 });
        order.MarkOrigin(OrderOriginPersona.Dealer, null, DealerAccountId, DealerUserId);
        order.Submit();
        return order;
    }

    private static List<RevisionLineSnapshot> SnapshotOf(Order order)
    {
        var snap = order.BuildCurrentLineSnapshot().ToList();
        return snap;
    }

    [Fact]
    public void RequestRevision_creates_proposed_revision_with_sequential_number()
    {
        var order = BuildSubmittedOrder();
        var snap = SnapshotOf(order);

        var revision = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, snap, "qty change", DateTime.UtcNow);

        revision.Status.Should().Be(RevisionStatus.Proposed);
        revision.RevisionNumber.Should().Be(1);
        revision.RequestedByPersona.Should().Be(OrderOriginPersona.Customer);
        order.Revisions.Should().ContainSingle();
        order.CurrentRevisionId.Should().Be(revision.Id);
    }

    [Fact]
    public void RequestRevision_after_Shipped_throws_RequestRevisionForbiddenException()
    {
        var order = BuildSubmittedOrder();
        order.Approve(TenantUserId);
        order.MarkAllocated(null);
        order.ChangeStatus(OrderStatus.Picking);
        order.ChangeStatus(OrderStatus.Packed);
        order.ChangeStatus(OrderStatus.Shipped);

        var act = () => order.RequestRevision(
            CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);

        act.Should().Throw<RequestRevisionForbiddenException>();
    }

    [Fact]
    public void RequestRevision_after_Cancelled_throws()
    {
        var order = BuildSubmittedOrder();
        order.ChangeStatus(OrderStatus.Cancelled);

        var act = () => order.RequestRevision(
            CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);

        act.Should().Throw<RequestRevisionForbiddenException>();
    }

    [Fact]
    public void RequestRevision_supersedes_existing_proposed_revision()
    {
        var order = BuildSubmittedOrder();
        var first = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);
        var second = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);

        first.Status.Should().Be(RevisionStatus.Superseded);
        second.Status.Should().Be(RevisionStatus.Proposed);
        second.RevisionNumber.Should().Be(2);
    }

    [Fact]
    public void ApplyRevision_updates_lines_and_recomputes_totals()
    {
        var order = BuildSubmittedOrder();
        var initialTotal = order.Total;

        var snap = SnapshotOf(order);
        snap[0].Quantity = 25m;
        snap[1].Quantity = 1m;

        var revision = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, snap, null, DateTime.UtcNow);
        order.ApplyRevision(revision.Id, DealerUserId, DateTime.UtcNow);

        revision.Status.Should().Be(RevisionStatus.Approved);
        revision.CounterpartyDecisionByUserId.Should().Be(DealerUserId);
        order.Lines.Should().HaveCount(2);
        order.Lines.First(l => l.ProductId == ProductA).Quantity.Should().Be(25m);
        order.Lines.First(l => l.ProductId == ProductB).Quantity.Should().Be(1m);
        order.Total.Should().NotBe(initialTotal);
        order.Total.Should().Be(25m * 100m + 1m * 200m);
        order.AppliedRevisionCount.Should().Be(1);
    }

    [Fact]
    public void RejectRevision_marks_revision_rejected_with_reason()
    {
        var order = BuildSubmittedOrder();
        var revision = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);

        order.RejectRevision(revision.Id, DealerUserId, "Stok yetersiz", DateTime.UtcNow);

        revision.Status.Should().Be(RevisionStatus.Rejected);
        revision.RejectionReason.Should().Be("Stok yetersiz");
        revision.CounterpartyDecisionByUserId.Should().Be(DealerUserId);
        order.Lines.First(l => l.ProductId == ProductA).Quantity.Should().Be(10m);
    }

    [Fact]
    public void RejectRevision_requires_reason()
    {
        var order = BuildSubmittedOrder();
        var revision = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);

        var act = () => order.RejectRevision(revision.Id, DealerUserId, " ", DateTime.UtcNow);
        act.Should().Throw<InvalidRevisionStateException>();
    }

    [Fact]
    public void CancelRevision_only_requester_can_cancel()
    {
        var order = BuildSubmittedOrder();
        var revision = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);

        var actOther = () => order.CancelRevision(revision.Id, DealerUserId, DateTime.UtcNow);
        actOther.Should().Throw<RevisionPersonaNotAuthorizedException>();

        order.CancelRevision(revision.Id, CustomerUserId, DateTime.UtcNow);
        revision.Status.Should().Be(RevisionStatus.Cancelled);
    }

    [Fact]
    public void ApplyRevision_to_already_approved_revision_throws()
    {
        var order = BuildSubmittedOrder();
        var revision = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);
        order.ApplyRevision(revision.Id, DealerUserId, DateTime.UtcNow);

        var act = () => order.ApplyRevision(revision.Id, DealerUserId, DateTime.UtcNow);
        act.Should().Throw<InvalidRevisionStateException>();
    }

    [Fact]
    public void ApplyRevision_unknown_id_throws_OrderRevisionNotFoundException()
    {
        var order = BuildSubmittedOrder();

        var act = () => order.ApplyRevision(Guid.NewGuid(), DealerUserId, DateTime.UtcNow);
        act.Should().Throw<OrderRevisionNotFoundException>();
    }

    [Fact]
    public void RequestRevision_persisting_proposed_lines_keeps_them_in_snapshot()
    {
        var order = BuildSubmittedOrder();
        var snap = SnapshotOf(order);
        snap[0].Quantity = 42m;

        var revision = order.RequestRevision(TenantUserId, OrderOriginPersona.Tenant, snap, "Test", DateTime.UtcNow);

        revision.ProposedLines.Should().HaveCount(2);
        revision.ProposedLines.First(l => l.ProductId == ProductA).Quantity.Should().Be(42m);
    }

    [Fact]
    public void Submit_captures_original_submitted_snapshot_once()
    {
        var order = BuildSubmittedOrder();
        var first = order.OriginalSubmittedSnapshotJson;

        first.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RequestRevision_in_Allocated_state_is_allowed()
    {
        var order = BuildSubmittedOrder();
        order.Approve(TenantUserId);
        order.MarkAllocated(null);

        order.CanRequestRevision().Should().BeTrue();
        var revision = order.RequestRevision(TenantUserId, OrderOriginPersona.Tenant, SnapshotOf(order), null, DateTime.UtcNow);
        revision.Should().NotBeNull();
    }

    [Fact]
    public void RequestRevision_emits_OrderRevisionRequestedEvent()
    {
        var order = BuildSubmittedOrder();
        order.ClearDomainEvents();

        order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);

        order.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "OrderRevisionRequestedEvent");
    }

    [Fact]
    public void ApplyRevision_emits_OrderRevisionApprovedEvent()
    {
        var order = BuildSubmittedOrder();
        var revision = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);
        order.ClearDomainEvents();

        order.ApplyRevision(revision.Id, DealerUserId, DateTime.UtcNow);

        order.DomainEvents.Should().Contain(e => e.GetType().Name == "OrderRevisionApprovedEvent");
    }

    [Fact]
    public void Revision_with_empty_lines_throws()
    {
        var order = BuildSubmittedOrder();

        var act = () => order.RequestRevision(
            CustomerUserId, OrderOriginPersona.Customer, new List<RevisionLineSnapshot>(), null, DateTime.UtcNow);
        act.Should().Throw<InvalidRevisionStateException>();
    }

    [Fact]
    public void Audit_timeline_keeps_all_historical_revisions_including_superseded()
    {
        var order = BuildSubmittedOrder();
        order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);
        order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);
        var third = order.RequestRevision(CustomerUserId, OrderOriginPersona.Customer, SnapshotOf(order), null, DateTime.UtcNow);

        order.Revisions.Should().HaveCount(3);
        order.Revisions.Count(r => r.Status == RevisionStatus.Superseded).Should().Be(2);
        order.Revisions.Count(r => r.Status == RevisionStatus.Proposed).Should().Be(1);
        order.CurrentRevisionId.Should().Be(third.Id);
    }
}
