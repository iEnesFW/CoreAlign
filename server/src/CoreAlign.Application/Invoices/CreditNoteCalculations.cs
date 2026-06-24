using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Invoices;

public static class CreditNoteCalculations
{
    public static Dictionary<Guid, decimal> SumCreditedByOriginLine(IEnumerable<Invoice> creditNotes)
    {
        var totals = new Dictionary<Guid, decimal>();
        foreach (var note in creditNotes)
        {
            if (note.Status == InvoiceStatus.Cancelled || note.Status == InvoiceStatus.Void)
            {
                continue;
            }
            foreach (var line in note.Lines)
            {
                if (line.OriginOrderLineId is null)
                {
                    continue;
                }
                var key = line.OriginOrderLineId.Value;
                totals[key] = totals.GetValueOrDefault(key) + line.Quantity;
            }
        }
        return totals;
    }
}
