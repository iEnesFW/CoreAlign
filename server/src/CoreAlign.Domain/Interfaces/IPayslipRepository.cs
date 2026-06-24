using CoreAlign.Domain.Entities.Payroll;

namespace CoreAlign.Domain.Interfaces;

public interface IPayslipRepository
{
    Task<Payslip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payslip>> GetByRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task AddAsync(Payslip payslip, CancellationToken cancellationToken = default);
    Task RemoveByRunAsync(Guid runId, CancellationToken cancellationToken = default);
}
