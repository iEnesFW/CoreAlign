using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class EmployeeYtdTaxBaseRepository : IEmployeeYtdTaxBaseRepository
{
    private readonly CoreAlignDbContext _context;
    public EmployeeYtdTaxBaseRepository(CoreAlignDbContext context) => _context = context;

    public async Task AcquireEmployeeYearLockAsync(Guid employeeId, int year, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsNpgsql())
        {
            return;
        }
        var key = $"payroll-ytd:{employeeId}:{year}";
        await _context.Database
            .ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
                cancellationToken);
    }

    public Task<EmployeeYtdTaxBase?> GetAsync(Guid employeeId, int year, CancellationToken cancellationToken = default) =>
        _context.EmployeeYtdTaxBases
            .FirstOrDefaultAsync(y => y.EmployeeId == employeeId && y.Year == year, cancellationToken);

    public async Task<IReadOnlyList<EmployeeYtdTaxBase>> GetByYearAsync(int year, CancellationToken cancellationToken = default) =>
        await _context.EmployeeYtdTaxBases
            .Where(y => y.Year == year)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(EmployeeYtdTaxBase ytd, CancellationToken cancellationToken = default) =>
        await _context.EmployeeYtdTaxBases.AddAsync(ytd, cancellationToken);

    public void Update(EmployeeYtdTaxBase ytd) => _context.EmployeeYtdTaxBases.Update(ytd);
}
