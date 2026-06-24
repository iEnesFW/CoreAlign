using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class PayslipRepository : IPayslipRepository
{
    private readonly CoreAlignDbContext _context;
    public PayslipRepository(CoreAlignDbContext context) => _context = context;

    public Task<Payslip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Payslips
            .AsSplitQuery()
            .Include(p => p.EarningLines)
            .Include(p => p.DeductionLines)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Payslip>> GetByRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await _context.Payslips
            .AsSplitQuery()
            .Include(p => p.EarningLines)
            .Include(p => p.DeductionLines)
            .Where(p => p.RunId == runId)
            .OrderBy(p => p.EmployeeNumber)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Payslip payslip, CancellationToken cancellationToken = default) =>
        await _context.Payslips.AddAsync(payslip, cancellationToken);

    public async Task RemoveByRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var payslips = await _context.Payslips
            .Include(p => p.EarningLines)
            .Include(p => p.DeductionLines)
            .Where(p => p.RunId == runId)
            .ToListAsync(cancellationToken);
        if (payslips.Count == 0) return;

        _context.PayslipEarningLines.RemoveRange(payslips.SelectMany(p => p.EarningLines));
        _context.PayslipDeductionLines.RemoveRange(payslips.SelectMany(p => p.DeductionLines));
        _context.Payslips.RemoveRange(payslips);
    }
}
