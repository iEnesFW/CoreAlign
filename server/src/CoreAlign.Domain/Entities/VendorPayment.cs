using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class VendorPayment : TenantEntity, IXminConcurrency
{
    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public string PaymentNumber { get; private set; } = string.Empty;
    public DateTime PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;
    public string? Method { get; private set; }
    public Guid? VendorBillId { get; private set; }
    public string? Notes { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public bool IsVoided { get; private set; }
    public DateTime? VoidedAtUtc { get; private set; }
    public string? VoidReason { get; private set; }

    public Vendor Vendor { get; set; } = null!;

    public decimal UnappliedAmount => Math.Max(0m, Math.Round(Amount - AppliedAmount, 4));
    public bool IsDraft => !IsVoided && AppliedAmount == 0m;

    protected VendorPayment() { }

    public VendorPayment(
        Guid vendorId,
        string vendorName,
        string paymentNumber,
        DateTime paymentDate,
        decimal amount,
        string currency,
        decimal exchangeRate = 1m,
        string? method = null,
        Guid? vendorBillId = null,
        string? notes = null)
    {
        VendorId = vendorId;
        VendorName = vendorName;
        PaymentNumber = paymentNumber;
        PaymentDate = paymentDate;
        Amount = Math.Round(amount, 4);
        Currency = currency;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        Method = method;
        VendorBillId = vendorBillId;
        Notes = notes;
    }

    public void UpdateDraft(
        DateTime paymentDate,
        decimal amount,
        string currency,
        decimal exchangeRate,
        string? method,
        string? notes)
    {
        if (!IsDraft)
        {
            throw new VendorPaymentImmutableException();
        }
        PaymentDate = paymentDate;
        Amount = Math.Round(amount, 4);
        Currency = currency;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        Method = method;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Void(string? reason)
    {
        if (IsVoided)
        {
            throw new VendorPaymentAlreadyVoidedException();
        }
        if (AppliedAmount > 0m)
        {
            throw new VendorPaymentImmutableException();
        }
        IsVoided = true;
        VoidedAtUtc = DateTime.UtcNow;
        VoidReason = reason;
        UpdatedAtUtc = VoidedAtUtc.Value;
    }

    public void RecordApplication(decimal amount)
    {
        if (IsVoided)
        {
            throw new VendorPaymentAlreadyVoidedException();
        }
        if (amount <= 0m)
        {
            throw new VendorPaymentOverApplicationException();
        }
        var next = Math.Round(AppliedAmount + amount, 4);
        if (next > Amount + 0.0001m)
        {
            throw new VendorPaymentOverApplicationException();
        }
        AppliedAmount = next;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReverseApplication(decimal amount)
    {
        if (amount <= 0m) return;
        AppliedAmount = Math.Max(0m, Math.Round(AppliedAmount - amount, 4));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
