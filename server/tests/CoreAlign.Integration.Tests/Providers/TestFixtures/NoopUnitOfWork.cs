using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>
/// No-op <see cref="IUnitOfWork"/> for integration harness tests that wire the
/// real <c>PaymentDispatcher</c> against in-memory repositories. The in-memory
/// repositories already commit on Add/Update, so <c>SaveChangesAsync</c> only
/// needs to satisfy the constructor.
/// </summary>
public sealed class NoopUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IUnitOfWorkTransaction>(new NoopTransaction());

    public void ClearChangeTracker()
    {
    }

    private sealed class NoopTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
