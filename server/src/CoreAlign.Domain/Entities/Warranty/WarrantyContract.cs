using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;

namespace CoreAlign.Domain.Entities.Warranty;

public class WarrantyContract : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public Guid OrderId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public WarrantyCoverageType CoverageType { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int WarrantyMonths { get; private set; }
    public WarrantyContractStatus Status { get; private set; } = WarrantyContractStatus.Active;
    public string TermsJson { get; private set; } = "{}";
    public string? Notes { get; private set; }
    public string? CancellationReason { get; private set; }

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        ((ISoftDeletable)this).MarkDeletedInternal(userId, reason, utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        ((ISoftDeletable)this).RestoreInternal();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    protected WarrantyContract() { }

    public WarrantyContract(
        Guid orderId,
        Guid customerId,
        string number,
        WarrantyCoverageType coverageType,
        DateTime startDate,
        int warrantyMonths,
        string termsJson,
        Guid? productId = null,
        Guid? workOrderId = null,
        Guid? invoiceId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("Number is required.", nameof(number));
        if (warrantyMonths <= 0) throw new ArgumentOutOfRangeException(nameof(warrantyMonths), "WarrantyMonths must be positive.");
        if (string.IsNullOrWhiteSpace(termsJson)) termsJson = "{}";

        OrderId = orderId;
        CustomerId = customerId;
        Number = number.Trim();
        CoverageType = coverageType;
        StartDate = startDate;
        WarrantyMonths = warrantyMonths;
        EndDate = startDate.AddMonths(warrantyMonths);
        TermsJson = termsJson;
        ProductId = productId;
        WorkOrderId = workOrderId;
        InvoiceId = invoiceId;
        Notes = notes;
        Status = WarrantyContractStatus.Active;
    }

    public void Activate(DateTime startDate)
    {
        if (Status == WarrantyContractStatus.Cancelled)
            throw new InvalidOperationException("Cancelled warranty cannot be activated.");

        StartDate = startDate;
        EndDate = startDate.AddMonths(WarrantyMonths);
        Status = WarrantyContractStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new WarrantyActivatedEvent(TenantId, Id, CustomerId, OrderId, Number, StartDate, EndDate, DateTime.UtcNow));
    }

    public void Suspend(string? reason)
    {
        if (Status != WarrantyContractStatus.Active)
            throw new InvalidOperationException("Only active warranties can be suspended.");
        Status = WarrantyContractStatus.Suspended;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes}\nSuspend: {reason}";
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Resume()
    {
        if (Status != WarrantyContractStatus.Suspended)
            throw new InvalidOperationException("Only suspended warranties can be resumed.");
        Status = WarrantyContractStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        if (Status == WarrantyContractStatus.Cancelled) return;

        Status = WarrantyContractStatus.Cancelled;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new WarrantyCancelledEvent(TenantId, Id, CustomerId, Number, CancellationReason, DateTime.UtcNow));
    }

    public void Extend(int monthsAdded, string? reason)
    {
        if (monthsAdded <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthsAdded), "Months added must be positive.");
        if (Status == WarrantyContractStatus.Cancelled)
            throw new InvalidOperationException("Cancelled warranty cannot be extended.");

        WarrantyMonths += monthsAdded;
        EndDate = EndDate.AddMonths(monthsAdded);
        if (Status == WarrantyContractStatus.Expired) Status = WarrantyContractStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new WarrantyExtendedEvent(TenantId, Id, monthsAdded, EndDate, reason, DateTime.UtcNow));
    }

    public void MarkExpired(DateTime asOfUtc)
    {
        if (Status != WarrantyContractStatus.Active) return;
        if (EndDate > asOfUtc) return;
        Status = WarrantyContractStatus.Expired;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new WarrantyExpiredEvent(TenantId, Id, CustomerId, Number, EndDate, DateTime.UtcNow));
    }

    public bool IsValidAtDate(DateTime asOf)
        => Status == WarrantyContractStatus.Active && StartDate <= asOf && asOf <= EndDate;

    public void AttachInvoice(Guid invoiceId)
    {
        InvoiceId = invoiceId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
