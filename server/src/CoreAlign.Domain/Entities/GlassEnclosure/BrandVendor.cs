using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class BrandVendor : TenantEntity
{
    public Guid BrandId { get; private set; }
    public Guid VendorId { get; private set; }
    public int DefaultLeadTimeDays { get; private set; }
    public string? DefaultPaymentTerms { get; private set; }
    public bool IsPreferred { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected BrandVendor() { }

    public BrandVendor(
        Guid brandId,
        Guid vendorId,
        int defaultLeadTimeDays,
        bool isPreferred,
        string? defaultPaymentTerms = null)
    {
        BrandId = brandId;
        VendorId = vendorId;
        DefaultLeadTimeDays = defaultLeadTimeDays;
        IsPreferred = isPreferred;
        DefaultPaymentTerms = defaultPaymentTerms;
    }

    public void Update(int defaultLeadTimeDays, bool isPreferred, string? defaultPaymentTerms, bool isActive)
    {
        DefaultLeadTimeDays = defaultLeadTimeDays;
        IsPreferred = isPreferred;
        DefaultPaymentTerms = defaultPaymentTerms;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
