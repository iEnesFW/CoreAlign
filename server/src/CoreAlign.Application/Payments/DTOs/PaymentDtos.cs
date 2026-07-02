using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Payments.DTOs;

public class PaymentApplicationDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal AppliedAmount { get; set; }
    public DateTime AppliedAtUtc { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public PaymentDirection Direction { get; set; }
    public PaymentStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public DateTime PostingDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal Amount { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal UnappliedAmount { get; set; }
    public bool IsAdvance { get; set; }
    public string? BankAccountInfo { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? CheckNumber { get; set; }
    public DateTime? CheckDueDate { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public string? VoidReason { get; set; }
    public string? Notes { get; set; }
    public List<PaymentApplicationDto> Applications { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public PaymentDirection Direction { get; set; }
    public PaymentStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public decimal UnappliedAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class CustomerLedgerEntryDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime PostingDate { get; set; }
    public LedgerEntryType EntryType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal AmountInBase { get; set; }
    public LedgerSourceType SourceType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string? SourceDocumentNumber { get; set; }
    public decimal RunningBalanceAfter { get; set; }
    public string? Description { get; set; }
}

public class AgingBucketDto
{
    public string Bucket { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int InvoiceCount { get; set; }
}

public class CustomerAgingDto
{
    public Guid CustomerId { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal DaysOver90 { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<AgingBucketDto> Buckets { get; set; } = new();
}
