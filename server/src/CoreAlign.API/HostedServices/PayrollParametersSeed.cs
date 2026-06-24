using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.API.HostedServices;

public static class PayrollParametersSeed
{
    private const int Year = 2026;

    public static async Task SeedGlobalAsync(IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<CoreAlignDbContext>();

        var exists = await db.PayrollParameters
            .IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == Guid.Empty && p.EffectiveYear == Year, ct);
        if (exists)
        {
            return;
        }

        var uow = sp.GetRequiredService<IUnitOfWork>();
        var parameters = new PayrollParameters(
            effectiveYear: Year,
            effectiveFrom: new DateOnly(Year, 1, 1),
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
            disability3Amount: 0m,
            minWageExemptionEnabled: true,
            effectiveTo: null,
            description: "PLACEHOLDER 2026 - verify with SMMM before go-live")
        {
            TenantId = Guid.Empty,
        };

        parameters.AddTaxBracket(new PayrollTaxBracket(15m, 1, 158000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(20m, 2, 330000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(27m, 3, 1200000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(35m, 4, 4300000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(40m, 5, null));

        await db.PayrollParameters.AddAsync(parameters, ct);
        await uow.SaveChangesAsync(ct);
    }
}
