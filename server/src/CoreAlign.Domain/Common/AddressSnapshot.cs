namespace CoreAlign.Domain.Common;

public class AddressSnapshot
{
    public string? Label { get; set; }
    public string? RecipientName { get; set; }
    public string? Phone { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public static AddressSnapshot Empty => new();

    public string ToSingleLine()
    {
        var parts = new[] { Line1, Line2, City, State, PostalCode, Country }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}

public class CustomerSnapshot
{
    public string? Code { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? NationalId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
