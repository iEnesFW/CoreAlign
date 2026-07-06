using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.Jobs;

/// <summary>
/// Real-DbContext proof that the log IP/UA anonymization backfill persists via a
/// set-based bulk UPDATE (ExecuteUpdate) rather than a load-track-modify-SaveChanges.
/// The <see cref="LogIpAnonymizationJob"/> unit test mocks IMaintenanceDataAccess, so it
/// cannot exercise the SaveChanges path that threw DbUpdateConcurrencyException in
/// production (login_audit_logs is a partitioned table whose per-row rows-affected
/// assertion is fragile). Sqlite has no partitioning so it cannot reproduce the exact
/// crash, but these lock the behaviour the fix must preserve: correct rows hashed, the
/// plaintext cleared, unrelated rows untouched, grouping by the hashed value, and an
/// idempotent rerun.
/// </summary>
public class MaintenanceDataAccessTests
{
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

    [Fact]
    public async Task AnonymizeLoginAuditLogs_hashes_old_rows_clears_ip_and_is_idempotent()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var cutoff = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc);

            db.LoginAuditLogs.Add(new LoginAuditLog("a@x.com", LoginResultType.Success, null, "10.0.0.1")
            { AttemptedAtUtc = cutoff.AddDays(-10) });
            db.LoginAuditLogs.Add(new LoginAuditLog("b@x.com", LoginResultType.Failed, null, "10.0.0.1")
            { AttemptedAtUtc = cutoff.AddDays(-9) });
            db.LoginAuditLogs.Add(new LoginAuditLog("c@x.com", LoginResultType.Success, null, "10.0.0.2")
            { AttemptedAtUtc = cutoff.AddDays(-30), IpAddressHash = "already" });
            db.LoginAuditLogs.Add(new LoginAuditLog("d@x.com", LoginResultType.Success, null, "10.0.0.3")
            { AttemptedAtUtc = cutoff.AddDays(5) });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var sut = new MaintenanceDataAccess(db);
            var affected = await sut.AnonymizeLoginAuditLogsOlderThanAsync(cutoff, ip => "H:" + ip, CancellationToken.None);

            affected.Should().Be(2);

            var all = await db.LoginAuditLogs.AsNoTracking().ToListAsync();
            var a = all.Single(l => l.EmailAttempted == "a@x.com");
            a.IpAddress.Should().BeNull();
            a.IpAddressHash.Should().Be("H:10.0.0.1");
            all.Single(l => l.EmailAttempted == "b@x.com").IpAddressHash.Should().Be("H:10.0.0.1");

            var c = all.Single(l => l.EmailAttempted == "c@x.com");
            c.IpAddress.Should().Be("10.0.0.2");
            c.IpAddressHash.Should().Be("already");

            var d = all.Single(l => l.EmailAttempted == "d@x.com");
            d.IpAddress.Should().Be("10.0.0.3");
            d.IpAddressHash.Should().BeNull();

            db.ChangeTracker.Clear();
            var second = await sut.AnonymizeLoginAuditLogsOlderThanAsync(cutoff, ip => "H:" + ip, CancellationToken.None);
            second.Should().Be(0);
        }
        finally
        {
            db.Dispose();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task AnonymizeActivityLogs_hashes_ip_and_user_agent_and_is_idempotent()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var cutoff = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc);

            db.Set<ActivityLog>().Add(new ActivityLog { TenantId = tenantId, Method = "GET", Path = "/x", IpAddress = "10.0.0.1", UserAgent = "UA-1" });
            db.Set<ActivityLog>().Add(new ActivityLog { TenantId = tenantId, Method = "GET", Path = "/y", IpAddress = "10.0.0.1", UserAgent = "UA-2" });
            db.Set<ActivityLog>().Add(new ActivityLog { TenantId = tenantId, Method = "GET", Path = "/z", IpAddress = "10.0.0.9", UserAgent = "UA-9" });
            await db.SaveChangesAsync();

            // Set the "old" rows past the cutoff via a bulk update so entity timestamp
            // stamping cannot overwrite the seeded date.
            await db.Set<ActivityLog>().IgnoreQueryFilters()
                .Where(a => a.Path == "/x" || a.Path == "/y")
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.CreatedAtUtc, cutoff.AddDays(-10)));
            await db.Set<ActivityLog>().IgnoreQueryFilters()
                .Where(a => a.Path == "/z")
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.CreatedAtUtc, cutoff.AddDays(5)));
            db.ChangeTracker.Clear();

            var sut = new MaintenanceDataAccess(db);
            var affected = await sut.AnonymizeActivityLogsOlderThanAsync(cutoff, ip => "IP:" + ip, ua => "UA:" + ua, CancellationToken.None);

            affected.Should().Be(2);

            var rows = await db.Set<ActivityLog>().IgnoreQueryFilters().AsNoTracking().ToListAsync();
            var x = rows.Single(r => r.Path == "/x");
            x.IpAddress.Should().BeNull();
            x.IpAddressHash.Should().Be("IP:10.0.0.1");
            x.UserAgent.Should().BeNull();
            x.UserAgentHash.Should().Be("UA:UA-1");

            var z = rows.Single(r => r.Path == "/z");
            z.IpAddress.Should().Be("10.0.0.9");
            z.IpAddressHash.Should().BeNull();
            z.UserAgentHash.Should().BeNull();

            db.ChangeTracker.Clear();
            var second = await sut.AnonymizeActivityLogsOlderThanAsync(cutoff, ip => "IP:" + ip, ua => "UA:" + ua, CancellationToken.None);
            second.Should().Be(0);
        }
        finally
        {
            db.Dispose();
            conn.Dispose();
        }
    }
}
