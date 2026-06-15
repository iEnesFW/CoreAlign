using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Sales;

public class DealerCommissionLedgerEntry : TenantEntity
{
    public Guid DealerAccountId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid? ShipmentId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal OrderTotal { get; private set; }
    public decimal CommissionPercent { get; private set; }
    public decimal CommissionAmount { get; private set; }
    public DealerCommissionStatus Status { get; private set; } = DealerCommissionStatus.Accrued;
    public DateTime AccruedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? PaidOutAtUtc { get; private set; }
    public string? Notes { get; private set; }

    protected DealerCommissionLedgerEntry() { }

    public DealerCommissionLedgerEntry(
        Guid dealerAccountId,
        Guid orderId,
        Guid? shipmentId,
        Guid customerId,
        string currency,
        decimal orderTotal,
        decimal commissionPercent,
        DateTime accruedAtUtc,
        string? notes = null)
    {
        if (dealerAccountId == Guid.Empty) throw new ArgumentException("Dealer account id is required.", nameof(dealerAccountId));
        if (orderId == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        if (orderTotal < 0m) throw new ArgumentOutOfRangeException(nameof(orderTotal), "Order total cannot be negative.");
        if (commissionPercent < 0m) throw new ArgumentOutOfRangeException(nameof(commissionPercent), "Commission percent cannot be negative.");
        if (commissionPercent > 100m) throw new ArgumentOutOfRangeException(nameof(commissionPercent), "Commission percent cannot exceed 100.");

        DealerAccountId = dealerAccountId;
        OrderId = orderId;
        ShipmentId = shipmentId;
        CustomerId = customerId;
        Currency = currency;
        OrderTotal = Math.Round(orderTotal, 4);
        CommissionPercent = Math.Round(commissionPercent, 4);
        CommissionAmount = Math.Round(orderTotal * commissionPercent / 100m, 4);
        AccruedAtUtc = accruedAtUtc;
        Notes = notes;
    }

    public void MarkPaid(DateTime paidOutAtUtc)
    {
        if (Status == DealerCommissionStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled commission entries cannot be marked paid.");
        }
        Status = DealerCommissionStatus.Paid;
        PaidOutAtUtc = paidOutAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string? reason)
    {
        Status = DealerCommissionStatus.Cancelled;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
