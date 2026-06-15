using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Inventory;

public sealed record StockOnHandReportQuery(Guid? WarehouseId, Guid? ProductId, bool OnlyBelowReorder)
    : IRequest<ReportDocument>;

public sealed class StockOnHandReportQueryHandler : IRequestHandler<StockOnHandReportQuery, ReportDocument>
{
    private const int MaxRows = 5_000;
    private readonly IStockItemRepository _stockItems;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public StockOnHandReportQueryHandler(
        IStockItemRepository stockItems,
        ITenantRepository tenants,
        ITenantContext tenantContext)
    {
        _stockItems = stockItems;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(StockOnHandReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var rows = await _stockItems.SearchAsync(
            request.ProductId,
            request.WarehouseId,
            request.OnlyBelowReorder,
            page: 1,
            pageSize: MaxRows,
            cancellationToken);

        var totalQty = rows.Sum(r => r.OnHand);
        var totalValue = rows.Sum(r => r.OnHand * r.AvgCost);

        var columns = new List<ReportColumn>
        {
            new("sku", "SKU", ReportColumnType.Text),
            new("product", "Product", ReportColumnType.Text),
            new("warehouse", "Warehouse", ReportColumnType.Text),
            new("onHand", "On hand", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("reserved", "Reserved", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("available", "Available", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("avgCost", "Avg cost", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("value", "Value", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("lastMovement", "Last movement", ReportColumnType.DateTime),
        };

        var dataRows = rows.Select(r => ReportRow.Of(
            r.ProductSku,
            r.ProductName,
            r.WarehouseName,
            r.OnHand,
            r.Reserved,
            r.OnHand - r.Reserved,
            r.AvgCost,
            r.OnHand * r.AvgCost,
            (object?)r.LastMovementAtUtc)).ToList();

        var footer = new List<ReportCell>
        {
            ReportCell.From("Total"),
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.From(totalQty),
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.From(totalValue),
            ReportCell.Empty,
        };

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "Stock on hand",
            Subtitle: request.WarehouseId.HasValue ? $"Warehouse filter applied" : null,
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodFromUtc: null,
            PeriodToUtc: DateTime.UtcNow,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, dataRows, Groups: null, FooterTotals: footer);
    }
}
