using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Domain.Interfaces;

public interface IEmployeeYtdTaxBaseRepository
{
    Task AcquireEmployeeYearLockAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);
    Task<EmployeeYtdTaxBase?> GetAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeYtdTaxBase>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task AddAsync(EmployeeYtdTaxBase ytd, CancellationToken cancellationToken = default);
    void Update(EmployeeYtdTaxBase ytd);
}
