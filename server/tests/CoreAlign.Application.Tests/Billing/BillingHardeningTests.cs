using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.Billing;

/// <summary>
/// Phase142. Two provisioning drains of the same paid order both read "Paid, not completed" and
/// both extend the grant, so the tenant silently gets double the paid time. Both aggregates now
/// carry an app-managed token (xmin is a no-op on PG18, §4.6), so the loser gets a 409 and retries
/// into the AlreadyCompleted no-op.
/// </summary>
public class BillingConcurrencyTokenTests
{
    [Fact]
    public void SubscriptionOrder_and_TenantModule_bump_their_concurrency_token()
    {
        IHasConcurrencyToken order = new SubscriptionOrder("SUB-1", Guid.NewGuid(), "TRY");
        IHasConcurrencyToken grant = new TenantModule(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(30), TenantModuleSource.Paid);

        foreach (var entity in new[] { order, grant })
        {
            var before = entity.ConcurrencyToken;
            entity.BumpConcurrencyToken();
            entity.ConcurrencyToken.Should().Be(before + 1);
        }
    }

    [Fact]
    public async Task Two_racing_extensions_of_the_same_grant_lose_no_update_one_gets_conflict()
    {
        var tenantId = Guid.NewGuid();
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        try
        {
            var tenant = Substitute.For<ITenantContext>();
            tenant.CurrentTenantId.Returns(tenantId);
            tenant.HasTenant.Returns(true);
            tenant.RequireTenantId().Returns(tenantId);

            DbContextOptions<CoreAlignDbContext> Options() =>
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options;

            await using var seedDb = new CoreAlignDbContext(Options(), tenant, Substitute.For<IPublisher>());
            seedDb.Database.EnsureCreated();
            seedDb.Tenants.Add(new Tenant("Test", "test") { Id = tenantId });
            var module = new Module("SALES", "Sales", null, "Test", "box", 0, isActive: true, isCore: false);
            seedDb.Set<Module>().Add(module);
            var grant = new TenantModule(module.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(30), TenantModuleSource.Paid)
            {
                TenantId = tenantId,
            };
            seedDb.Set<TenantModule>().Add(grant);
            await seedDb.SaveChangesAsync();

            await using var dbA = new CoreAlignDbContext(Options(), tenant, Substitute.For<IPublisher>());
            await using var dbB = new CoreAlignDbContext(Options(), tenant, Substitute.For<IPublisher>());

            var gA = await dbA.Set<TenantModule>().SingleAsync(g => g.Id == grant.Id);
            var gB = await dbB.Set<TenantModule>().SingleAsync(g => g.Id == grant.Id);

            gA.Extend(30);
            await dbA.SaveChangesAsync();

            gB.Extend(30);
            Func<Task> losing = () => dbB.SaveChangesAsync();
            await losing.Should().ThrowAsync<DbUpdateConcurrencyException>();

            await using var verifyDb = new CoreAlignDbContext(Options(), tenant, Substitute.For<IPublisher>());
            var reloaded = await verifyDb.Set<TenantModule>().SingleAsync(g => g.Id == grant.Id);
            reloaded.EndUtc.Should().BeCloseTo(grant.EndUtc!.Value.AddDays(30), TimeSpan.FromSeconds(5));
        }
        finally
        {
            conn.Dispose();
        }
    }

    /// <summary>
    /// The grant must not outlive the catalogue row it points at; before Phase142 module_id was a
    /// soft Guid, so deleting a module left grants nothing resolves.
    /// </summary>
    [Fact]
    public async Task A_grant_cannot_reference_a_module_that_does_not_exist()
    {
        var tenantId = Guid.NewGuid();
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        try
        {
            var tenant = Substitute.For<ITenantContext>();
            tenant.CurrentTenantId.Returns(tenantId);
            tenant.HasTenant.Returns(true);
            tenant.RequireTenantId().Returns(tenantId);

            await using var db = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options,
                tenant,
                Substitute.For<IPublisher>());
            db.Database.EnsureCreated();
            db.Tenants.Add(new Tenant("Test", "test") { Id = tenantId });
            await db.SaveChangesAsync();

            db.Set<TenantModule>().Add(
                new TenantModule(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(30), TenantModuleSource.Paid)
                {
                    TenantId = tenantId,
                });

            Func<Task> act = () => db.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }
        finally
        {
            conn.Dispose();
        }
    }
}
