using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Domain.Interfaces;

public interface IPayrollParametersRepository
{
    Task<PayrollParameters?> ResolveAsync(int year, DateOnly period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollParameters>> ListAsync(int? year, CancellationToken cancellationToken = default);
    Task<PayrollParameters?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PayrollParameters?> GetOwnedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(PayrollParameters parameters, CancellationToken cancellationToken = default);
    void Update(PayrollParameters parameters);
}
