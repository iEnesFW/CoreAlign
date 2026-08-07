using CoreAlign.Application.Billing.Expiry;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Billing;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

/// <summary>
/// RED-BEFORE: the reminder job's own unit tests substitute <see cref="IModuleExpiryDataSource"/>,
/// so the real query was never compiled. It ordered by a member of the projected record, which EF
/// cannot translate — the job failed on every run and Hangfire retried it into the ground while the
/// unit suite stayed green.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class ModuleExpiryDataSourceTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public ModuleExpiryDataSourceTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task The_expiring_query_compiles_and_returns_only_grants_inside_the_window()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        // A tenant holds at most one grant per module (unique index), so every case needs its own module.
        var due = await AddGrantAsync(db, $"EXPDUE{suffix}", now.AddDays(-30), now.AddDays(4));
        await AddGrantAsync(db, $"EXPFAR{suffix}", now.AddDays(-30), now.AddDays(90));
        await AddGrantAsync(db, $"EXPPRP{suffix}", now.AddDays(-30), null);
        await AddGrantAsync(db, $"EXPOLD{suffix}", now.AddDays(-30), now.AddDays(-1));
        await AddGrantAsync(db, $"EXPFUT{suffix}", now.AddDays(5), now.AddDays(6));

        var source = new ModuleExpiryDataSource(db);

        var expiring = await source.GetExpiringAsync(now, ModuleExpiryThresholds.WindowDays, 500);

        var mine = expiring.Where(e => e.ModuleCode.EndsWith(suffix, StringComparison.Ordinal)).ToList();
        mine.Should().ContainSingle();
        mine[0].ModuleCode.Should().Be(due.Code);
        mine[0].ModuleName.Should().Be(due.Name);
        mine[0].TenantId.Should().Be(_factory.TenantA.TenantId);
    }

    [Fact]
    public async Task The_recipient_query_compiles_and_finds_the_company_administrator()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        var source = new ModuleExpiryDataSource(db);

        var act = async () => await source.GetTenantAdminUserIdsAsync(_factory.TenantA.TenantId);

        await act.Should().NotThrowAsync();
    }

    private async Task<Module> AddGrantAsync(
        CoreAlignDbContext db,
        string code,
        DateTime startUtc,
        DateTime? endUtc)
    {
        var module = new Module(code, $"Module {code}", "fixture", "Test", "box", 900, isActive: true, isCore: false);
        db.Set<Module>().Add(module);
        db.Set<TenantModule>().Add(
            new TenantModule(module.Id, startUtc, endUtc, TenantModuleSource.Paid, null)
            {
                TenantId = _factory.TenantA.TenantId,
            });
        await db.SaveChangesAsync();
        return module;
    }
}
