using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class TaxDeclarationRepository : ITaxDeclarationRepository
{
    private readonly CoreAlignDbContext _context;

    public TaxDeclarationRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<TaxDeclaration?> GetByPeriodAsync(
        int year,
        int month,
        TaxDeclarationType declarationType,
        CancellationToken cancellationToken = default)
    {
        return _context.TaxDeclarations
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(
                d => d.Year == year && d.Month == month && d.DeclarationType == declarationType,
                cancellationToken);
    }

    public Task<TaxDeclaration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.TaxDeclarations
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaxDeclaration>> ListAsync(
        int? year,
        TaxDeclarationType? declarationType,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TaxDeclarations.AsNoTracking().AsQueryable();
        if (year.HasValue) query = query.Where(d => d.Year == year.Value);
        if (declarationType.HasValue) query = query.Where(d => d.DeclarationType == declarationType.Value);
        return await query
            .OrderByDescending(d => d.Year)
            .ThenByDescending(d => d.Month)
            .ThenBy(d => d.DeclarationType)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaxDeclaration declaration, CancellationToken cancellationToken = default)
    {
        await _context.TaxDeclarations.AddAsync(declaration, cancellationToken);
    }

    public void Update(TaxDeclaration declaration)
    {
        _context.TaxDeclarations.Update(declaration);
    }
}
