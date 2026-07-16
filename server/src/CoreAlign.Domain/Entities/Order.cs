using System.Text.Json;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public enum OrderStockEffect
{
    None,
    Decrement,
    Restore
}

public static class OrderOriginPersona
{
    public const string Customer = "Customer";
    public const string Dealer = "Dealer";
    public const string Tenant = "Tenant";
}

public static class DealerOrderApprovalStatuses
{
    public const string PendingCustomerApproval = "PendingCustomerApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public class Order : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public string OrderNumber { get; private set; } = string.Empty;
    public OrderType Type { get; private set; } = OrderType.Standard;
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public OrderSource Source { get; private set; } = OrderSource.Manual;

    public Guid CustomerId { get; private set; }
    public Guid? BillingAddressId { get; private set; }
    public Guid? ShippingAddressId { get; private set; }

    public CustomerSnapshot? CustomerSnapshot { get; private set; }
    public AddressSnapshot? BillingAddressSnapshot { get; private set; }
    public AddressSnapshot? ShippingAddressSnapshot { get; private set; }

    public DateTime OrderDate { get; private set; } = DateTime.UtcNow;
    public DateTime? RequestedDeliveryDate { get; private set; }
    public DateTime? PromisedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;

    public Guid? PaymentTermsId { get; private set; }
    public int? PaymentTermsNetDaysSnapshot { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid? PriceListId { get; private set; }

    public decimal Subtotal { get; private set; }
    public decimal LineDiscountTotal { get; private set; }
    public decimal HeaderDiscountAmount { get; private set; }
    public decimal HeaderDiscountPercent { get; private set; }
    public decimal TaxableTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal WithholdingTotal { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal RoundingAdjustment { get; private set; }
    public decimal Total { get; private set; }

    public Guid? SalesRepUserId { get; private set; }
    public string? Channel { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string? CancelReason { get; private set; }
    public Guid? OriginOrderId { get; private set; }

    public string? InternalNotes { get; private set; }
    public string? CustomerNotes { get; private set; }
    public string? Notes { get; private set; }

    public string? OriginPersona { get; private set; }
    public Guid? OriginCustomerUserId { get; private set; }
    public Guid? OriginDealerAccountId { get; private set; }
    public Guid? OriginDealerUserId { get; private set; }

    public string? DealerApprovalStatus { get; private set; }
    public Guid? DealerApprovedByUserId { get; private set; }
    public DateTime? DealerApprovedAtUtc { get; private set; }
    public DateTime? DealerRejectedAtUtc { get; private set; }
    public string? DealerRejectionReason { get; private set; }

    public Guid? SourceQuoteId { get; private set; }
    public Guid? GlassProjectId { get; private set; }
    public Guid? SourceGlassProjectId { get; private set; }

    public Guid? CurrentRevisionId { get; private set; }
    public int AppliedRevisionCount { get; private set; }
    public string? OriginalSubmittedSnapshotJson { get; private set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<OrderLine> Lines { get; private set; } = new List<OrderLine>();
    public ICollection<Shipment> Shipments { get; private set; } = new List<Shipment>();
    public ICollection<OrderRevision> Revisions { get; private set; } = new List<OrderRevision>();

    public bool IsDealerOrder => string.Equals(OriginPersona, OrderOriginPersona.Dealer, StringComparison.Ordinal);
    public bool IsPendingDealerApproval =>
        string.Equals(DealerApprovalStatus, DealerOrderApprovalStatuses.PendingCustomerApproval, StringComparison.Ordinal);

    public bool CanRequestRevision() =>
        Status is OrderStatus.Submitted or OrderStatus.Approved or OrderStatus.Allocated or OrderStatus.Picking;

    protected Order() { }

    public Order(string orderNumber, Guid customerId, DateTime orderDate, string currency, string? notes = null)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        OrderDate = orderDate;
        Currency = currency;
        Notes = notes;
    }

    public bool IsDraft => Status == OrderStatus.Draft;
    public bool IsCancellable =>
        Status == OrderStatus.Draft ||
        Status == OrderStatus.Submitted ||
        Status == OrderStatus.Approved ||
        Status == OrderStatus.Allocated ||
        Status == OrderStatus.Confirmed;
    public bool IsEditable => Status == OrderStatus.Draft;

    public void UpdateHeader(string orderNumber, Guid customerId, DateTime orderDate, string currency, string? notes)
    {
        EnsureDraft();
        OrderNumber = orderNumber;
        CustomerId = customerId;
        OrderDate = orderDate;
        Currency = currency;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(
        OrderType type,
        OrderSource source,
        DateTime? requestedDeliveryDate,
        DateTime? promisedDeliveryDate,
        Guid? billingAddressId,
        Guid? shippingAddressId,
        Guid? paymentTermsId,
        Guid? priceListId,
        decimal exchangeRate,
        decimal shippingCost,
        decimal headerDiscountPercent,
        decimal headerDiscountAmount,
        Guid? salesRepUserId,
        string? channel,
        string? internalNotes,
        string? customerNotes,
        Guid? originOrderId,
        decimal roundingAdjustment = 0m)
    {
        EnsureDraft();
        Type = type;
        Source = source;
        RequestedDeliveryDate = requestedDeliveryDate;
        PromisedDeliveryDate = promisedDeliveryDate;
        BillingAddressId = billingAddressId;
        ShippingAddressId = shippingAddressId;
        PaymentTermsId = paymentTermsId;
        PriceListId = priceListId;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        ShippingCost = shippingCost;
        RoundingAdjustment = roundingAdjustment;
        HeaderDiscountPercent = headerDiscountPercent;
        HeaderDiscountAmount = headerDiscountAmount;
        SalesRepUserId = salesRepUserId;
        Channel = channel;
        InternalNotes = internalNotes;
        CustomerNotes = customerNotes;
        OriginOrderId = originOrderId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplySnapshots(
        CustomerSnapshot customerSnapshot,
        AddressSnapshot? billingAddressSnapshot,
        AddressSnapshot? shippingAddressSnapshot,
        int? paymentTermsNetDays,
        DateTime? dueDate)
    {
        CustomerSnapshot = customerSnapshot;
        BillingAddressSnapshot = billingAddressSnapshot;
        ShippingAddressSnapshot = shippingAddressSnapshot;
        PaymentTermsNetDaysSnapshot = paymentTermsNetDays;
        DueDate = dueDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<OrderLine> newLines)
    {
        EnsureDraft();
        Lines.Clear();
        foreach (var line in newLines)
        {
            Lines.Add(line);
        }
        Recalculate();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Recalculate()
    {
        Subtotal = Math.Round(Lines.Sum(l => l.LineSubtotal), 4);
        LineDiscountTotal = Math.Round(Lines.Sum(l => l.LineDiscountAmount), 4);
        var lineNet = Math.Round(Lines.Sum(l => l.LineNetAmount), 4);
        var headerDiscount = HeaderDiscountAmount + (lineNet * (HeaderDiscountPercent / 100m));
        var afterHeaderDiscount = lineNet - headerDiscount;
        TaxableTotal = Math.Round(afterHeaderDiscount, 4);
        TaxTotal = Math.Round(Lines.Sum(l => l.LineTaxAmount), 4);
        WithholdingTotal = Math.Round(Lines.Sum(l => l.LineWithholdingAmount), 4);
        Total = Math.Round(TaxableTotal + TaxTotal - WithholdingTotal + ShippingCost + RoundingAdjustment, 4);
    }

    public OrderStockEffect ChangeStatus(OrderStatus newStatus)
    {
        if (Status == newStatus) return OrderStockEffect.None;

        EnsureTransitionAllowed(Status, newStatus);

        var previous = Status;
        var effect = ResolveStockEffect(previous, newStatus);
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;

        var now = DateTime.UtcNow;
        AddDomainEvent(new OrderStatusChangedEvent(TenantId, Id, OrderNumber, previous, newStatus, now));

        if (newStatus == OrderStatus.Submitted) SubmittedAtUtc = now;
        if (newStatus == OrderStatus.Cancelled) CancelledAtUtc = now;
        if (newStatus == OrderStatus.Closed) AddDomainEvent(new OrderClosedEvent(TenantId, Id, OrderNumber, now));
        if (newStatus == OrderStatus.Delivered)
        {
            ActualDeliveryDate ??= now;
            AddDomainEvent(new OrderDeliveredEvent(TenantId, Id, OrderNumber, now));
        }

        if (effect == OrderStockEffect.Decrement || effect == OrderStockEffect.Restore)
        {
            var snapshot = Lines
                .Where(l => !l.IsService)
                .Select(l => new OrderLineSnapshot(l.ProductId, l.Quantity))
                .ToList();
            if (effect == OrderStockEffect.Decrement)
            {
                AddDomainEvent(new OrderConfirmedEvent(TenantId, Id, OrderNumber, snapshot, now));
            }
            else
            {
                AddDomainEvent(new OrderCancelledFromActiveEvent(TenantId, Id, OrderNumber, snapshot, now));
            }
        }

        return effect;
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), OrderStatus.Submitted.ToString());
        }
        if (Lines.Count == 0)
        {
            throw new InvalidOrderLineException("Cannot submit an order with no lines.");
        }
        Status = OrderStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = SubmittedAtUtc.Value;
        if (string.IsNullOrEmpty(OriginalSubmittedSnapshotJson))
        {
            OriginalSubmittedSnapshotJson = JsonSerializer.Serialize(BuildCurrentLineSnapshot());
        }
        AddDomainEvent(new OrderSubmittedEvent(TenantId, Id, OrderNumber, SubmittedAtUtc.Value));
        AddDomainEvent(new OrderStatusChangedEvent(TenantId, Id, OrderNumber, OrderStatus.Draft, OrderStatus.Submitted, SubmittedAtUtc.Value));
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), OrderStatus.Approved.ToString());
        }
        Status = OrderStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ApprovedAtUtc.Value;
        AddDomainEvent(new OrderApprovedEvent(TenantId, Id, OrderNumber, approvedByUserId, ApprovedAtUtc.Value));
        AddDomainEvent(new OrderStatusChangedEvent(TenantId, Id, OrderNumber, OrderStatus.Submitted, OrderStatus.Approved, ApprovedAtUtc.Value));
    }

