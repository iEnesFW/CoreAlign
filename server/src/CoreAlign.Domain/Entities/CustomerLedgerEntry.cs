using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class CustomerLedgerEntry : TenantEntity, IXminConcurrency
{
    public Guid CustomerId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime PostingDate { get; private set; } = DateTime.UtcNow.Date;
    public LedgerEntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1m;
    public decimal AmountInBase { get; private set; }

    public LedgerSourceType SourceType { get; private set; }
    public Guid? SourceDocumentId { get; private set; }
    public string? SourceDocumentNumber { get; private set; }

    public decimal RunningBalanceAfter { get; private set; }
    public string? Description { get; private set; }

    public Customer Customer { get; set; } = null!;

    protected CustomerLedgerEntry() { }

    public CustomerLedgerEntry(
        Guid customerId,
        DateTime occurredAtUtc,
        DateTime postingDate,
        LedgerEntryType entryType,
        decimal amount,
        string currency,
        decimal exchangeRate,
        LedgerSourceType sourceType,
        Guid? sourceDocumentId,
        string? sourceDocumentNumber,
        string? description)
    {
        CustomerId = customerId;
        OccurredAtUtc = occurredAtUtc;
        PostingDate = postingDate;
        EntryType = entryType;
        Amount = Math.Abs(amount);
        Currency = currency;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
        AmountInBase = Math.Round(Amount * ExchangeRate, 4);
        SourceType = sourceType;
        SourceDocumentId = sourceDocumentId;
        SourceDocumentNumber = sourceDocumentNumber;
        Description = description;
    }

    public void SetRunningBalance(decimal balance) => RunningBalanceAfter = Math.Round(balance, 4);

    public decimal SignedAmount => EntryType == LedgerEntryType.Debit ? Amount : -Amount;
}
