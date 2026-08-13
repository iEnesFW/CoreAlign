using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.B2B.DealerPortal;

public record ResolvedDealerOrderLine(
    Product Product,
    int LineNumber,
    decimal Quantity,
    decimal UnitPrice,
    decimal? ReferenceListPrice,
    decimal DiscountPercent,
    decimal TaxRatePercent,
    Guid? TaxRateId,
    bool IsTaxInclusive,
    decimal? MinOrderQuantity);

// WHY this lives outside both handlers: the price a dealer is SHOWN and the price the order is
// BOOKED at have to come from the same resolution, or the confirmation screen quietly lies —
// the catalogue prices at quantity 1 while a basket is priced at its real quantity.
public static class DealerOrderPricingResolver
{
    public static async Task<IReadOnlyList<ResolvedDealerOrderLine>> ResolveAsync(
        IPricingService pricing,
        IReadOnlyDictionary<Guid, Product> products,
        Guid customerId,
        string orderCurrency,
        IReadOnlyList<DealerOrderLineInput> lines,
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        var resolved = new List<ResolvedDealerOrderLine>(lines.Count);
        var lineNumber = 1;

        foreach (var input in lines)
        {
            var product = products[input.ProductId];

            var minQuantity = await pricing.ResolveMinQuantityAsync(product.Id, customerId, cancellationToken);
            if (minQuantity.HasValue && input.Quantity < minQuantity.Value)
            {
                throw new MinOrderQuantityNotMetException(product.Id, lineNumber, input.Quantity, minQuantity.Value);
            }

            var resolution = await pricing.ResolveAsync(
                new PriceResolutionRequest(product.Id, customerId, input.Quantity, asOfUtc, orderCurrency),
                cancellationToken);

            if (!string.Equals(resolution.Currency, orderCurrency, StringComparison.OrdinalIgnoreCase))
            {
                throw new CurrencyMismatchException(product.Id, orderCurrency, resolution.Currency);
            }

            resolved.Add(new ResolvedDealerOrderLine(
                product,
                lineNumber++,
                input.Quantity,
                resolution.UnitPrice,
                resolution.ReferenceListPrice ?? product.ListPrice,
                resolution.DiscountPercent,
                resolution.TaxRatePercent,
                resolution.TaxRateId,
                resolution.IsTaxInclusive,
                minQuantity));
        }

        return resolved;
    }

    public static OrderLine ToOrderLine(this ResolvedDealerOrderLine resolved, string? lineNotes)
    {
        var product = resolved.Product;
        var line = new OrderLine(product.Id, product.Sku, product.Name, resolved.Quantity, resolved.UnitPrice);
        line.SetLineNumber(resolved.LineNumber);
        line.ApplyPricing(
            resolved.Quantity,
            resolved.ReferenceListPrice ?? product.ListPrice,
            resolved.UnitPrice,
            resolved.DiscountPercent,
            lineDiscountAmount: 0m,
            isManualPriceOverride: false,
            resolved.TaxRatePercent,
            resolved.TaxRateId,
            resolved.IsTaxInclusive,
            withholdingRatePercent: 0m,
            product.AverageCost,
            uomId: product.SalesUomId ?? product.BaseUomId,
            uomCode: product.Unit,
            uomConversionFactor: 1m,
            warehouseId: null,
            lineNotes: lineNotes,
            null,
            false,
            product.Description);
        return line;
    }
}
