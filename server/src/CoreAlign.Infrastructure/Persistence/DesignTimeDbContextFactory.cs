using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreAlign.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CoreAlignDbContext>
{
    public CoreAlignDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseNpgsql("Host=localhost;Database=corealign;Username=postgres;Password=Asdqwe123")
            .Options;
        return new CoreAlignDbContext(options, new DesignTimeTenantContext(), new DesignTimePublisher());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? CurrentTenantId => null;
        public bool HasTenant => false;
        public Guid RequireTenantId() => throw new MissingTenantContextException();
        public void EnsureSameTenant(Guid resourceTenantId) { }
        public IDisposable PushScope(Guid tenantId) => new DummyScope();

        private sealed class DummyScope : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class DesignTimePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
