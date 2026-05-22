namespace CoreAlign.Application.Lookups;

public record CurrencyDto(string Code, string Name, string? Symbol, bool IsActive);
public record CountryDto(string Code, string Name, string? DialCode, bool IsActive);
public record ProvinceDto(int Id, string CountryCode, string Name, bool IsActive);
public record DistrictDto(int Id, int ProvinceId, string Name, bool IsActive);

/// <summary>
/// Read-only access to global reference lookups (currencies, countries,
/// provinces, districts). These are shared across tenants, so no tenant scoping
/// applies.
/// </summary>
public interface ILookupQueryService
{
    Task<IReadOnlyList<CurrencyDto>> GetCurrenciesAsync(bool? isActive, CancellationToken ct = default);
    Task<IReadOnlyList<CountryDto>> GetCountriesAsync(bool? isActive, CancellationToken ct = default);
    Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(string? countryCode, CancellationToken ct = default);
    Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(int? provinceId, CancellationToken ct = default);
}
