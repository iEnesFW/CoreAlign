using MediatR;

namespace CoreAlign.Application.Mrp;

public class SeedMrpDemoHandler : IRequestHandler<SeedMrpDemoCommand, MrpDemoSeedResultDto>
{
    private readonly IMrpDemoSeeder _seeder;

    public SeedMrpDemoHandler(IMrpDemoSeeder seeder) => _seeder = seeder;

    public async Task<MrpDemoSeedResultDto> Handle(SeedMrpDemoCommand command, CancellationToken ct)
    {
        var result = await _seeder.SeedAsync(ct);
        return new MrpDemoSeedResultDto(
            result.TenantId,
            result.ScenarioTag,
            result.Warehouses,
            result.Products,
            result.BuyProducts,
            result.MakeProducts,
            result.BomComponents,
            result.StockItems,
            result.PurchaseOrders,
            result.PurchaseOrderLines,
            result.SalesOrders,
            result.SalesOrderLines);
    }
}
