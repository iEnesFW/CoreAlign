using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Inventory;

public sealed record StockMovementsReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? WarehouseId,
    Guid? ProductId,
    StockMovementType? Type) : IRequest<ReportDocument>;

public sealed class StockMovementsReportQueryHandler : IRequestHandler<StockMovementsReportQuery, ReportDocument>
{
    private const int MaxRows = 10_000;
    private readonly IStockMovementRepository _movements;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public StockMovementsReportQueryHandler(
        IStockMovementRepository movements,
        ITenantRepository tenants,
        ITenantContext tenantContext)
    {
        _movements = movements;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(StockMovementsReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var (items, _) = await _movements.SearchAsync(
            request.ProductId,
            request.WarehouseId,
            request.Type,
            request.FromUtc,
            request.ToUtc,
            page: 1,
            pageSize: MaxRows,
            cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("date", "Date", ReportColumnType.DateTime),
            new("ref", "Reference", ReportColumnType.Text),
            new("type", "Type", ReportColumnType.Text),
            new("sku", "SKU", ReportColumnType.Text),
            new("product", "Product", ReportColumnType.Text),
            new("warehouse", "Warehouse", ReportColumnType.Text),
            new("qty", "Quantity", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("unitCost", "Unit cost", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("totalCost", "Total cost", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("balanceAfter", "Balance after", ReportColumnType.Decimal, ReportColumnAlign.Right),
        };

        var ordered = items.OrderBy(m => m.OccurredAtUtc).ToList();
        var rows = ordered.Select(m => ReportRow.Of(
            (object?)m.OccurredAtUtc,
            m.SourceReference ?? string.Empty,
            m.Type.ToString(),
            m.Product?.Sku ?? string.Empty,
            m.Product?.Name ?? string.Empty,
            m.Warehouse?.Name ?? string.Empty,
            m.Quantity,
            m.UnitCost,
            m.TotalCost,
            m.OnHandAfter)).ToList();

        var totalQty = ordered.Sum(m => m.Quantity);
        var totalCost = ordered.Sum(m => m.TotalCost);
        var footer = new List<ReportCell>
        {
            ReportCell.From("Total"),
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.From(totalQty),
            ReportCell.Empty,
            ReportCell.From(totalCost),
            ReportCell.Empty,
        };

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "Stock movements",
            Subtitle: request.Type.HasValue ? $"Type: {request.Type}" : null,
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodFromUtc: request.FromUtc,
            PeriodToUtc: request.ToUtc,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, rows, FooterTotals: footer);
    }
}