    public void RevertToDraft()
    {
        EnsureTransitionAllowed(Status, OrderStatus.Draft);
        var previous = Status;
        var now = DateTime.UtcNow;
        Status = OrderStatus.Draft;
        SubmittedAtUtc = null;
        ApprovedByUserId = null;
        ApprovedAtUtc = null;
        UpdatedAtUtc = now;
        AddDomainEvent(new OrderStatusChangedEvent(TenantId, Id, OrderNumber, previous, OrderStatus.Draft, now));
    }

    public void MarkAllocated(Guid? preferredWarehouseId)
    {
        if (Status != OrderStatus.Approved)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), OrderStatus.Allocated.ToString());
        }
        var now = DateTime.UtcNow;
        Status = OrderStatus.Allocated;
        UpdatedAtUtc = now;

        // Reservations are created directly by the allocation handler (ReserveAsync),
        // not via a domain event — the preferred warehouse is honoured there. The
        // status transition is the only signal broadcast here.
        _ = preferredWarehouseId;
        AddDomainEvent(new OrderStatusChangedEvent(TenantId, Id, OrderNumber, OrderStatus.Approved, OrderStatus.Allocated, now));
    }

    public void Cancel(string? reason)
    {
        if (!IsCancellable)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), OrderStatus.Cancelled.ToString());
        }
        var previous = Status;
        Status = OrderStatus.Cancelled;
        CancelReason = reason;
        CancelledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CancelledAtUtc.Value;
        AddDomainEvent(new OrderStatusChangedEvent(TenantId, Id, OrderNumber, previous, OrderStatus.Cancelled, CancelledAtUtc.Value));

        if (previous is OrderStatus.Confirmed or OrderStatus.Shipped)
        {
            // Mirror ChangeStatus's restore snapshot: service lines carry no
            // ProductId and must be excluded, else the restore handler resolves a
            // Guid.Empty "product" and throws KeyNotFoundException.
            var snap = Lines.Where(l => !l.IsService).Select(l => new OrderLineSnapshot(l.ProductId, l.Quantity)).ToList();
            AddDomainEvent(new OrderCancelledFromActiveEvent(TenantId, Id, OrderNumber, snap, CancelledAtUtc.Value));
        }
    }

    public void NotePartialShipment()
    {
        if (Status == OrderStatus.Allocated || Status == OrderStatus.Picking || Status == OrderStatus.Packed)
        {
            Status = OrderStatus.PartiallyShipped;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public void MarkFullyShipped(Guid shipmentId, string shipmentNumber, bool isPartial)
    {
        var target = isPartial ? OrderStatus.PartiallyShipped : OrderStatus.Shipped;

        // A stray shipment dispatched after the order has already moved downstream
        // (Delivered/Closed) or to a terminal state (Cancelled/Returned) must NOT
        // drag the order backward to Shipped/PartiallyShipped and re-emit
        // OrderShippedEvent. Only mutate when the FSM permits the forward move;
        // otherwise the shipment dispatch proceeds without touching order status.
        if (Status == target || !IsTransitionAllowed(Status, target))
        {
            return;
        }

        var now = DateTime.UtcNow;
        Status = target;
        UpdatedAtUtc = now;
        AddDomainEvent(new OrderShippedEvent(TenantId, Id, shipmentId, OrderNumber, shipmentNumber, isPartial, now));
    }

    public bool HasSameHeader(string orderNumber, Guid customerId, DateTime orderDate, string currency, string? notes)
    {
        if (!string.Equals(OrderNumber, orderNumber, StringComparison.Ordinal)) return false;
        if (CustomerId != customerId) return false;
        if (OrderDate.Date != orderDate.Date) return false;
        if (!string.Equals(Currency, currency, StringComparison.Ordinal)) return false;
        if (!string.Equals(Notes ?? string.Empty, notes ?? string.Empty, StringComparison.Ordinal)) return false;
        return true;
    }

    public bool HasSameLines(IEnumerable<(Guid ProductId, decimal Quantity, decimal UnitPrice)> incoming)
    {
        var existing = Lines
            .Select(l => (l.ProductId, l.Quantity, l.UnitPrice))
            .OrderBy(t => t.ProductId)
            .ToList();
        var incomingSorted = incoming.OrderBy(t => t.ProductId).ToList();
        return existing.SequenceEqual(incomingSorted);
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new OrderImmutableException(Status.ToString());
        }
    }

    private static void EnsureTransitionAllowed(OrderStatus from, OrderStatus to)
    {
        if (!IsTransitionAllowed(from, to))
        {
            throw new InvalidOrderStatusTransitionException(from.ToString(), to.ToString());
        }
    }

    private static bool IsTransitionAllowed(OrderStatus from, OrderStatus to) =>
        from switch
        {
            OrderStatus.Draft => to is OrderStatus.Submitted or OrderStatus.Cancelled or OrderStatus.Confirmed,
            OrderStatus.Submitted => to is OrderStatus.Approved or OrderStatus.Draft or OrderStatus.Cancelled,
            OrderStatus.Approved => to is OrderStatus.Allocated or OrderStatus.Cancelled or OrderStatus.Draft,
            // WHY: a dispatched shipment ships its order directly from any fulfilment-active
            // state — the Shipment aggregate carries the pick/pack lifecycle, so the order's
            // coarse status is not separately walked through Picking/Packed by the WMS flow.
            OrderStatus.Allocated => to is OrderStatus.Picking or OrderStatus.Packed or OrderStatus.Shipped or OrderStatus.PartiallyShipped or OrderStatus.Cancelled or OrderStatus.Approved or OrderStatus.Draft,
            OrderStatus.Picking => to is OrderStatus.Packed or OrderStatus.Shipped or OrderStatus.PartiallyShipped or OrderStatus.Cancelled,
            OrderStatus.Packed => to is OrderStatus.Shipped or OrderStatus.PartiallyShipped or OrderStatus.Cancelled,
            OrderStatus.PartiallyShipped => to is OrderStatus.Shipped or OrderStatus.Picking or OrderStatus.Delivered or OrderStatus.Closed,
            OrderStatus.Shipped => to is OrderStatus.Delivered or OrderStatus.Closed or OrderStatus.Returned or OrderStatus.Cancelled,
            OrderStatus.Delivered => to is OrderStatus.Closed or OrderStatus.Returned,
            OrderStatus.Confirmed => to is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Closed => false,
            OrderStatus.Cancelled => false,
            OrderStatus.Returned => false,
            _ => false
        };

    private static OrderStockEffect ResolveStockEffect(OrderStatus from, OrderStatus to)
    {
        if (from == OrderStatus.Draft && to == OrderStatus.Confirmed) return OrderStockEffect.Decrement;
        if ((from is OrderStatus.Confirmed or OrderStatus.Shipped) && to == OrderStatus.Cancelled)
        {
            return OrderStockEffect.Restore;
        }
        return OrderStockEffect.None;
    }

    public void MarkOrigin(string persona, Guid? customerUserId, Guid? dealerAccountId, Guid? dealerUserId)
    {
        if (string.IsNullOrWhiteSpace(persona))
        {
            throw new ArgumentException("Persona is required.", nameof(persona));
        }
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOrderApprovalStateException(
                $"Origin can only be set while order is Draft (current: {Status}).");
        }

        OriginPersona = persona;
        OriginCustomerUserId = customerUserId;
        OriginDealerAccountId = dealerAccountId;
        OriginDealerUserId = dealerUserId;

        if (string.Equals(persona, OrderOriginPersona.Dealer, StringComparison.Ordinal))
        {
            DealerApprovalStatus = DealerOrderApprovalStatuses.PendingCustomerApproval;
        }
        else
        {
            DealerApprovalStatus = null;
            DealerApprovedByUserId = null;
            DealerApprovedAtUtc = null;
            DealerRejectedAtUtc = null;
            DealerRejectionReason = null;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApproveDealerSubmission(Guid approverId)
    {
        if (!IsPendingDealerApproval)
        {
            throw new InvalidOrderApprovalStateException(
                $"Order is not pending dealer approval (state: {DealerApprovalStatus ?? "<none>"}).");
        }
        DealerApprovalStatus = DealerOrderApprovalStatuses.Approved;
        DealerApprovedByUserId = approverId;
        DealerApprovedAtUtc = DateTime.UtcNow;
        DealerRejectionReason = null;
        UpdatedAtUtc = DealerApprovedAtUtc.Value;
    }

    public void RejectDealerSubmission(Guid rejectorId, string reason)
    {
        if (!IsPendingDealerApproval)
        {
            throw new InvalidOrderApprovalStateException(
                $"Order is not pending dealer approval (state: {DealerApprovalStatus ?? "<none>"}).");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOrderApprovalStateException("Rejection reason is required.");
        }
        DealerApprovalStatus = DealerOrderApprovalStatuses.Rejected;
        DealerApprovedByUserId = rejectorId;
        DealerRejectedAtUtc = DateTime.UtcNow;
        DealerRejectionReason = reason.Trim();
        UpdatedAtUtc = DealerRejectedAtUtc.Value;
    }

    public void LinkSourceQuote(Guid quoteId)
    {
        SourceQuoteId = quoteId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void LinkToGlassProject(Guid projectId)
    {
        GlassProjectId = projectId;
        SourceGlassProjectId = projectId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public IReadOnlyList<RevisionLineSnapshot> BuildCurrentLineSnapshot()
    {
        return Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new RevisionLineSnapshot
            {
                ProductId = l.ProductId,
                ProductSku = l.ProductSku,
                ProductName = l.ProductName,
                LineNumber = l.LineNumber,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineDiscountPercent = l.LineDiscountPercent,
                LineDiscountAmount = l.LineDiscountAmount,
                TaxRatePercent = l.TaxRatePercent,
                IsTaxInclusive = l.IsTaxInclusive,
                WithholdingRatePercent = l.WithholdingRatePercent,
                LineNotes = l.LineNotes,
            })
            .ToList();
    }

    public OrderRevision RequestRevision(
        Guid userId,
        string persona,
        IEnumerable<RevisionLineSnapshot> lineSnapshots,
        string? notes,
        DateTime nowUtc)
    {
        if (!CanRequestRevision())
        {
            throw new RequestRevisionForbiddenException(Status.ToString());
        }

        foreach (var existing in Revisions.Where(r => r.IsPending).ToList())
        {
            existing.Supersede(nowUtc);
        }

        var nextNumber = Revisions.Count + 1;
        var revision = new OrderRevision(Id, nextNumber, userId, persona, lineSnapshots, notes, nowUtc)
        {
            TenantId = TenantId,
        };
        Revisions.Add(revision);
        CurrentRevisionId = revision.Id;
        UpdatedAtUtc = nowUtc;

        AddDomainEvent(new OrderRevisionRequestedEvent(
            TenantId, Id, revision.Id, revision.RevisionNumber, OrderNumber, userId, persona, nowUtc));

        return revision;
    }

    public void ApplyRevision(Guid revisionId, Guid decidedByUserId, DateTime nowUtc)
    {
        var revision = Revisions.FirstOrDefault(r => r.Id == revisionId)
            ?? throw new OrderRevisionNotFoundException();
        revision.Approve(decidedByUserId, nowUtc);

        foreach (var snap in revision.ProposedLines)
        {
            var line = Lines.FirstOrDefault(l => l.LineNumber == snap.LineNumber)
                ?? Lines.FirstOrDefault(l => l.ProductId == snap.ProductId);
            if (line is null) continue;

            line.ApplyPricing(
                quantity: snap.Quantity,
                listPriceSnapshot: line.ListPriceSnapshot,
                unitPrice: snap.UnitPrice,
                lineDiscountPercent: snap.LineDiscountPercent,
                lineDiscountAmount: snap.LineDiscountAmount,
                isManualPriceOverride: line.IsManualPriceOverride,
                taxRatePercent: snap.TaxRatePercent,
                taxRateId: line.TaxRateId,
                isTaxInclusive: snap.IsTaxInclusive,
                withholdingRatePercent: snap.WithholdingRatePercent,
                unitCostSnapshot: line.UnitCostSnapshot,
                uomId: line.UomId,
                uomCode: line.UomCode,
                uomConversionFactor: line.UomConversionFactor,
                warehouseId: line.WarehouseId,
                lineNotes: snap.LineNotes,
                parentLineId: line.ParentLineId,
                isKitComponent: line.IsKitComponent,
                productDescriptionSnapshot: line.ProductDescriptionSnapshot);
        }

        Recalculate();
        AppliedRevisionCount++;
        UpdatedAtUtc = nowUtc;

        AddDomainEvent(new OrderRevisionApprovedEvent(
            TenantId, Id, revision.Id, revision.RevisionNumber, OrderNumber, decidedByUserId, Total, Currency, nowUtc));
    }

    public void RejectRevision(Guid revisionId, Guid decidedByUserId, string reason, DateTime nowUtc)
    {
        var revision = Revisions.FirstOrDefault(r => r.Id == revisionId)
            ?? throw new OrderRevisionNotFoundException();
        revision.Reject(decidedByUserId, reason, nowUtc);
        UpdatedAtUtc = nowUtc;

        AddDomainEvent(new OrderRevisionRejectedEvent(
            TenantId, Id, revision.Id, revision.RevisionNumber, OrderNumber, decidedByUserId, reason, nowUtc));
    }

    public void CancelRevision(Guid revisionId, Guid cancelledByUserId, DateTime nowUtc)
    {
        var revision = Revisions.FirstOrDefault(r => r.Id == revisionId)
            ?? throw new OrderRevisionNotFoundException();
        revision.Cancel(cancelledByUserId, nowUtc);
        UpdatedAtUtc = nowUtc;
    }

    public void RecordLineScrap(Guid lineId, decimal qty, string? reason = null)
    {
        if (Status == OrderStatus.Cancelled || Status == OrderStatus.Closed)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), "Scrap");
        }
        var line = Lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOrderLineException($"Order line '{lineId}' not found.");
        line.RecordScrap(qty);
        UpdatedAtUtc = DateTime.UtcNow;
        _ = reason;
    }
}
