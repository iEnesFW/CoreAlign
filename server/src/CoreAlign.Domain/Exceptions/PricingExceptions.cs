namespace CoreAlign.Domain.Exceptions;

public sealed class PriceListNotFoundException : NotFoundException
{
    public PriceListNotFoundException(Guid id) : base($"PriceList '{id}' was not found.") { }
}

public sealed class PriceListItemNotFoundException : NotFoundException
{
    public PriceListItemNotFoundException(Guid id) : base($"PriceListItem '{id}' was not found.") { }
}

public sealed class PriceListItemConflictException : ConflictException
{
    public PriceListItemConflictException(Guid priceListId, Guid productId)
        : base($"PriceList '{priceListId}' already has an item for product '{productId}' in the same quantity tier.") { }
}

public sealed class DiscountRuleNotFoundException : NotFoundException
{
    public DiscountRuleNotFoundException(Guid id) : base($"DiscountRule '{id}' was not found.") { }
}

public sealed class DiscountRuleCodeConflictException : ConflictException
{
    public DiscountRuleCodeConflictException(string code)
        : base($"DiscountRule code '{code}' already exists.") { }
}

public sealed class TaxRuleNotFoundException : NotFoundException
{
    public TaxRuleNotFoundException(Guid id) : base($"TaxRule '{id}' was not found.") { }
}

public sealed class TaxRuleCodeConflictException : ConflictException
{
    public TaxRuleCodeConflictException(string code)
        : base($"TaxRule code '{code}' already exists.") { }
}

public sealed class CurrencyMismatchException : ConflictException
{
    public CurrencyMismatchException(Guid productId, string orderCurrency, string resolvedCurrency)
        : base($"Pricing currency mismatch for product {productId}: order is {orderCurrency} but resolved price is {resolvedCurrency}.")
    {
        ProductId = productId;
        OrderCurrency = orderCurrency;
        ResolvedCurrency = resolvedCurrency;
    }

    public Guid ProductId { get; }
    public string OrderCurrency { get; }
    public string ResolvedCurrency { get; }
}
