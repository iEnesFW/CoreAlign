using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Inventory;

public sealed record ReorderLevelReportQuery(Guid? WarehouseId) : IRequest<ReportDocument>;

public sealed class ReorderLevelReportQueryHandler : IRequestHandler<ReorderLevelReportQuery, ReportDocument>
{
    private const int MaxRows = 5_000;
    private readonly IStockItemRepository _stockItems;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public ReorderLevelReportQueryHandler(
        IStockItemRepository stockItems,
        ITenantRepository tenants,
        ITenantContext tenantContext)
    {
        _stockItems = stockItems;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(ReorderLevelReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var rows = await _stockItems.SearchAsync(
            productId: null,
            warehouseId: request.WarehouseId,
            onlyBelowReorder: true,
            page: 1,
            pageSize: MaxRows,
            cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("sku", "SKU", ReportColumnType.Text),
            new("product", "Product", ReportColumnType.Text),
            new("warehouse", "Warehouse", ReportColumnType.Text),
            new("onHand", "On hand", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("reserved", "Reserved", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("available", "Available", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("minStock", "Min stock", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("reorderPoint", "Reorder point", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("shortage", "Shortage", ReportColumnType.Decimal, ReportColumnAlign.Right),
        };

        var dataRows = rows.Select(r =>
        {
            var available = r.OnHand - r.Reserved;
            var reorder = r.ProductReorderPoint ?? 0m;
            var shortage = Math.Max(0m, reorder - available);
            return ReportRow.Of(
                r.ProductSku,
                r.ProductName,
                r.WarehouseName,
                r.OnHand,
                r.Reserved,
                available,
                r.ProductMinStock ?? 0m,
                reorder,
                shortage);
        }).ToList();

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "Reorder level alerts",
            Subtitle: "Products at or below reorder point",
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodToUtc: DateTime.UtcNow,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, dataRows);
    }
}
