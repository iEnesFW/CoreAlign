using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Pricing;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Services;

public class PricingService : IPricingService
{
    private readonly IProductRepository _products;
    private readonly ICustomerRepository _customers;
    private readonly IPriceListRepository _priceLists;
    private readonly ICustomerProductPriceRepository _customerProductPrices;
    private readonly IPricingDiscountRuleRepository? _discountRules;
    private readonly ITaxRuleRepository? _taxRules;
    private readonly ITaxRateRepository? _taxRates;

    public PricingService(
        IProductRepository products,
        ICustomerRepository customers,
        IPriceListRepository priceLists,
        ICustomerProductPriceRepository customerProductPrices)
        : this(products, customers, priceLists, customerProductPrices, null, null, null) { }

    public PricingService(
        IProductRepository products,
        ICustomerRepository customers,
        IPriceListRepository priceLists,
        ICustomerProductPriceRepository customerProductPrices,
        IPricingDiscountRuleRepository? discountRules,
        ITaxRuleRepository? taxRules,
        ITaxRateRepository? taxRates)
    {
        _products = products;
        _customers = customers;
        _priceLists = priceLists;
        _customerProductPrices = customerProductPrices;
        _discountRules = discountRules;
        _taxRules = taxRules;
        _taxRates = taxRates;
    }

    public async Task<PriceResolutionResult> ResolveAsync(PriceResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found for pricing.");

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found for pricing.");

        var cppList = await _customerProductPrices.GetForCustomerAndProductAsync(customer.Id, product.Id, cancellationToken);

        PriceList? priceList = null;
        if (customer.PriceListId.HasValue)
        {
            priceList = await _priceLists.GetWithItemsAsync(customer.PriceListId.Value, cancellationToken);
        }

        return ResolveFromLoaded(request, product, customer, cppList, priceList);
    }

    public async Task<IReadOnlyList<PriceResolutionResult>> ResolveBatchAsync(IEnumerable<PriceResolutionRequest> requests, CancellationToken cancellationToken = default)
    {
        var reqs = requests.ToList();
        if (reqs.Count == 0)
        {
            return Array.Empty<PriceResolutionResult>();
        }

        var products = await _products.GetByIdsAsync(reqs.Select(r => r.ProductId).Distinct(), cancellationToken);
        var customers = await _customers.GetByIdsAsync(reqs.Select(r => r.CustomerId).Distinct(), cancellationToken);

        var cppByCustomer = new Dictionary<Guid, IReadOnlyList<CustomerProductPrice>>();
        var priceListByCustomer = new Dictionary<Guid, PriceList?>();
        foreach (var customerId in reqs.Select(r => r.CustomerId).Distinct())
        {
            cppByCustomer[customerId] = await _customerProductPrices.GetByCustomerAsync(customerId, cancellationToken);
            PriceList? priceList = null;
            if (customers.TryGetValue(customerId, out var cust) && cust.PriceListId.HasValue)
            {
                priceList = await _priceLists.GetWithItemsAsync(cust.PriceListId.Value, cancellationToken);
            }
            priceListByCustomer[customerId] = priceList;
        }

        var results = new List<PriceResolutionResult>(reqs.Count);
        foreach (var req in reqs)
        {
            if (!products.TryGetValue(req.ProductId, out var product))
            {
                throw new InvalidOperationException("Product not found for pricing.");
            }
            if (!customers.TryGetValue(req.CustomerId, out var customer))
            {
                throw new InvalidOperationException("Customer not found for pricing.");
            }
            var cppList = cppByCustomer[req.CustomerId].Where(c => c.ProductId == product.Id).ToList();
            results.Add(ResolveFromLoaded(req, product, customer, cppList, priceListByCustomer[req.CustomerId]));
        }
        return results;
    }

