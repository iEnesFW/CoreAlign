using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class IncomingInvoice : TenantEntity
{
    public string Ettn { get; private set; } = string.Empty;
    public string SenderVkn { get; private set; } = string.Empty;
    public string? SenderName { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateTime IssueDate { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string? ProviderStatus { get; private set; }
    public IncomingInvoiceStatus Status { get; private set; } = IncomingInvoiceStatus.New;
    public Guid? LinkedVendorBillId { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    protected IncomingInvoice() { }

    public IncomingInvoice(
        string ettn,
        string senderVkn,
        string? senderName,
        string invoiceNumber,
        DateTime issueDate,
        string providerName,
        string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(ettn)) throw new ArgumentException("Ettn is required.", nameof(ettn));
        if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentException("ProviderName is required.", nameof(providerName));

        Ettn = ettn.Trim();
        SenderVkn = senderVkn?.Trim() ?? string.Empty;
        SenderName = string.IsNullOrWhiteSpace(senderName) ? null : senderName.Trim();
        InvoiceNumber = invoiceNumber?.Trim() ?? string.Empty;
        IssueDate = DateTime.SpecifyKind(issueDate, DateTimeKind.Utc);
        ProviderName = providerName.Trim();
        ProviderStatus = string.IsNullOrWhiteSpace(providerStatus) ? null : providerStatus.Trim();
    }

    public void MarkReviewed()
    {
        if (Status == IncomingInvoiceStatus.New)
        {
            Status = IncomingInvoiceStatus.Reviewed;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public void MarkProcessed(Guid vendorBillId)
    {
        if (Status == IncomingInvoiceStatus.Processed)
        {
            throw new IncomingInvoiceAlreadyProcessedException(Ettn);
        }
        if (Status == IncomingInvoiceStatus.Ignored)
        {
            throw new IncomingInvoiceIgnoredException(Ettn);
        }

        Status = IncomingInvoiceStatus.Processed;
        LinkedVendorBillId = vendorBillId;
        ProcessedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ProcessedAtUtc.Value;
    }

    public void MarkIgnored(string? reason)
    {
        if (Status == IncomingInvoiceStatus.Processed)
        {
            throw new IncomingInvoiceAlreadyProcessedException(Ettn);
        }

        Status = IncomingInvoiceStatus.Ignored;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
