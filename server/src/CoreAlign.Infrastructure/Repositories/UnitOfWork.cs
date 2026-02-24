using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;

namespace CoreAlign.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CoreAlignDbContext _context;

    public UnitOfWork(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
