using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public class PreviewDealerOrderPricingHandler
    : IRequestHandler<PreviewDealerOrderPricingQuery, DealerOrderPricingPreviewDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IPricingService _pricing;

    public PreviewDealerOrderPricingHandler(
        IPortalScopeService scope,
        ICustomerRepository customers,
        IProductRepository products,
        IPricingService pricing)
    {
        _scope = scope;
        _customers = customers;
        _products = products;
        _pricing = pricing;
    }

    public async Task<DealerOrderPricingPreviewDto> Handle(
        PreviewDealerOrderPricingQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Lines is null || request.Lines.Count == 0)
        {
            throw new InvalidOrderLineException("At least one order line is required.");
        }

        var allowed = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);
        if (!allowed.Contains(request.CustomerId))
        {
            throw new DealerCustomerNotAuthorizedException();
        }

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }

        var currency = !string.IsNullOrWhiteSpace(request.Currency)
            ? request.Currency!
            : customer.DefaultCurrency;

        var resolutions = await DealerOrderPricingResolver.ResolveAsync(
            _pricing,
            products,
            customer.Id,
            currency,
            request.Lines,
            DateTime.UtcNow,
            cancellationToken);

        // WHY the real OrderLine is built: the preview has to show the totals the order will carry,
        // and OrderLine.ApplyPricing is where discount, tax and rounding actually happen. Recomputing
        // them here would be a second implementation of the same arithmetic.
        var lines = resolutions
            .Select((resolved, index) =>
            {
                var line = resolved.ToOrderLine(request.Lines[index].LineNotes);
                return new DealerOrderPricingPreviewLineDto
                {
                    ProductId = resolved.Product.Id,
                    ProductSku = resolved.Product.Sku,
                    ProductName = resolved.Product.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    ReferenceListPrice = line.ListPriceSnapshot,
                    DiscountPercent = line.LineDiscountPercent,
                    TaxRatePercent = line.TaxRatePercent,
                    LineSubtotal = line.LineSubtotal,
                    LineNetAmount = line.LineNetAmount,
                    TaxAmount = line.TaxAmount,
                    LineTotal = line.LineTotal,
                    MinOrderQuantity = resolved.MinOrderQuantity,
                };
            })
            .ToList();

        return new DealerOrderPricingPreviewDto
        {
            Currency = currency,
            Lines = lines,
            Subtotal = lines.Sum(l => l.LineNetAmount),
            TaxTotal = lines.Sum(l => l.TaxAmount),
            Total = lines.Sum(l => l.LineTotal),
        };
    }
}
