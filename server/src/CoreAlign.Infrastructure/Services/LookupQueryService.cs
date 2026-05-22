using CoreAlign.Application.Lookups;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Services;

public class LookupQueryService : ILookupQueryService
{
    private readonly CoreAlignDbContext _context;

    public LookupQueryService(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<CurrencyDto>> GetCurrenciesAsync(bool? isActive, CancellationToken ct = default)
    {
        var query = _context.Currencies.AsNoTracking().AsQueryable();
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        return await query
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Code, c.Name, c.Symbol, c.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CountryDto>> GetCountriesAsync(bool? isActive, CancellationToken ct = default)
    {
        var query = _context.Countries.AsNoTracking().AsQueryable();
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Code, c.Name, c.DialCode, c.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(string? countryCode, CancellationToken ct = default)
    {
        var query = _context.Provinces.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(p => p.CountryCode == countryCode);
        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProvinceDto(p.Id, p.CountryCode, p.Name, p.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(int? provinceId, CancellationToken ct = default)
    {
        var query = _context.Districts.AsNoTracking().Where(d => d.IsActive);
        if (provinceId.HasValue) query = query.Where(d => d.ProvinceId == provinceId.Value);
        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DistrictDto(d.Id, d.ProvinceId, d.Name, d.IsActive))
            .ToListAsync(ct);
    }
}
