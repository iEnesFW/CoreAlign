using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoreAlign.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CoreAlignDbContext _context;

    public UnitOfWork(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public void ClearChangeTracker() => _context.ChangeTracker.Clear();

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return NoopTransaction.Instance;
        }
        var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransaction(tx);
    }

    private sealed class EfTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _tx;
        private bool _disposed;

        public EfTransaction(IDbContextTransaction tx)
        {
            _tx = tx;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => _tx.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => _tx.RollbackAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            await _tx.DisposeAsync();
            _disposed = true;
        }
    }

    private sealed class NoopTransaction : IUnitOfWorkTransaction
    {
        public static readonly NoopTransaction Instance = new();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
