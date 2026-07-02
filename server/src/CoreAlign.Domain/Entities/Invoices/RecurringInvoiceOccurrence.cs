using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Invoices;

public class RecurringInvoiceOccurrence : TenantEntity
{
    public Guid TemplateId { get; private set; }
    public RecurringInvoiceTemplate? Template { get; private set; }
    public DateOnly PeriodKey { get; private set; }
    public Guid GeneratedInvoiceId { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }

    protected RecurringInvoiceOccurrence() { }

    public RecurringInvoiceOccurrence(DateOnly periodKey, Guid generatedInvoiceId, DateTime generatedAtUtc)
    {
        if (generatedInvoiceId == Guid.Empty)
            throw new ArgumentException("Generated invoice id is required.", nameof(generatedInvoiceId));

        PeriodKey = periodKey;
        GeneratedInvoiceId = generatedInvoiceId;
        GeneratedAtUtc = DateTime.SpecifyKind(generatedAtUtc, DateTimeKind.Utc);
    }

    internal void AttachTo(RecurringInvoiceTemplate template)
    {
        Template = template;
        TemplateId = template.Id;
        TenantId = template.TenantId;
    }
}
