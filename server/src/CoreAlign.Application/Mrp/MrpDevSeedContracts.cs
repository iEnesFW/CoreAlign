using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Mrp;

public record MrpDemoSeedResultDto(
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
/// Dev-only command that seeds a rich MRP scenario for the current tenant. The
/// transport is gated to the Development environment at the controller; the
/// handler stays thin and delegates to <see cref="IMrpDemoSeeder"/>.
/// </summary>
public record SeedMrpDemoCommand : IRequest<MrpDemoSeedResultDto>, ITransactionalRequest;
