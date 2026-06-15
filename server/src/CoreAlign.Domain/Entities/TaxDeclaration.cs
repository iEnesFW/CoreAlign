using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class TaxDeclaration : TenantEntity
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public TaxDeclarationType DeclarationType { get; private set; }
    public TaxDeclarationStatus Status { get; private set; } = TaxDeclarationStatus.Draft;

    public decimal TotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal WithholdingAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "TRY";

    public string? XmlPayload { get; private set; }

    public DateTime? GeneratedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }

    public int LineCount { get; private set; }
    public string? FailureReason { get; private set; }

    public ICollection<TaxDeclarationLine> Lines { get; set; } = new List<TaxDeclarationLine>();

    protected TaxDeclaration() { }

    public TaxDeclaration(int year, int month, TaxDeclarationType declarationType)
    {
        if (year < 2000 || year > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2000 and 2100.");
        }
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }
        Year = year;
        Month = month;
        DeclarationType = declarationType;
        Status = TaxDeclarationStatus.Draft;
    }

    public void Generate(string xml, decimal totalAmount, decimal taxAmount, decimal withholdingAmount, int lineCount)
    {
        if (Status == TaxDeclarationStatus.Submitted
            || Status == TaxDeclarationStatus.Accepted)
        {
            throw new TaxDeclarationInvalidStateException(Status.ToString(), "regenerate");
        }
        XmlPayload = xml;
        TotalAmount = Math.Round(totalAmount, 4);
        TaxAmount = Math.Round(taxAmount, 4);
        WithholdingAmount = Math.Round(withholdingAmount, 4);
        LineCount = lineCount;
        Status = TaxDeclarationStatus.Generated;
        GeneratedAtUtc = DateTime.UtcNow;
        FailureReason = null;
        UpdatedAtUtc = GeneratedAtUtc.Value;
    }

    public void ReplaceLines(IEnumerable<TaxDeclarationLine> newLines)
    {
        Lines.Clear();
        foreach (var line in newLines)
        {
            Lines.Add(line);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkSubmitted(DateTime? submittedAtUtc = null)
    {
        if (Status != TaxDeclarationStatus.Generated)
        {
            throw new TaxDeclarationInvalidStateException(Status.ToString(), "submit");
        }
        Status = TaxDeclarationStatus.Submitted;
        SubmittedAtUtc = submittedAtUtc ?? DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAccepted()
    {
        if (Status != TaxDeclarationStatus.Submitted)
        {
            throw new TaxDeclarationInvalidStateException(Status.ToString(), "accept");
        }
        Status = TaxDeclarationStatus.Accepted;
        AcceptedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = AcceptedAtUtc.Value;
    }

    public void MarkRejected(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new TaxDeclarationRejectionReasonRequiredException();
        }
        if (Status != TaxDeclarationStatus.Submitted)
        {
            throw new TaxDeclarationInvalidStateException(Status.ToString(), "reject");
        }
        Status = TaxDeclarationStatus.Rejected;
        FailureReason = reason.Length > 500 ? reason.Substring(0, 500) : reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
