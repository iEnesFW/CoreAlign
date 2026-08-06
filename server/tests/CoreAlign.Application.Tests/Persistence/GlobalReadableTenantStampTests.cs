using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.Persistence;

/// <summary>
/// Global reference rows (<see cref="IGlobalReadable"/>) use tenant_id = Guid.Empty to mean
/// "shared by every tenant", not "tenant not resolved yet". The SaveChanges auto-stamp must leave
/// them alone: stamping one inside an authenticated request turned the day's global TCMB rates into
/// a single tenant's private overrides, after which the next ingest could not find them and
/// collided on the unique key. Ordinary tenant rows must still be stamped.
/// </summary>
public class GlobalReadableTenantStampTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private static readonly Guid TenantId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private sealed class FixedTenantContext : ITenantContext
    {
        public Guid? CurrentTenantId => TenantId;
        public bool HasTenant => true;
        public Guid RequireTenantId() => TenantId;
        public void EnsureSameTenant(Guid tenantId) { }
        public IDisposable PushScope(Guid tenantId) => new NoopScope();

        private sealed class NoopScope : IDisposable
        {
            public void Dispose() { }
        }
    }

    private CoreAlignDbContext CreateContext()
    {
        var tenantContext = new FixedTenantContext();
        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new CoreAlignDbContext(options, tenantContext, Substitute.For<IPublisher>());
    }

    [Fact]
    public async Task A_global_reference_row_keeps_its_empty_tenant_inside_a_tenant_scope()
    {
        await using var context = CreateContext();
        var rate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Currency = "JPY",
            RateAgainstTry = 0.21m,
            ValidOnDate = new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc),
            Source = "TCMB",
            FetchedAtUtc = DateTime.UtcNow,
        };

        await context.ExchangeRates.AddAsync(rate);
        await context.SaveChangesAsync();

        rate.TenantId.Should().Be(Guid.Empty, "an IGlobalReadable row is shared, not owned");
    }

    [Fact]
    public async Task An_explicit_tenant_override_is_still_honoured()
    {
        await using var context = CreateContext();
        var overrideRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Currency = "JPY",
            RateAgainstTry = 0.22m,
            ValidOnDate = new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Utc),
            Source = "MANUAL",
            FetchedAtUtc = DateTime.UtcNow,
        };

        await context.ExchangeRates.AddAsync(overrideRate);
        await context.SaveChangesAsync();

        overrideRate.TenantId.Should().Be(TenantId);
    }
}
