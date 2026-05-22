using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class GLAccountRepository : IGLAccountRepository
{
    private readonly CoreAlignDbContext _context;

    public GLAccountRepository(CoreAlignDbContext context) => _context = context;

    public Task<GLAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GLAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<GLAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.GLAccounts.FirstOrDefaultAsync(a => a.Code == code, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.GLAccounts.AsNoTracking().Where(a => a.Code == code);
        if (excludeId.HasValue) query = query.Where(a => a.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasChildrenAsync(Guid parentId, CancellationToken cancellationToken = default) =>
        _context.GLAccounts.AsNoTracking().AnyAsync(a => a.ParentId == parentId, cancellationToken);

    public async Task<IReadOnlyList<GLAccount>> ListAsync(
        AccountType? type,
        bool? isActive,
        bool? isPostable,
        Guid? parentId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.GLAccounts.AsNoTracking().AsQueryable();
        if (type.HasValue) query = query.Where(a => a.Type == type.Value);
        if (isActive.HasValue) query = query.Where(a => a.IsActive == isActive.Value);
        if (isPostable.HasValue) query = query.Where(a => a.IsPostable == isPostable.Value);
        if (parentId.HasValue) query = query.Where(a => a.ParentId == parentId.Value);
        return await query
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GLAccount>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.GLAccounts
            .AsNoTracking()
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(GLAccount account, CancellationToken cancellationToken = default) =>
        await _context.GLAccounts.AddAsync(account, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<GLAccount> accounts, CancellationToken cancellationToken = default) =>
        await _context.GLAccounts.AddRangeAsync(accounts, cancellationToken);

    public void Update(GLAccount account) => _context.GLAccounts.Update(account);

    public void Remove(GLAccount account) => _context.GLAccounts.Remove(account);
}
