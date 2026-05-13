using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class PaymentApplication : TenantEntity
{
    public Guid PaymentId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public DateTime AppliedAtUtc { get; private set; } = DateTime.UtcNow;

    public Payment Payment { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;

    protected PaymentApplication() { }

    public PaymentApplication(Guid paymentId, Guid invoiceId, decimal appliedAmount)
    {
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        AppliedAmount = appliedAmount;
        AppliedAtUtc = DateTime.UtcNow;
    }
}
