namespace CoreAlign.Application.Imports.Customers;

public class CustomerImportRow
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string Type { get; set; } = "Business";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? NationalId { get; set; }
    public string DefaultCurrency { get; set; } = "TRY";
    public decimal CreditLimit { get; set; }
    public decimal DefaultDiscountPercent { get; set; }
    public string? Notes { get; set; }
}
