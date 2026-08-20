using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Employee> Items, int Total)> GetPagedAsync(
        string? search,
        Enums.EmploymentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetActiveForPayrollAsync(DateOnly period, CancellationToken cancellationToken = default);
    // Tracked on purpose: posting a run amortises each instalment against the balance here.
    Task<IReadOnlyList<EmployeeDeduction>> GetDeductionsByIdsAsync(
        IEnumerable<Guid> deductionIds,
        CancellationToken cancellationToken = default);
    Task<bool> NumberExistsAsync(string employeeNumber, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> NationalIdExistsAsync(string nationalId, Guid? excludeId, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    void Update(Employee employee);
}
