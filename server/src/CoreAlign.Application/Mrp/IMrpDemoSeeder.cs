namespace CoreAlign.Application.Mrp;

/// <summary>
/// Counts produced by a single MRP demo-seed run, returned to the caller so a
/// developer can confirm the workbench has independent demand, scheduled
/// receipts, a Make assembly with a BOM, and below-safety-stock buy items.
/// </summary>
public sealed record MrpDemoSeedResult(
    Guid TenantId,
    string ScenarioTag,
    int Warehouses,
    int Products,
    int BuyProducts,
    int MakeProducts,
    int BomComponents,
    int StockItems,
    int PurchaseOrders,
    int PurchaseOrderLines,
    int SalesOrders,
    int SalesOrderLines);

/// <summary>
/// Dev-only seeder that populates a small but rich MRP scenario for the current
/// authenticated tenant. Every call uses fresh, run-unique SKUs/codes so it is
/// safe to invoke repeatedly without tripping unique constraints.
/// </summary>
public interface IMrpDemoSeeder
{
    Task<MrpDemoSeedResult> SeedAsync(CancellationToken cancellationToken = default);
}
