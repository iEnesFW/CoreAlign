using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly CoreAlignDbContext _context;
    public EmployeeRepository(CoreAlignDbContext context) => _context = context;

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Employees
            .Include(e => e.SalaryComponents)
            .Include(e => e.Deductions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Employee> Items, int Total)> GetPagedAsync(
        string? search,
        EmploymentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees.AsNoTracking().AsSplitQuery().AsQueryable();
        if (status.HasValue) query = query.Where(e => e.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.FirstName, $"%{term}%")
                || EF.Functions.Like(e.LastName, $"%{term}%")
                || EF.Functions.Like(e.EmployeeNumber, $"%{term}%")
                || (e.Department != null && EF.Functions.Like(e.Department, $"%{term}%"))
                || (e.Title != null && EF.Functions.Like(e.Title, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Employee>> GetActiveForPayrollAsync(DateOnly period, CancellationToken cancellationToken = default)
    {
        var periodStart = new DateOnly(period.Year, period.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        return await _context.Employees
            .AsSplitQuery()
            .Include(e => e.SalaryComponents)
            .Include(e => e.Deductions)
            .Where(e => e.HireDate <= periodEnd
                && (e.Status == EmploymentStatus.Active
                    || e.Status == EmploymentStatus.OnLeave
                    || (e.Status == EmploymentStatus.Terminated
                        && e.TerminationDate != null
                        && e.TerminationDate >= periodStart)))
            .OrderBy(e => e.EmployeeNumber)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NumberExistsAsync(string employeeNumber, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.Employees.AnyAsync(
            e => e.EmployeeNumber == employeeNumber && (excludeId == null || e.Id != excludeId), cancellationToken);

    public Task<bool> NationalIdExistsAsync(string nationalId, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.Employees.AnyAsync(
            e => e.NationalId == nationalId && (excludeId == null || e.Id != excludeId), cancellationToken);

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default) =>
        await _context.Employees.AddAsync(employee, cancellationToken);

    public void Update(Employee employee) => _context.Employees.Update(employee);
}
