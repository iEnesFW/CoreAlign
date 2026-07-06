using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Mrp;
using CoreAlign.Application.Mrp.Capacity;
using CoreAlign.Application.Mrp.Distribution;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

public record ReleasePlannedOrdersRequest(IReadOnlyList<Guid> PlannedOrderIds, Guid OperationId);

public record FirmPlannedOrderRequest(Guid OperationId, decimal? OverrideQuantity = null, DateTime? OverrideDueDateUtc = null);

public record FirmProductionOrderRequest(Guid OperationId, decimal? OverrideQuantity = null, DateTime? OverrideDueDateUtc = null);

public record ReleaseProductionOrderRequest(Guid OperationId);

public record CompleteProductionOrderRequest(Guid OperationId, Guid? WarehouseId = null);

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mrp")]
public class MrpController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHostEnvironment _environment;

    public MrpController(IMediator mediator, IHostEnvironment environment)
    {
        _mediator = mediator;
        _environment = environment;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] int topN = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetMrpDashboardQuery(topN), ct)).ToOk();

    [HttpGet("stock-projection/{productId:guid}")]
    public async Task<IActionResult> StockProjection(
        Guid productId,
        [FromQuery] int daysAhead = 30,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetStockProjectionQuery(productId, daysAhead), ct)).ToOk();

    [HttpGet("demand-forecast/{productId:guid}")]
    public async Task<IActionResult> DemandForecast(
        Guid productId,
        [FromQuery] int windowDays = 90,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetDemandForecastQuery(productId, windowDays), ct)).ToOk();

    [HttpPost("products/classify-abc")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> ClassifyProductsAbc(
        [FromBody] ClassifyProductsAbcCommand? cmd,
        CancellationToken ct = default)
        => (await _mediator.Send(cmd ?? new ClassifyProductsAbcCommand(), ct)).ToOk();

    [HttpPost("generate-suggestions")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> GenerateSuggestions(
        [FromBody] GenerateMrpSuggestionsCommand? cmd,
        CancellationToken ct = default)
        => (await _mediator.Send(cmd ?? new GenerateMrpSuggestionsCommand(), ct)).ToAccepted();

    [HttpGet("plan/preview")]
    public async Task<IActionResult> PlanPreview(
        [FromQuery] DateTime? asOf = null,
        [FromQuery] MrpBucketKind bucket = MrpBucketKind.Day,
        [FromQuery] int horizon = 60,
        CancellationToken ct = default)
        => (await _mediator.Send(new RunMrpPreviewQuery(asOf, bucket, horizon), ct)).ToOk();

    [HttpGet("plan/item/{productId:guid}")]
    public async Task<IActionResult> ItemPlan(
        Guid productId,
        [FromQuery] DateTime? asOf = null,
        [FromQuery] MrpBucketKind bucket = MrpBucketKind.Day,
        [FromQuery] int horizon = 60,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetMrpItemPlanQuery(productId, asOf, bucket, horizon), ct)).ToOk();

    [HttpPost("plan/commit")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> CommitPlan(
        [FromBody] CommitMrpPlanCommand cmd,
        CancellationToken ct = default)
        => (await _mediator.Send(cmd, ct)).ToOk();

    [HttpGet("plan/runs")]
    public async Task<IActionResult> PlanRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListMrpPlanRunsQuery(page, pageSize), ct)).ToOk();

    [HttpGet("action-messages")]
    public async Task<IActionResult> ActionMessages(
        [FromQuery] Guid? planRunId = null,
        [FromQuery] MrpActionType? type = null,
        [FromQuery] MrpActionSeverity? severity = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] bool includeDismissed = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new ListMrpActionMessagesQuery(planRunId, type, severity, supplierId, includeDismissed, page, pageSize),
            ct)).ToOk();

    [HttpPost("action-messages/{id:guid}/dismiss")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> DismissActionMessage(
        Guid id,
        CancellationToken ct = default)
        => (await _mediator.Send(new DismissMrpActionMessageCommand(id), ct)).ToOk();

    [HttpGet("pegging/{planRunId:guid}/{componentProductId:guid}")]
    public async Task<IActionResult> Pegging(
        Guid planRunId,
        Guid componentProductId,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetMrpPeggingQuery(planRunId, componentProductId), ct)).ToOk();

    [HttpPost("plan/{planRunId:guid}/release")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> ReleasePlannedOrders(
        Guid planRunId,
        [FromBody] ReleasePlannedOrdersRequest body,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new ReleasePlannedOrdersCommand(planRunId, body.PlannedOrderIds, body.OperationId),
            ct)).ToOk();

    [HttpPost("planned-orders/{id:guid}/firm")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> FirmPlannedOrder(
        Guid id,
        [FromBody] FirmPlannedOrderRequest body,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new FirmMrpPlannedOrderCommand(id, body.OperationId, body.OverrideQuantity, body.OverrideDueDateUtc),
            ct)).ToOk();

    [HttpGet("production-orders")]
    public async Task<IActionResult> ProductionOrders(
        [FromQuery] Guid? planRunId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] PlannedProductionOrderStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new ListPlannedProductionOrdersQuery(planRunId, productId, status, page, pageSize),
            ct)).ToOk();

    [HttpGet("pegging-chain/{planRunId:guid}/{componentProductId:guid}")]
    public async Task<IActionResult> PeggingChain(
        Guid planRunId,
        Guid componentProductId,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetProductionPeggingChainQuery(planRunId, componentProductId), ct)).ToOk();

    [HttpGet("change-impact/{planRunId:guid}/{sourceOrderLineId:guid}")]
    public async Task<IActionResult> ChangeImpact(
        Guid planRunId,
        Guid sourceOrderLineId,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetChangeImpactQuery(planRunId, sourceOrderLineId), ct)).ToOk();

    [HttpGet("distribution/transfer-suggestions")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> TransferSuggestions(CancellationToken ct = default)
        => (await _mediator.Send(new GetMrpTransferSuggestionsQuery(), ct)).ToOk();

    [HttpGet("capacity/load")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> CapacityLoad(
        [FromQuery] DateTime? asOf = null,
        [FromQuery] MrpBucketKind bucket = MrpBucketKind.Day,
        [FromQuery] int horizon = 60,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetMrpCapacityLoadQuery(asOf, bucket, horizon), ct)).ToOk();

    [HttpPost("production-orders/{id:guid}/firm")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> FirmProductionOrder(
        Guid id,
        [FromBody] FirmProductionOrderRequest body,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new FirmPlannedProductionOrderCommand(id, body.OperationId, body.OverrideQuantity, body.OverrideDueDateUtc),
            ct)).ToOk();

    [HttpPost("production-orders/{id:guid}/release")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> ReleaseProductionOrder(
        Guid id,
        [FromBody] ReleaseProductionOrderRequest body,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new ReleasePlannedProductionOrderCommand(id, body.OperationId),
            ct)).ToOk();

    [HttpPost("production-orders/{id:guid}/complete")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> CompleteProductionOrder(
        Guid id,
        [FromBody] CompleteProductionOrderRequest body,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new CompletePlannedProductionOrderCommand(id, body.OperationId, body.WarehouseId),
            ct)).ToOk();

    /// <summary>
    /// DEV-ONLY: seeds a small but rich MRP scenario (warehouse, buy items below
    /// safety stock, a Make assembly with a 2-component BOM, on-hand stock, an open
    /// purchase order, and an allocated-but-unshipped sales order) for the current
    /// authenticated tenant so the planning workbench comes alive. Returns 404 in
    /// any non-Development environment. Safe to call repeatedly (run-unique SKUs).
    /// </summary>
    [HttpPost("dev/seed-demo")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole)]
    public async Task<IActionResult> SeedDemo(CancellationToken ct = default)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }
        return (await _mediator.Send(new SeedMrpDemoCommand(), ct)).ToOk();
    }
}
