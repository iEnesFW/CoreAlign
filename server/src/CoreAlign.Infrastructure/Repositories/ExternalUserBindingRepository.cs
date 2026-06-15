using CoreAlign.Domain.Entities.Sso;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ExternalUserBindingRepository : IExternalUserBindingRepository
{
    private readonly CoreAlignDbContext _context;

    public ExternalUserBindingRepository(CoreAlignDbContext context) => _context = context;

    public Task<ExternalUserBinding?> GetByExternalIdAsync(Guid identityProviderId, string externalUserId, CancellationToken cancellationToken = default) =>
        _context.ExternalUserBindings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.IdentityProviderId == identityProviderId && b.ExternalUserId == externalUserId, cancellationToken);

    public Task<ExternalUserBinding?> GetByLocalUserAsync(Guid identityProviderId, Guid localUserId, CancellationToken cancellationToken = default) =>
        _context.ExternalUserBindings
            .FirstOrDefaultAsync(b => b.IdentityProviderId == identityProviderId && b.LocalUserId == localUserId, cancellationToken);

    public async Task<IReadOnlyList<ExternalUserBinding>> ListByUserAsync(Guid localUserId, CancellationToken cancellationToken = default) =>
        await _context.ExternalUserBindings
            .AsNoTracking()
            .Where(b => b.LocalUserId == localUserId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ExternalUserBinding entity, CancellationToken cancellationToken = default) =>
        await _context.ExternalUserBindings.AddAsync(entity, cancellationToken);

    public void Update(ExternalUserBinding entity) => _context.ExternalUserBindings.Update(entity);
}
