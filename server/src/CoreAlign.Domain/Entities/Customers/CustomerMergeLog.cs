using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Customers;

public class CustomerMergeLog : TenantEntity
{
    public Guid OperationId { get; private set; }
    public Guid SourceCustomerId { get; private set; }
    public Guid TargetCustomerId { get; private set; }
    public Guid? InitiatedByUserId { get; private set; }
    public DateTime ExecutedAtUtc { get; private set; } = DateTime.UtcNow;
    public int OrdersMoved { get; private set; }
    public int InvoicesMoved { get; private set; }
    public int PaymentsMoved { get; private set; }
    public int AddressesMoved { get; private set; }
    public int ContactsMoved { get; private set; }
    public int CommentsMoved { get; private set; }
    public int LedgerEntriesMoved { get; private set; }
    public int TransactionsMoved { get; private set; }
    public int TagLinksMoved { get; private set; }
    public int DealerLinksMoved { get; private set; }
    public int CustomerUsersMoved { get; private set; }
    public int OtherRecordsMoved { get; private set; }
    public string? Notes { get; private set; }

    protected CustomerMergeLog() { }

    public CustomerMergeLog(
        Guid operationId,
        Guid sourceCustomerId,
        Guid targetCustomerId,
        Guid? initiatedByUserId,
        string? notes)
    {
        OperationId = operationId;
        SourceCustomerId = sourceCustomerId;
        TargetCustomerId = targetCustomerId;
        InitiatedByUserId = initiatedByUserId;
        Notes = notes;
    }

    public void RecordCounts(
        int orders,
        int invoices,
        int payments,
        int addresses,
        int contacts,
        int comments,
        int ledgerEntries,
        int transactions,
        int tagLinks,
        int dealerLinks,
        int customerUsers,
        int other)
    {
        OrdersMoved = orders;
        InvoicesMoved = invoices;
        PaymentsMoved = payments;
        AddressesMoved = addresses;
        ContactsMoved = contacts;
        CommentsMoved = comments;
        LedgerEntriesMoved = ledgerEntries;
        TransactionsMoved = transactions;
        TagLinksMoved = tagLinks;
        DealerLinksMoved = dealerLinks;
        CustomerUsersMoved = customerUsers;
        OtherRecordsMoved = other;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
