using CoreAlign.Application.Customers.Maintenance;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Customers;

public sealed class CustomerBalanceRecomputeDataSource : ICustomerBalanceRecomputeDataSource
{
    private readonly CoreAlignDbContext _context;

    public CustomerBalanceRecomputeDataSource(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<Guid>> GetTenantIdsWithCustomersAsync(CancellationToken ct = default)
    {
        return await _context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(c => c.TenantId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