    private static PriceResolutionResult ResolveFromLoaded(
        PriceResolutionRequest request,
        Product product,
        Customer customer,
        IReadOnlyList<CustomerProductPrice> cppList,
        PriceList? priceList)
    {
        var currency = request.RequestedCurrency ?? customer.DefaultCurrency ?? product.Currency;

        var matchingCpp = cppList
            .Where(p => p.IsValid(request.AsOfUtc, request.Quantity))
            .OrderByDescending(p => p.UpdatedAtUtc)
            .FirstOrDefault();
        if (matchingCpp is not null)
        {
            return new PriceResolutionResult(
                UnitPrice: matchingCpp.Price,
                Currency: matchingCpp.Currency,
                DiscountPercent: matchingCpp.DiscountPercent ?? 0m,
                Source: PriceSource.CustomerProductPrice,
                SourceLabel: "Customer-specific price",
                ReferenceListPrice: product.ListPrice == 0 ? product.Price : product.ListPrice,
                TaxRatePercent: 0m,
                IsTaxInclusive: product.IsPriceTaxInclusive,
                TaxRateId: product.TaxRateId,
                AppliedRecordId: matchingCpp.Id);
        }

        if (priceList is not null && priceList.IsCurrentlyValid(request.AsOfUtc))
        {
            var item = priceList.Items
                .Where(i => i.ProductId == product.Id && i.MatchesQuantity(request.Quantity))
                .OrderByDescending(i => i.UpdatedAtUtc)
                .FirstOrDefault();
            if (item is not null)
            {
                return new PriceResolutionResult(
                    UnitPrice: item.Price,
                    Currency: priceList.Currency,
                    DiscountPercent: item.DiscountPercent ?? 0m,
                    Source: PriceSource.PriceList,
                    SourceLabel: $"Price list: {priceList.Name}",
                    ReferenceListPrice: product.ListPrice == 0 ? product.Price : product.ListPrice,
                    TaxRatePercent: 0m,
                    IsTaxInclusive: priceList.IsTaxInclusive,
                    TaxRateId: product.TaxRateId,
                    AppliedRecordId: item.Id);
            }
        }

        var unitPrice = product.ListPrice > 0 ? product.ListPrice : product.Price;
        return new PriceResolutionResult(
            UnitPrice: unitPrice,
            Currency: currency,
            DiscountPercent: customer.DefaultDiscountPercent,
            Source: PriceSource.ProductListPrice,
            SourceLabel: "Product list price",
            ReferenceListPrice: unitPrice,
            TaxRatePercent: 0m,
            IsTaxInclusive: product.IsPriceTaxInclusive,
            TaxRateId: product.TaxRateId,
            AppliedRecordId: null);
    }

    public async Task<decimal?> ResolveMinQuantityAsync(Guid productId, Guid customerId, CancellationToken cancellationToken = default)
    {
        // Customer-specific override wins when it has an explicit MinOrderQuantityOverride.
        var cppList = await _customerProductPrices.GetForCustomerAndProductAsync(customerId, productId, cancellationToken);
        var override_ = cppList
            .Where(p => p.IsCurrentlyValid(DateTime.UtcNow) && p.MinOrderQuantityOverride.HasValue)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .FirstOrDefault();
        if (override_ is not null) return override_.MinOrderQuantityOverride;

        // Fall back to the product-level minimum.
        var product = await _products.GetByIdAsync(productId, cancellationToken);
        return product?.MinOrderQuantity;
    }

    public async Task<TaxResolutionResult> ResolveTaxAsync(TaxResolutionContext context, CancellationToken cancellationToken = default)
    {
        if (_taxRules is not null)
        {
            var rules = await _taxRules.ListActiveAtAsync(context.AsOfUtc, cancellationToken);
            var matched = rules
                .Where(r => r.MatchesContext(context.CustomerRegionCode, context.ProductClass, context.ProductCategoryId, context.ProductId, context.AsOfUtc))
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.UpdatedAtUtc)
                .FirstOrDefault();
            if (matched is not null)
            {
                if (matched.RatePercent <= 0m && matched.FallbackTaxRateId.HasValue && _taxRates is not null)
                {
                    var fallback = await _taxRates.GetByIdAsync(matched.FallbackTaxRateId.Value, cancellationToken);
                    if (fallback is not null)
                    {
                        return new TaxResolutionResult(fallback.RatePercent, matched.Id, matched.FallbackTaxRateId, $"TaxRule:{matched.Code}");
                    }
                }
                return new TaxResolutionResult(matched.RatePercent, matched.Id, matched.FallbackTaxRateId, $"TaxRule:{matched.Code}");
            }
        }

        var product = await _products.GetByIdAsync(context.ProductId, cancellationToken);
        if (product?.TaxRateId is Guid trid && _taxRates is not null)
        {
            var rate = await _taxRates.GetByIdAsync(trid, cancellationToken);
            if (rate is not null)
            {
                return new TaxResolutionResult(rate.RatePercent, null, trid, $"TaxRate:{rate.Code}");
            }
        }

        return new TaxResolutionResult(0m, null, null, "None");
    }

    public async Task<DiscountResolutionResult> ResolveDiscountAsync(DiscountResolutionContext context, CancellationToken cancellationToken = default)
    {
        if (_discountRules is null)
        {
            return new DiscountResolutionResult(0m, 0m, null, null);
        }

        var rules = await _discountRules.ListActiveAtAsync(context.AsOfUtc, cancellationToken);
        var match = rules
            .Where(r => r.MatchesContext(context.CustomerGroupId, context.ProductCategoryId, context.ProductId, context.Quantity, context.AsOfUtc))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.UpdatedAtUtc)
            .FirstOrDefault();
        if (match is null)
        {
            return new DiscountResolutionResult(0m, 0m, null, null);
        }

        var amount = match.ApplyTo(context.LineSubtotal);
        var percent = match.ValueType == DiscountValueType.Percent
            ? match.Value
            : (context.LineSubtotal > 0m ? Math.Round(amount * 100m / context.LineSubtotal, 4) : 0m);
        return new DiscountResolutionResult(amount, percent, match.Id, match.Code);
    }
}
