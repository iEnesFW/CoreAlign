namespace CoreAlign.Domain.Exceptions;

public class FxRateNotFoundException : DomainException
{
    public FxRateNotFoundException(string currency, DateTime asOf)
        : base($"No exchange rate found for {currency} as of {asOf:yyyy-MM-dd}; a foreign-currency document cannot be booked without a rate.") { }
}

public class CurrencyNotFoundException : NotFoundException
{
    public CurrencyNotFoundException(string code)
        : base($"Currency '{code}' was not found in the catalogue.") { }
}
