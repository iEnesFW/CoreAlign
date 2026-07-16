using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace CoreAlign.API.HostedServices;

// WHY: glass scrap/fire reason codes are system reference data — seeded per-tenant idempotently on
// every startup (NOT behind DEMO_DATA), so ScrapGlassPlate / below-min auto-scrap always resolve a
// costed write-off reason.
public sealed class GlassPlateSystemDataSeeder : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GlassPlateSystemDataSeeder> _logger;

    public GlassPlateSystemDataSeeder(IServiceScopeFactory scopeFactory, ILogger<GlassPlateSystemDataSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private sealed record ReasonSeed(string Code, string Name, StockReasonCategory Category);

    private static readonly ReasonSeed[] Reasons =
    {
        new("SCR-CUT", "Cam fire — kesim kırığı", StockReasonCategory.Scrap),
        new("SCR-BADCUT", "Cam fire — kötü/yanlış kesim", StockReasonCategory.Scrap),
        new("SCR-EDGE", "Cam fire — kenar fire", StockReasonCategory.Scrap),
        new("SCR-GRIND", "Cam fire — taşlama/rodaj kırığı", StockReasonCategory.Scrap),
        new("SCR-TEMPER", "Cam fire — temper fırın kırığı", StockReasonCategory.Scrap),
        new("SCR-NIS", "Cam fire — NiS kendiliğinden patlama", StockReasonCategory.Scrap),
        new("SCR-LAM", "Cam fire — lamine defekti", StockReasonCategory.Scrap),
        new("SCR-STOCK", "Cam fire — stok hasarı", StockReasonCategory.Scrap),
        new("SCR-SAMPLE", "Cam fire — yıkıcı numune/QC testi", StockReasonCategory.Scrap),
        new("SCR-BELOWMIN", "Cam fire — eşik-altı artan", StockReasonCategory.Scrap),
        new("SCR-HANDLE", "Cam fire — elleçleme/taşıma kırığı", StockReasonCategory.Loss),
        new("SCR-SHELF", "Cam fire — raf ömrü (kaplama/interlayer)", StockReasonCategory.Expired),
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

            var codes = Reasons.Select(r => r.Code).ToArray();
            var tenantIds = await db.Set<Tenant>().Select(t => t.Id).ToListAsync(stoppingToken);

            var added = 0;
            foreach (var tenantId in tenantIds)
            {
                var existing = await db.Set<StockReasonCode>()
                    .IgnoreQueryFilters()
                    .Where(r => r.TenantId == tenantId && codes.Contains(r.Code))
                    .Select(r => r.Code)
                    .ToListAsync(stoppingToken);

                foreach (var seed in Reasons.Where(s => !existing.Contains(s.Code)))
                {
                    var reason = new StockReasonCode(seed.Code, seed.Name, seed.Category)
                    {
                        TenantId = tenantId,
                    };
                    await db.Set<StockReasonCode>().AddAsync(reason, stoppingToken);
                    added++;
                }
            }

            if (added > 0)
            {
                await db.SaveChangesAsync(stoppingToken);
            }
            _logger.LogInformation("Glass plate scrap reason codes seeded ({Added} new across {Tenants} tenants).", added, tenantIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Glass plate scrap reason seeding failed; below-min scrap will not post GL until reasons exist.");
        }
    }
}
