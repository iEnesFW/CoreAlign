using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using CoreAlign.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAlign.Application.Tests.Payroll;

public sealed class PayrollParametersResolutionTests
{
    private readonly string _dbName = $"payroll-{Guid.NewGuid():N}";

    private CoreAlignDbContext ContextFor(Guid tenantId)
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(tenantId);
        return Build(tenant);
    }

    private CoreAlignDbContext ContextWithoutTenant()
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns((Guid?)null);
        tenant.HasTenant.Returns(false);
        return Build(tenant);
    }

    private CoreAlignDbContext Build(ITenantContext tenant)
    {
        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase(_dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new CoreAlignDbContext(options, tenant, Substitute.For<MediatR.IPublisher>());
        db.Database.EnsureCreated();
        return db;
    }

    private static PayrollParameters BuildParameters(Guid tenantId, int year, DateOnly effectiveFrom, params (decimal Rate, int Order, decimal? Upper)[] brackets)
    {
        var parameters = new PayrollParameters(
            effectiveYear: year,
            effectiveFrom: effectiveFrom,
            sgkEmployeeRate: 0.14m,
            sgkEmployerRate: 0.205m,
            sgkEmployer5PointIncentiveRate: 0.155m,
            unemploymentEmployeeRate: 0.01m,
            unemploymentEmployerRate: 0.02m,
            sgkFloorMonthly: 26005.50m,
            sgkCeilingMultiplier: 7.5m,
            sgkCeilingMonthly: 195041.25m,
            stampTaxRate: 0.00759m,
            grossMinimumWage: 26005.50m,
            disability1Amount: 0m,
            disability2Amount: 0m,
            disability3Amount: 0m)
        {
            TenantId = tenantId,
        };
        foreach (var (rate, order, upper) in brackets)
        {
            parameters.AddTaxBracket(new PayrollTaxBracket(rate, order, upper));
        }
        return parameters;
    }

    private static (decimal Rate, int Order, decimal? Upper)[] SeededBrackets() => new[]
    {
        (15m, 1, (decimal?)158000m),
        (20m, 2, (decimal?)330000m),
        (27m, 3, (decimal?)1200000m),
        (35m, 4, (decimal?)4300000m),
        (40m, 5, (decimal?)null),
    };

    [Fact]
    public async Task Resolve_prefers_tenant_row_over_global()
    {
        var tenantId = Guid.NewGuid();
        var period = new DateOnly(2026, 3, 1);

        await using (var globalSeed = ContextWithoutTenant())
        {
            globalSeed.PayrollParameters.Add(BuildParameters(Guid.Empty, 2026, new DateOnly(2026, 1, 1), SeededBrackets()));
            await globalSeed.SaveChangesAsync();
        }
        await using (var tenantSeed = ContextFor(tenantId))
        {
            tenantSeed.PayrollParameters.Add(BuildParameters(tenantId, 2026, new DateOnly(2026, 1, 1), SeededBrackets()));
            await tenantSeed.SaveChangesAsync();
        }

        await using var db = ContextFor(tenantId);
        var repo = new PayrollParametersRepository(db);

        var resolved = await repo.ResolveAsync(2026, period);

        resolved.Should().NotBeNull();
        resolved!.TenantId.Should().Be(tenantId);
        resolved.TaxBrackets.Should().HaveCount(5);
    }

    [Fact]
    public async Task Resolve_falls_back_to_global_when_no_tenant_row()
    {
        var tenantId = Guid.NewGuid();
        var period = new DateOnly(2026, 6, 1);

        await using (var globalSeed = ContextWithoutTenant())
        {
            globalSeed.PayrollParameters.Add(BuildParameters(Guid.Empty, 2026, new DateOnly(2026, 1, 1), SeededBrackets()));
            await globalSeed.SaveChangesAsync();
        }

        await using var db = ContextFor(tenantId);
        var repo = new PayrollParametersRepository(db);

        var resolved = await repo.ResolveAsync(2026, period);

        resolved.Should().NotBeNull();
        resolved!.TenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Resolve_isolates_tenants_but_both_read_global()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var period = new DateOnly(2026, 4, 1);

        await using (var globalSeed = ContextWithoutTenant())
        {
            globalSeed.PayrollParameters.Add(BuildParameters(Guid.Empty, 2026, new DateOnly(2026, 1, 1), SeededBrackets()));
            await globalSeed.SaveChangesAsync();
        }
        await using (var seedA = ContextFor(tenantA))
        {
            seedA.PayrollParameters.Add(BuildParameters(tenantA, 2026, new DateOnly(2026, 1, 1), SeededBrackets()));
            await seedA.SaveChangesAsync();
        }

        await using (var dbA = ContextFor(tenantA))
        {
            var resolvedA = await new PayrollParametersRepository(dbA).ResolveAsync(2026, period);
            resolvedA.Should().NotBeNull();
            resolvedA!.TenantId.Should().Be(tenantA);
        }

        await using var dbB = ContextFor(tenantB);
        var resolvedB = await new PayrollParametersRepository(dbB).ResolveAsync(2026, period);
        resolvedB.Should().NotBeNull();
        resolvedB!.TenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Resolve_picks_newest_effective_from()
    {
        var tenantId = Guid.NewGuid();
        var period = new DateOnly(2026, 8, 1);

        await using (var seed = ContextFor(tenantId))
        {
            seed.PayrollParameters.Add(BuildParameters(tenantId, 2026, new DateOnly(2026, 1, 1), SeededBrackets()));
            var newer = BuildParameters(tenantId, 2026, new DateOnly(2026, 7, 1), SeededBrackets());
            seed.PayrollParameters.Add(newer);
            await seed.SaveChangesAsync();
        }

        await using var db = ContextFor(tenantId);
        var repo = new PayrollParametersRepository(db);

        var resolved = await repo.ResolveAsync(2026, period);

        resolved.Should().NotBeNull();
        resolved!.EffectiveFrom.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task Resolve_skips_rows_not_yet_effective_for_period()
    {
        var tenantId = Guid.NewGuid();
        var period = new DateOnly(2026, 2, 1);

        await using (var globalSeed = ContextWithoutTenant())
        {
            globalSeed.PayrollParameters.Add(BuildParameters(Guid.Empty, 2026, new DateOnly(2026, 1, 1), SeededBrackets()));
            await globalSeed.SaveChangesAsync();
        }
        await using (var tenantSeed = ContextFor(tenantId))
        {
            tenantSeed.PayrollParameters.Add(BuildParameters(tenantId, 2026, new DateOnly(2026, 7, 1), SeededBrackets()));
            await tenantSeed.SaveChangesAsync();
        }

        await using var db = ContextFor(tenantId);
        var repo = new PayrollParametersRepository(db);

        var resolved = await repo.ResolveAsync(2026, period);

        resolved.Should().NotBeNull();
        resolved!.TenantId.Should().Be(Guid.Empty);
    }
}
