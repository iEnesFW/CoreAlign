using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Invoices;

namespace CoreAlign.Application.Invoices.Recurring.Handlers;

internal static class RecurringInvoiceLineBuilder
{
    public static IReadOnlyList<Guid> ProductIds(IEnumerable<RecurringInvoiceLineInput> lines) =>
        lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();

    public static List<RecurringInvoiceTemplateLine> Build(
        IEnumerable<RecurringInvoiceLineInput> lines,
        IReadOnlyDictionary<Guid, Product> products)
    {
        var result = new List<RecurringInvoiceTemplateLine>();
        foreach (var l in lines)
        {
            string sku;
            string name;
            if (l.ProductId.HasValue && products.TryGetValue(l.ProductId.Value, out var product))
            {
                sku = product.Sku;
                name = product.Name;
            }
            else
            {
                sku = string.Empty;
                name = l.ProductName ?? string.Empty;
            }

            result.Add(new RecurringInvoiceTemplateLine(
                productId: l.ProductId,
                productSku: sku,
                productName: name,
                description: l.Description,
                quantity: l.Quantity,
                unitPrice: l.UnitPrice,
                taxRatePercent: l.TaxRatePercent,
                taxRateId: l.TaxRateId,
                lineDiscountPercent: l.LineDiscountPercent,
                lineDiscountAmount: l.LineDiscountAmount,
                withholdingRatePercent: l.WithholdingRatePercent,
                isTaxInclusive: l.IsTaxInclusive,
                uomId: l.UomId,
                uomCode: l.UomCode));
        }
        return result;
    }
}
