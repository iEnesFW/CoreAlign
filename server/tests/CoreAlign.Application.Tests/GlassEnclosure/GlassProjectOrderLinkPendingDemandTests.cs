using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.GlassEnclosure;

/// <summary>
/// Real-DbContext proof for the convert-time over-commit fix: the pending-demand query joins
/// links -> orders -> lines, sums per product, counts ONLY other projects' pre-stock-effect orders
/// (Draft/Submitted/Approved) and excludes the current project. A mocked repo would hide any EF
/// translation error, so this drives the actual query over the live model.
/// </summary>
public class GlassProjectOrderLinkPendingDemandTests
{
    private static readonly OrderStatus[] PendingStatuses =
        { OrderStatus.Draft, OrderStatus.Submitted, OrderStatus.Approved };

    private static (CoreAlignDbContext db, SqliteConnection conn) NewDb(Guid tenantId)
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(tenantId);

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options;
        var db = new CoreAlignDbContext(options, tenant, Substitute.For<IPublisher>());
        db.Database.EnsureCreated();
        db.Tenants.Add(new Tenant("Test", "test") { Id = tenantId });
        db.SaveChanges();
        return (db, conn);
    }

    private static async Task<Order> SeedLinkedOrderAsync(
        CoreAlignDbContext db,
        Guid tenantId,
        Guid customerId,
        Guid productId,
        decimal quantity,
        string orderNumber)
    {
        var project = new GlassProject($"PRJ-{orderNumber}", customerId, "P", Guid.NewGuid()) { TenantId = tenantId };
        db.Set<GlassProject>().Add(project);

        var order = new Order(orderNumber, customerId, DateTime.UtcNow, "TRY") { TenantId = tenantId };
        order.ReplaceLines(new[] { new OrderLine(productId, "SKU", "Name", quantity, 10m) });
        db.Set<Order>().Add(order);
        db.Set<GlassProjectOrderLink>().Add(new GlassProjectOrderLink(project.Id, order.Id) { TenantId = tenantId });
        await db.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task SumPendingOrderDemand_counts_only_other_projects_pending_orders()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var customer = new Customer("Test Customer") { TenantId = tenantId };
            db.Set<Customer>().Add(customer);
            var productEntity = new Product("SKU", "Name", "pcs", 0m, "TRY") { TenantId = tenantId };
            db.Set<Product>().Add(productEntity);
            await db.SaveChangesAsync();

            var product = productEntity.Id;

            // Another project's Draft order — SHOULD count (6).
            await SeedLinkedOrderAsync(db, tenantId, customer.Id, product, 6m, "SO-DRAFT-A");

            // The current project's own Draft order — MUST be excluded by project id (3).
            var currentOrder = await SeedLinkedOrderAsync(db, tenantId, customer.Id, product, 3m, "SO-CURRENT");
            var currentProjectId = (await db.Set<GlassProjectOrderLink>()
                .AsNoTracking().FirstAsync(l => l.OrderId == currentOrder.Id)).ProjectId;

            // Another project's order driven to Allocated (stock already reserved) — MUST be excluded (5).
            var allocated = await SeedLinkedOrderAsync(db, tenantId, customer.Id, product, 5m, "SO-ALLOC");
            allocated.Submit();
            allocated.Approve(Guid.NewGuid());
            allocated.MarkAllocated(null);
            db.Set<Order>().Update(allocated);
            await db.SaveChangesAsync();

            // Another project's Cancelled order — MUST be excluded (4).
            var cancelled = await SeedLinkedOrderAsync(db, tenantId, customer.Id, product, 4m, "SO-CANCEL");
            cancelled.Cancel("test");
            db.Set<Order>().Update(cancelled);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var repo = new GlassProjectOrderLinkRepository(db);

            var demand = await repo.SumPendingOrderDemandByProductsAsync(
                new[] { product }, currentProjectId, PendingStatuses);

            demand.Should().ContainKey(product);
            demand[product].Should().Be(6m); // only SO-DRAFT-A; current/allocated/cancelled excluded

            var none = await repo.SumPendingOrderDemandByProductsAsync(
                new[] { Guid.NewGuid() }, currentProjectId, PendingStatuses);
            none.Should().BeEmpty();
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }
}
