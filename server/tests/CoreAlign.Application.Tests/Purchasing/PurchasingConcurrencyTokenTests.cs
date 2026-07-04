using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.Purchasing;

// Optimistic concurrency (§4.6): VendorBill/VendorPayment/PurchaseOrder used to declare
// IXminConcurrency, but xmin is a disabled no-op on PostgreSQL 18 — so these money
// aggregates had NO conflict detection. They now carry an app-managed IHasConcurrencyToken
// (like StockItem). Two simultaneous mutations of the same row must not silently lose an
// update: one commit wins, the other surfaces a DbUpdateConcurrencyException (→409).
public class PurchasingConcurrencyTokenTests
{
    [Fact]
    public void VendorBill_VendorPayment_PurchaseOrder_all_bump_their_concurrency_token()
    {
        IHasConcurrencyToken bill = new VendorBill(Guid.NewGuid(), "V", "INV-1", DateTime.UtcNow, "TRY", 100m, 0m);
        IHasConcurrencyToken payment = new VendorPayment(Guid.NewGuid(), "V", "VPAY-1", DateTime.UtcNow, 100m, "TRY");
        IHasConcurrencyToken po = new PurchaseOrder("PO-1", Guid.NewGuid(), "V", DateTime.UtcNow, "TRY");

        foreach (var entity in new[] { bill, payment, po })
        {
            var before = entity.ConcurrencyToken;
            entity.BumpConcurrencyToken();
            entity.ConcurrencyToken.Should().Be(before + 1);
        }
    }

    [Fact]
    public async Task Two_racing_payments_on_same_vendor_bill_lose_no_update_one_gets_conflict()
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

            await using var seedDb = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options, tenant, Substitute.For<IPublisher>());
            seedDb.Database.EnsureCreated();
            seedDb.Tenants.Add(new Tenant("Test", "test") { Id = tenantId });
            var vendor = new Vendor("Acme") { TenantId = tenantId };
            seedDb.Set<Vendor>().Add(vendor);
            var bill = new VendorBill(vendor.Id, "Acme", "INV-1", DateTime.UtcNow, "TRY", 1000m, 0m) { TenantId = tenantId };
            bill.Post();
            seedDb.Set<VendorBill>().Add(bill);
            await seedDb.SaveChangesAsync();

            await using var dbA = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options, tenant, Substitute.For<IPublisher>());
            await using var dbB = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options, tenant, Substitute.For<IPublisher>());

            var bA = await dbA.Set<VendorBill>().SingleAsync(b => b.Id == bill.Id);
            var bB = await dbB.Set<VendorBill>().SingleAsync(b => b.Id == bill.Id);

            bA.RecordPayment(100m);
            await dbA.SaveChangesAsync(); // wins

            bB.RecordPayment(50m);
            Func<Task> losing = () => dbB.SaveChangesAsync();
            await losing.Should().ThrowAsync<DbUpdateConcurrencyException>();

            await using var verifyDb = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options, tenant, Substitute.For<IPublisher>());
            var reloaded = await verifyDb.Set<VendorBill>().SingleAsync(b => b.Id == bill.Id);
            reloaded.AmountPaid.Should().Be(100m);
        }
        finally
        {
            conn.Dispose();
        }
    }
}
