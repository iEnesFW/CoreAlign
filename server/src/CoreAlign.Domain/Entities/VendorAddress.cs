using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class VendorAddress : TenantEntity
{
    public Guid VendorId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public bool IsPrimary { get; set; }

    public Vendor Vendor { get; set; } = null!;

    protected VendorAddress() { }

    public VendorAddress(Guid vendorId, string label, string line1)
    {
        VendorId = vendorId;
        Label = label;
        Line1 = line1;
    }

    public void Update(string label, string line1, string? line2, string? city, string? state, string? postalCode, string? country, bool isPrimary)
    {
        Label = label;
        Line1 = line1;
        Line2 = line2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        IsPrimary = isPrimary;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
