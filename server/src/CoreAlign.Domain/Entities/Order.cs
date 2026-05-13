using CoreAlign.Domain.Common;
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

public class Order : TenantEntity
{
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

    public Customer Customer { get; set; } = null!;
    public ICollection<OrderLine> Lines { get; private set; } = new List<OrderLine>();
    public ICollection<Shipment> Shipments { get; private set; } = new List<Shipment>();

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
        Status == OrderStatus.Allocated;
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
        Guid? originOrderId)
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

    public void MarkAllocated(Guid? preferredWarehouseId)
    {
        if (Status != OrderStatus.Approved)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), OrderStatus.Allocated.ToString());
        }
        var now = DateTime.UtcNow;
        Status = OrderStatus.Allocated;
        UpdatedAtUtc = now;

        var snapshot = Lines.Select(l => new OrderLineDetailSnapshot(l.Id, l.ProductId, l.Quantity)).ToList();
        AddDomainEvent(new OrderAllocationRequestedEvent(TenantId, Id, OrderNumber, preferredWarehouseId, snapshot, now));
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

        if (previous != OrderStatus.Draft && previous != OrderStatus.Submitted)
        {
            var snap = Lines.Select(l => new OrderLineSnapshot(l.ProductId, l.Quantity)).ToList();
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
        var now = DateTime.UtcNow;
        Status = isPartial ? OrderStatus.PartiallyShipped : OrderStatus.Shipped;
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
        var allowed = from switch
        {
            OrderStatus.Draft => to is OrderStatus.Submitted or OrderStatus.Cancelled or OrderStatus.Confirmed,
            OrderStatus.Submitted => to is OrderStatus.Approved or OrderStatus.Draft or OrderStatus.Cancelled,
            OrderStatus.Approved => to is OrderStatus.Allocated or OrderStatus.Cancelled or OrderStatus.Confirmed,
            OrderStatus.Allocated => to is OrderStatus.Picking or OrderStatus.Cancelled or OrderStatus.Approved or OrderStatus.Confirmed,
            OrderStatus.Picking => to is OrderStatus.Packed or OrderStatus.Cancelled,
            OrderStatus.Packed => to is OrderStatus.Shipped or OrderStatus.PartiallyShipped or OrderStatus.Cancelled,
            OrderStatus.PartiallyShipped => to is OrderStatus.Shipped or OrderStatus.Picking or OrderStatus.Delivered or OrderStatus.Closed,
            OrderStatus.Shipped => to is OrderStatus.Delivered or OrderStatus.Closed or OrderStatus.Returned or OrderStatus.Cancelled,
            OrderStatus.Delivered => to is OrderStatus.Closed or OrderStatus.Returned,
            OrderStatus.Confirmed => to is OrderStatus.Shipped or OrderStatus.Cancelled or OrderStatus.Allocated,
            OrderStatus.Closed => false,
            OrderStatus.Cancelled => false,
            OrderStatus.Returned => false,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOrderStatusTransitionException(from.ToString(), to.ToString());
        }
    }

    private static OrderStockEffect ResolveStockEffect(OrderStatus from, OrderStatus to)
    {
        if (from == OrderStatus.Draft && to == OrderStatus.Confirmed) return OrderStockEffect.Decrement;
        if ((from is OrderStatus.Confirmed or OrderStatus.Shipped or OrderStatus.Approved or OrderStatus.Allocated)
            && to == OrderStatus.Cancelled)
        {
            return OrderStockEffect.Restore;
        }
        return OrderStockEffect.None;
    }
}
