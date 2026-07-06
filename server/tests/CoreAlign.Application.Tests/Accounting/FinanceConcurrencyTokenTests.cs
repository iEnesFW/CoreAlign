using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.Accounting;

// §4.6 remediation (Phase119): Invoice/Order/Payment/JournalEntry/CustomerLedgerEntry/
// VendorLedgerEntry/Employee/PayrollRun/Payslip declared the IXminConcurrency marker, but xmin
// is a disabled no-op on PostgreSQL 18 (CoreAlignDbContext.ApplyXminConcurrencyTokens) — so
// these money aggregates had NO conflict detection (last-write-wins). They now carry an
// app-managed IHasConcurrencyToken (like StockItem/VendorBill). These lock (a) the token is
// mapped as an optimistic-concurrency token for all nine, and (b) a real two-context race
// surfaces a DbUpdateConcurrencyException instead of silently losing an update.
public class FinanceConcurrencyTokenTests
{
    private static readonly Type[] FinanceAggregates =
    {
        typeof(Invoice), typeof(Order), typeof(Payment), typeof(JournalEntry),
        typeof(CustomerLedgerEntry), typeof(VendorLedgerEntry),
        typeof(Employee), typeof(PayrollRun), typeof(Payslip),
    };

    private static CoreAlignDbContext NewDb(SqliteConnection conn, Guid tenantId)
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(tenantId);
        return new CoreAlignDbContext(
            new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options,
            tenant,
            Substitute.For<IPublisher>());
    }

    [Fact]
    public void All_nine_finance_aggregates_map_concurrency_token_as_a_concurrency_token()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        try
        {
            using var db = NewDb(conn, Guid.NewGuid());
            foreach (var t in FinanceAggregates)
            {
                var entityType = db.Model.FindEntityType(t);
                entityType.Should().NotBeNull($"{t.Name} must be mapped");
                var prop = entityType!.FindProperty(nameof(IHasConcurrencyToken.ConcurrencyToken));
                prop.Should().NotBeNull($"{t.Name}.ConcurrencyToken must be mapped");
                prop!.IsConcurrencyToken.Should().BeTrue(
                    $"{t.Name}.ConcurrencyToken must be an optimistic-concurrency token");
            }
        }
        finally
        {
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Two_racing_updates_on_same_ledger_entry_lose_no_update_one_gets_conflict()
    {
        var tenantId = Guid.NewGuid();
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        try
        {
            await using var seedDb = NewDb(conn, tenantId);
            seedDb.Database.EnsureCreated();
            seedDb.Tenants.Add(new Tenant("Test", "test") { Id = tenantId });
            var customer = new Customer("Acme") { TenantId = tenantId };
            seedDb.Customers.Add(customer);
            var entry = new CustomerLedgerEntry(
                customer.Id, DateTime.UtcNow, DateTime.UtcNow.Date, LedgerEntryType.Debit, 500m, "TRY", 1m,
                LedgerSourceType.Invoice, null, null, null)
            { TenantId = tenantId };
            seedDb.CustomerLedgerEntries.Add(entry);
            await seedDb.SaveChangesAsync();

            await using var dbA = NewDb(conn, tenantId);
            await using var dbB = NewDb(conn, tenantId);
            var eA = await dbA.CustomerLedgerEntries.SingleAsync(e => e.Id == entry.Id);
            var eB = await dbB.CustomerLedgerEntries.SingleAsync(e => e.Id == entry.Id);

            eA.SetRunningBalance(500m);
            await dbA.SaveChangesAsync(); // wins

            eB.SetRunningBalance(250m);
            Func<Task> losing = () => dbB.SaveChangesAsync();
            await losing.Should().ThrowAsync<DbUpdateConcurrencyException>();

            await using var verifyDb = NewDb(conn, tenantId);
            var reloaded = await verifyDb.CustomerLedgerEntries.SingleAsync(e => e.Id == entry.Id);
            reloaded.RunningBalanceAfter.Should().Be(500m);
        }
        finally
        {
            conn.Dispose();
        }
    }
}
