using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IPayrollRunRepository
{
    Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PayrollRun?> GetWithPayslipsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForPeriodAsync(int periodYear, int periodMonth, PayrollRunType runType, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PayrollRun> Items, int Total)> GetPagedAsync(
        int? periodYear,
        PayrollRunStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(PayrollRun run, CancellationToken cancellationToken = default);
    void Update(PayrollRun run);
}
