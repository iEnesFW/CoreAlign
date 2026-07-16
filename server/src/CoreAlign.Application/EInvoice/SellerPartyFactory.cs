using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.EInvoice;

public static class SellerPartyFactory
{
    public static bool HasTaxIdentity(Tenant tenant) =>
        !string.IsNullOrWhiteSpace(tenant.TaxNumber) || !string.IsNullOrWhiteSpace(tenant.NationalId);

    public static SellerParty FromTenant(Tenant tenant) =>
        new(
            Name: tenant.LegalName ?? tenant.TradeName ?? tenant.Name,
            TaxNumber: tenant.TaxNumber,
            NationalId: tenant.NationalId,
            TaxOffice: tenant.TaxOffice,
            AddressLine: tenant.AddressLine1,
            City: tenant.City,
            PostalCode: tenant.PostalCode,
            Country: tenant.Country ?? "Türkiye");
}
