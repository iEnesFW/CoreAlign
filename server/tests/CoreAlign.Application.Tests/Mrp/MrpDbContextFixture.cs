using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.Mrp;

internal sealed class MrpDbContextFixture : IAsyncDisposable
{
    public CoreAlignDbContext Db { get; }
    public ITenantContext TenantContext { get; }
    public Guid TenantId { get; }

    private MrpDbContextFixture(CoreAlignDbContext db, ITenantContext tenant, Guid tenantId)
    {
        Db = db;
        TenantContext = tenant;
        TenantId = tenantId;
    }

    public static async Task<MrpDbContextFixture> CreateAsync(Guid? tenantId = null)
    {
        var tenant = tenantId ?? Guid.NewGuid();

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CurrentTenantId.Returns(tenant);
        tenantContext.HasTenant.Returns(true);
        tenantContext.RequireTenantId().Returns(tenant);

        var publisher = Substitute.For<IPublisher>();

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase($"mrp-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new CoreAlignDbContext(options, tenantContext, publisher);
        await db.Database.EnsureCreatedAsync();

        return new MrpDbContextFixture(db, tenantContext, tenant);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
    }
}
