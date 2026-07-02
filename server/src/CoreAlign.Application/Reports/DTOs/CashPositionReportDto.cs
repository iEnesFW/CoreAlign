namespace CoreAlign.Application.Reports.DTOs;

public class CashPositionReportDto
{
    public DateTime AsOfUtc { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal CashOnHand { get; set; }
    public decimal BankBalance { get; set; }
    public decimal TotalCash { get; set; }
    public decimal CustomerAdvances { get; set; }
    public IReadOnlyList<BankAccountSummaryDto> Accounts { get; set; } = new List<BankAccountSummaryDto>();
}

public class BankAccountSummaryDto
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal OpeningBalance { get; set; }
    public bool IsPrimary { get; set; }
}
