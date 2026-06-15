using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class VendorPaymentApplication : TenantEntity
{
    public Guid VendorPaymentId { get; private set; }
    public Guid VendorBillId { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public DateTime AppliedAtUtc { get; private set; } = DateTime.UtcNow;
    public Guid? AppliedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public VendorPayment VendorPayment { get; set; } = null!;
    public VendorBill VendorBill { get; set; } = null!;

    protected VendorPaymentApplication() { }

    public VendorPaymentApplication(
        Guid vendorPaymentId,
        Guid vendorBillId,
        decimal appliedAmount,
        Guid? appliedByUserId = null,
        string? notes = null)
    {
        VendorPaymentId = vendorPaymentId;
        VendorBillId = vendorBillId;
        AppliedAmount = Math.Round(appliedAmount, 4);
        AppliedAtUtc = DateTime.UtcNow;
        AppliedByUserId = appliedByUserId;
        Notes = notes;
    }
}
