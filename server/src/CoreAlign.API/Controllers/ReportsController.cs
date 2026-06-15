using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Reports.Accounting;
using CoreAlign.Application.Reports.Common;
using CoreAlign.Application.Reports.Inventory;
using CoreAlign.Application.Reports.Purchase;
using CoreAlign.Application.Reports.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IReportFileFactory _files;

    public ReportsController(IMediator mediator, IReportFileFactory files)
    {
        _mediator = mediator;
        _files = files;
    }

    [HttpGet("sales-by-period")]
    public async Task<IActionResult> GetSalesByPeriodAsync(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] SalesBucket bucket = SalesBucket.Month,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetSalesByPeriodQuery(fromUtc, toUtc, bucket), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("top-customers")]
    public async Task<IActionResult> GetTopCustomersAsync(
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTopCustomersQuery(limit, fromUtc, toUtc), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProductsAsync(
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTopProductsQuery(limit, fromUtc, toUtc), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("aging-summary")]
    public async Task<IActionResult> GetAgingSummaryAsync(
        [FromQuery] DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAgingSummaryQuery(asOfUtc), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{reportKey}")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole + "," + PersonaPolicies.PlatformAdminRole)]
    public async Task<IActionResult> DownloadAsync(
        string reportKey,
        [FromQuery] string format = "pdf",
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] DateTime? asOfUtc = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? accountId = null,
        [FromQuery] bool onlyBelowReorder = false,
        [FromQuery] StockMovementType? type = null,
        CancellationToken cancellationToken = default)
    {
        var fmt = string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ? ReportFormat.Xlsx : ReportFormat.Pdf;
        var document = await BuildDocumentAsync(reportKey, fromUtc, toUtc, asOfUtc, warehouseId, productId, accountId, onlyBelowReorder, type, cancellationToken);
        if (document is null)
        {
            return NotFound(new { error = $"Unknown reportKey '{reportKey}'." });
        }
        var file = await _files.RenderAsync(document, fmt, reportKey, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private async Task<ReportDocument?> BuildDocumentAsync(
        string reportKey,
        DateTime? fromUtc,
        DateTime? toUtc,
        DateTime? asOfUtc,
        Guid? warehouseId,
        Guid? productId,
        Guid? accountId,
        bool onlyBelowReorder,
        StockMovementType? type,
        CancellationToken cancellationToken)
    {
        var from = fromUtc ?? DateTime.UtcNow.AddDays(-30);
        var to = toUtc ?? DateTime.UtcNow;
        return reportKey.ToLowerInvariant() switch
        {
            "inventory-stock-on-hand" => await _mediator.Send(new StockOnHandReportQuery(warehouseId, productId, onlyBelowReorder), cancellationToken),
            "inventory-stock-movements" => await _mediator.Send(new StockMovementsReportQuery(from, to, warehouseId, productId, type), cancellationToken),
            "inventory-reorder-alerts" => await _mediator.Send(new ReorderLevelReportQuery(warehouseId), cancellationToken),
            "accounting-cash-flow" => await _mediator.Send(new CashFlowReportQuery(from, to), cancellationToken),
            "accounting-gl-detail" => await _mediator.Send(new GlDetailReportQuery(accountId, fromUtc, toUtc), cancellationToken),
            "accounting-ap-aging" => await _mediator.Send(new ApAgingReportQuery(asOfUtc), cancellationToken),
            "purchase-by-vendor" => await _mediator.Send(new PurchaseByVendorReportQuery(from, to), cancellationToken),
            "purchase-by-product" => await _mediator.Send(new PurchaseByProductReportQuery(from, to), cancellationToken),
            "purchase-open-pos" => await _mediator.Send(new OpenPurchaseOrdersReportQuery(), cancellationToken),
            _ => null,
        };
    }
}
