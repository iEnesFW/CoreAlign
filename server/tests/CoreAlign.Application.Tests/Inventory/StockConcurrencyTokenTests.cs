using CoreAlign.Application.Common.Behaviors;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Inventory;

/// <summary>
/// Rule 16 optimistic concurrency: two simultaneous decrements of the same
/// stock-holding row must NOT silently lose an update. One commit wins; the other
/// must surface a DomainConcurrencyException (HTTP 409) via ConcurrencyTokenBehavior.
///
/// ProductVariant carries StockQuantity AND implements IHasConcurrencyToken, so it
/// is the live, real-database proof of the guarantee. StockItem (the warehouse-level
/// balance) currently does NOT implement IHasConcurrencyToken — that gap is asserted
/// explicitly below and tracked in docs/sprint12-blockers.md (ERP-CONCUR-001),
/// because closing it requires a schema migration (forbidden this sprint).
/// </summary>
public class StockConcurrencyTokenTests
{
    private static (CoreAlignDbContext db, SqliteConnection conn) NewDb(Guid tenantId)
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(tenantId);

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new CoreAlignDbContext(options, tenant, Substitute.For<IPublisher>());
        db.Database.EnsureCreated();
        // Seed the parent Tenant row: TenantEntity rows now carry a tenant_id FK to
        // tenants(id) (ApplyTenantForeignKeys), which SQLite enforces.
        db.Tenants.Add(new Tenant("Test", "test") { Id = tenantId });
        db.SaveChanges();
        return (db, conn);
    }

    private static async Task<ProductVariant> SeedVariantAsync(CoreAlignDbContext db, Guid tenantId, decimal stock)
    {
        var parent = new Product("SKU-P", "Parent", "pcs", 10m, "TRY") { TenantId = tenantId };
        db.Products.Add(parent);
        var variant = new ProductVariant(parent.Id, "SKU-V", "{}", stockQuantity: stock) { TenantId = tenantId };
        db.Set<ProductVariant>().Add(variant);
        await db.SaveChangesAsync();
        return variant;
    }

    [Fact]
    public async Task Two_racing_decrements_on_same_row_lose_no_update_one_gets_concurrency_conflict()
    {
        var tenantId = Guid.NewGuid();
        var (seedDb, conn) = NewDb(tenantId);
        try
        {
            var variant = await SeedVariantAsync(seedDb, tenantId, stock: 10m);

            var optionsA = new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options;
            var optionsB = new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options;
            var tenant = Substitute.For<ITenantContext>();
            tenant.CurrentTenantId.Returns(tenantId);
            tenant.HasTenant.Returns(true);
            tenant.RequireTenantId().Returns(tenantId);

            await using var dbA = new CoreAlignDbContext(optionsA, tenant, Substitute.For<IPublisher>());
            await using var dbB = new CoreAlignDbContext(optionsB, tenant, Substitute.For<IPublisher>());

            // Both contexts load the same row at the same token.
            var vA = await dbA.Set<ProductVariant>().SingleAsync(v => v.Id == variant.Id);
            var vB = await dbB.Set<ProductVariant>().SingleAsync(v => v.Id == variant.Id);

            vA.AdjustStock(-3m);
            await dbA.SaveChangesAsync(); // wins

            vB.AdjustStock(-4m);
            Func<Task> losing = () => dbB.SaveChangesAsync();
            await losing.Should().ThrowAsync<DbUpdateConcurrencyException>();

            // Winner's decrement is persisted; loser's was rejected (no lost update).
            await using var verifyDb = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options,
                tenant, Substitute.For<IPublisher>());
            var reloaded = await verifyDb.Set<ProductVariant>().SingleAsync(v => v.Id == variant.Id);
            reloaded.StockQuantity.Should().Be(7m);
        }
        finally
        {
            await seedDb.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Concurrency_behavior_translates_stock_conflict_to_409_domain_exception()
    {
        // The pipeline behavior is what turns the EF DbUpdateConcurrencyException into
        // a DomainConcurrencyException (409) instead of bubbling a raw 500.
        var behavior = new ConcurrencyTokenBehavior<DecrementStockRequest, Unit>(
            NullLogger<ConcurrencyTokenBehavior<DecrementStockRequest, Unit>>.Instance);
        RequestHandlerDelegate<Unit> next = () => throw new DbUpdateConcurrencyException("stock row changed");

        Func<Task> act = () => behavior.Handle(new DecrementStockRequest(Guid.NewGuid(), 5m), next, CancellationToken.None);

        await act.Should().ThrowAsync<DomainConcurrencyException>();
    }

    [Fact]
    public void ProductVariant_bumps_token_so_optimistic_concurrency_can_detect_a_change()
    {
        var variant = new ProductVariant(Guid.NewGuid(), "SKU-V", "{}", stockQuantity: 5m);
        var before = variant.ConcurrencyToken;

        ((IHasConcurrencyToken)variant).BumpConcurrencyToken();

        variant.ConcurrencyToken.Should().Be(before + 1);
    }

    [Fact]
    public void StockItem_now_carries_concurrency_token_ERP_CONCUR_001_closed()
    {
        typeof(IHasConcurrencyToken).IsAssignableFrom(typeof(StockItem)).Should().BeTrue(
            "StockItem implements IHasConcurrencyToken after Phase71StockItemConcurrencyToken — ERP-CONCUR-001 closed");

        var item = new StockItem(Guid.NewGuid(), Guid.NewGuid());
        var before = item.ConcurrencyToken;
        ((IHasConcurrencyToken)item).BumpConcurrencyToken();
        item.ConcurrencyToken.Should().Be(before + 1);
    }

    [Fact]
    public async Task Two_racing_issues_on_same_StockItem_lose_no_update_one_gets_concurrency_conflict()
    {
        var tenantId = Guid.NewGuid();
        var (seedDb, conn) = NewDb(tenantId);
        try
        {
            var product = new Product("SKU-S", "Stocked", "pcs", 10m, "TRY") { TenantId = tenantId };
            seedDb.Products.Add(product);
            var warehouse = new Warehouse("WH-1", "Main") { TenantId = tenantId };
            seedDb.Warehouses.Add(warehouse);
            var item = new StockItem(product.Id, warehouse.Id) { TenantId = tenantId };
            item.ApplyReceipt(10m, 5m, DateTime.UtcNow);
            seedDb.Set<StockItem>().Add(item);
            await seedDb.SaveChangesAsync();

            var tenant = Substitute.For<ITenantContext>();
            tenant.CurrentTenantId.Returns(tenantId);
            tenant.HasTenant.Returns(true);
            tenant.RequireTenantId().Returns(tenantId);

            await using var dbA = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options, tenant, Substitute.For<IPublisher>());
            await using var dbB = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options, tenant, Substitute.For<IPublisher>());

            var sA = await dbA.Set<StockItem>().SingleAsync(s => s.Id == item.Id);
            var sB = await dbB.Set<StockItem>().SingleAsync(s => s.Id == item.Id);

            sA.ApplyIssue(3m, DateTime.UtcNow);
            await dbA.SaveChangesAsync();

            sB.ApplyIssue(4m, DateTime.UtcNow);
            Func<Task> losing = () => dbB.SaveChangesAsync();
            await losing.Should().ThrowAsync<DbUpdateConcurrencyException>();

            await using var verifyDb = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options, tenant, Substitute.For<IPublisher>());
            var reloaded = await verifyDb.Set<StockItem>().SingleAsync(s => s.Id == item.Id);
            reloaded.OnHand.Should().Be(7m);
        }
        finally
        {
            await seedDb.DisposeAsync();
            conn.Dispose();
        }
    }

    public sealed record DecrementStockRequest(Guid StockItemId, decimal Quantity) : IRequest<Unit>;
}
