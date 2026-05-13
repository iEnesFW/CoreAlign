using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class Lot : TenantEntity
{
    public Guid ProductId { get; private set; }
    public string LotNumber { get; private set; } = string.Empty;
    public DateTime? ManufactureDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public string? SupplierLotRef { get; private set; }
    public string? CountryOfOrigin { get; private set; }
    public string? Notes { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? BlockReason { get; private set; }

    public Product Product { get; set; } = null!;

    protected Lot() { }

    public Lot(Guid productId, string lotNumber, DateTime? manufactureDate = null, DateTime? expiryDate = null, string? supplierLotRef = null)
    {
        ProductId = productId;
        LotNumber = lotNumber;
        ManufactureDate = manufactureDate;
        ExpiryDate = expiryDate;
        SupplierLotRef = supplierLotRef;
    }

    public bool IsExpired(DateTime asOfUtc) => ExpiryDate.HasValue && asOfUtc > ExpiryDate.Value;

    public void Update(string lotNumber, DateTime? manufactureDate, DateTime? expiryDate, string? supplierLotRef, string? countryOfOrigin, string? notes)
    {
        LotNumber = lotNumber;
        ManufactureDate = manufactureDate;
        ExpiryDate = expiryDate;
        SupplierLotRef = supplierLotRef;
        CountryOfOrigin = countryOfOrigin;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Block(string reason)
    {
        IsBlocked = true;
        BlockReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Unblock()
    {
        IsBlocked = false;
        BlockReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
