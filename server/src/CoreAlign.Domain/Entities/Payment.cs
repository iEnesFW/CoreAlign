using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class Payment : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public string PaymentNumber { get; private set; } = string.Empty;
    public PaymentDirection Direction { get; private set; } = PaymentDirection.CustomerReceipt;
    public PaymentStatus Status { get; private set; } = PaymentStatus.Draft;

    public Guid CustomerId { get; private set; }
    public string CustomerNameSnapshot { get; private set; } = string.Empty;

    public DateTime PaymentDate { get; private set; } = DateTime.UtcNow;
    public DateTime PostingDate { get; private set; } = DateTime.UtcNow.Date;
    public PaymentMethod Method { get; private set; } = PaymentMethod.BankTransfer;

    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;
    public decimal? FxRateSnapshot { get; private set; }
    public string? FxSource { get; private set; }
    public DateTime? FxLockedAtUtc { get; private set; }
    public decimal Amount { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public decimal UnappliedAmount => Math.Max(0m, Amount - AppliedAmount);
    public bool IsAdvance { get; private set; }

    public string? BankAccountInfo { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? CheckNumber { get; private set; }
    public DateTime? CheckDueDate { get; private set; }

    public Guid? PostedByUserId { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? VoidedAtUtc { get; private set; }
    public string? VoidReason { get; private set; }
    public string? Notes { get; private set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<PaymentApplication> Applications { get; private set; } = new List<PaymentApplication>();

    public bool IsEditable => Status == PaymentStatus.Draft;
    public bool IsConfirmed =>
        Status == PaymentStatus.Confirmed ||
        Status == PaymentStatus.PartiallyApplied ||
        Status == PaymentStatus.FullyApplied;

    protected Payment() { }

    public Payment(
        string paymentNumber,
        Guid customerId,
        string customerNameSnapshot,
        PaymentDirection direction,
        DateTime paymentDate,
        PaymentMethod method,
        decimal amount,
        string currency,
        bool isAdvance = false)
    {
        if (amount <= 0m)
        {
            throw new PaymentApplicationException("Payment amount must be positive.");
        }
        PaymentNumber = paymentNumber;
        CustomerId = customerId;
        CustomerNameSnapshot = customerNameSnapshot;
        Direction = direction;
        PaymentDate = DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc);
        PostingDate = PaymentDate.Date;
        Method = method;
        Amount = amount;
        Currency = currency;
        IsAdvance = isAdvance;
    }

    public void UpdateDetails(
        DateTime paymentDate,
        DateTime postingDate,
        PaymentMethod method,
        decimal amount,
        decimal exchangeRate,
        string? bankAccountInfo,
        string? referenceNumber,
        string? checkNumber,
        DateTime? checkDueDate,
        string? notes)
    {
        if (Status != PaymentStatus.Draft)
        {
            throw new PaymentApplicationException("Only draft payments can be edited.");
        }
        if (amount <= 0m)
        {
            throw new PaymentApplicationException("Payment amount must be positive.");
        }
        PaymentDate = DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc);
        PostingDate = DateTime.SpecifyKind(postingDate, DateTimeKind.Utc);
        Method = method;
        Amount = amount;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        BankAccountInfo = bankAccountInfo;
        ReferenceNumber = referenceNumber;
        CheckNumber = checkNumber;
        CheckDueDate = checkDueDate.HasValue
            ? DateTime.SpecifyKind(checkDueDate.Value, DateTimeKind.Utc)
            : null;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Confirm(Guid? postedByUserId)
    {
        if (Status != PaymentStatus.Draft)
        {
            throw new PaymentApplicationException("Only draft payments can be confirmed.");
        }
        Status = PaymentStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
        PostedByUserId = postedByUserId;
        UpdatedAtUtc = ConfirmedAtUtc.Value;
        AddDomainEvent(new PaymentConfirmedEvent(TenantId, Id, CustomerId, PaymentNumber, Direction, Amount, Currency, ConfirmedAtUtc.Value, ExchangeRate));
    }

    public PaymentApplication Apply(Guid invoiceId, decimal amount, decimal invoiceRemaining)
    {
        if (!IsConfirmed)
        {
            throw new PaymentApplicationException("Payment must be confirmed before applying.");
        }
        if (amount <= 0m)
        {
            throw new PaymentApplicationException("Application amount must be positive.");
        }
        var existing = Applications.FirstOrDefault(a => a.InvoiceId == invoiceId);
        if (existing is not null)
        {
            return existing;
        }
        if (amount > UnappliedAmount)
        {
            throw new CannotOverApplyPaymentException(UnappliedAmount, amount);
        }
        if (amount > invoiceRemaining)
        {
            throw new CannotOverPayInvoiceException(invoiceRemaining, amount);
        }

        var application = new PaymentApplication(Id, invoiceId, amount);
        Applications.Add(application);
        AppliedAmount = Math.Round(AppliedAmount + amount, 4);
        Status = AppliedAmount >= Amount ? PaymentStatus.FullyApplied : PaymentStatus.PartiallyApplied;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new PaymentAppliedEvent(TenantId, Id, invoiceId, CustomerId, amount, UpdatedAtUtc));
        return application;
    }

    public void Unapply(Guid applicationId)
    {
        var app = Applications.FirstOrDefault(a => a.Id == applicationId)
            ?? throw new PaymentApplicationException("Application not found on payment.");
        AppliedAmount = Math.Max(0m, AppliedAmount - app.AppliedAmount);
        Applications.Remove(app);
        Status = AppliedAmount <= 0m ? PaymentStatus.Confirmed : PaymentStatus.PartiallyApplied;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Void(string? reason)
    {
        if (Status == PaymentStatus.Void)
        {
            // Terminal-state self-guard: a retry/double-click must not re-emit
            // PaymentVoidedEvent (which would double-reverse cash + AR). Mirrors
            // VendorPayment.Void's already-voided guard.
            return;
        }
        var now = DateTime.UtcNow;
        Status = PaymentStatus.Void;
        VoidReason = reason;
        VoidedAtUtc = now;
        UpdatedAtUtc = now;
        AddDomainEvent(new PaymentVoidedEvent(TenantId, Id, CustomerId, PaymentNumber, Amount, Currency, now));
    }

    public void MarkRefunded()
    {
        Status = PaymentStatus.Refunded;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyFxRateSnapshot(decimal rate, string source, DateTime lockedAtUtc)
    {
        if (rate <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Exchange rate must be positive.");
        }
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }
        FxRateSnapshot = rate;
        FxSource = source.Trim().ToUpperInvariant();
        FxLockedAtUtc = DateTime.SpecifyKind(lockedAtUtc, DateTimeKind.Utc);
        ExchangeRate = rate;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
