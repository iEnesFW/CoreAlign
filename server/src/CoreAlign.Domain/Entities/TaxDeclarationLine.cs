using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class TaxDeclarationLine : TenantEntity
{
    public Guid TaxDeclarationId { get; private set; }
    public string? CounterpartyTaxNumber { get; private set; }
    public string CounterpartyName { get; private set; } = string.Empty;
    public int DocumentCount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }

    public TaxDeclaration TaxDeclaration { get; set; } = null!;

    protected TaxDeclarationLine() { }

    public TaxDeclarationLine(
        Guid taxDeclarationId,
        string? counterpartyTaxNumber,
        string counterpartyName,
        int documentCount,
        decimal totalAmount,
        decimal taxAmount)
    {
        TaxDeclarationId = taxDeclarationId;
        CounterpartyTaxNumber = counterpartyTaxNumber;
        CounterpartyName = counterpartyName;
        DocumentCount = documentCount;
        TotalAmount = Math.Round(totalAmount, 4);
        TaxAmount = Math.Round(taxAmount, 4);
    }
}
