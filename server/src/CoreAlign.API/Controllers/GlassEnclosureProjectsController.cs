using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassEnclosure.Authorization;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/glass-enclosure/projects")]
public class GlassEnclosureProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    public GlassEnclosureProjectsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("presets")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectView)]
    public async Task<IActionResult> GetPresets([FromQuery] EnclosureCategory? category, CancellationToken ct) =>
        (await _mediator.Send(new GetEnclosurePresetsQuery(category), ct)).ToOk();

    [HttpGet("templates")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectView)]
    public async Task<IActionResult> ListTemplates([FromQuery] EnclosureCategory? category, CancellationToken ct) =>
        (await _mediator.Send(new ListProjectTemplatesQuery(category), ct)).ToOk();

    [HttpGet("templates/{templateId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectView)]
    public async Task<IActionResult> GetTemplate(Guid templateId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectTemplateByIdQuery(templateId), ct);
        return result is null ? NotFound() : result.ToOk();
    }

    [HttpPost("from-template/{templateId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectCreate)]
    public async Task<IActionResult> CreateFromTemplate(Guid templateId, [FromBody] CreateProjectFromTemplateBody body, CancellationToken ct)
    {
        var dto = new CreateProjectFromTemplateDto(templateId, body.CustomerId, body.ProjectName, body.Currency);
        var result = await _mediator.Send(new CreateProjectFromTemplateCommand(dto), ct);
        return result.ToCreated();
    }

    [HttpGet]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectView)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] GlassProjectStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? assignedDesignerUserId,
        [FromQuery] Guid? assignedSalespersonUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        (await _mediator.Send(new GetGlassProjectsQuery(
            search, status, customerId,
            assignedDesignerUserId, assignedSalespersonUserId,
            page, pageSize), ct)).ToOk();

    [HttpGet("{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectView)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGlassProjectByIdQuery(id), ct);
        return result is null ? NotFound() : result.ToOk();
    }

    [HttpPost]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectCreate)]
    public async Task<IActionResult> Create([FromBody] CreateGlassProjectDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateGlassProjectCommand(data), ct)).ToCreated();

    [HttpPut("{id:guid}/header")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> UpdateHeader(Guid id, [FromBody] UpdateGlassProjectHeaderDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateGlassProjectHeaderCommand(id, data), ct)).ToOk();

    [HttpPut("{id:guid}/team")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> AssignTeam(Guid id, [FromBody] AssignProjectTeamDto data, CancellationToken ct) =>
        (await _mediator.Send(new AssignGlassProjectTeamCommand(id, data), ct)).ToOk();

    [HttpPut("{id:guid}/enclosure")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> ConfigureEnclosure(Guid id, [FromBody] ConfigureEnclosureDto data, CancellationToken ct) =>
        (await _mediator.Send(new ConfigureEnclosureCommand(id, data), ct)).ToOk();

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> TransitionStatus(Guid id, [FromBody] TransitionProjectStatusDto data, CancellationToken ct) =>
        (await _mediator.Send(new TransitionGlassProjectStatusCommand(id, data), ct)).ToOk();

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteGlassProjectCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/runs")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> AddRun(Guid id, [FromBody] AddRunDto data, CancellationToken ct) =>
        (await _mediator.Send(new AddRunCommand(id, data), ct)).ToCreated();

    [HttpPut("{id:guid}/runs/{runId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> UpdateRun(Guid id, Guid runId, [FromBody] UpdateRunDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateRunCommand(id, runId, data), ct)).ToOk();

    [HttpDelete("{id:guid}/runs/{runId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> RemoveRun(Guid id, Guid runId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveRunCommand(id, runId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/runs/{runId:guid}/rebalance-panels")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> RebalancePanels(Guid id, Guid runId, [FromBody] BulkRebalancePanelsDto data, CancellationToken ct) =>
        (await _mediator.Send(new BulkRebalancePanelsCommand(id, runId, data), ct)).ToOk();

    [HttpPost("{id:guid}/runs/{runId:guid}/set-panels")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> SetRunPanels(Guid id, Guid runId, [FromBody] SetRunPanelsDto data, CancellationToken ct) =>
        (await _mediator.Send(new SetRunPanelsCommand(id, runId, data), ct)).ToOk();

    [HttpPost("{id:guid}/runs/{runId:guid}/panels")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> AddPanel(Guid id, Guid runId, [FromBody] AddPanelDto data, CancellationToken ct) =>
        (await _mediator.Send(new AddPanelCommand(id, runId, data), ct)).ToCreated();

    [HttpPut("{id:guid}/runs/{runId:guid}/panels/{panelId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> UpdatePanel(Guid id, Guid runId, Guid panelId, [FromBody] UpdatePanelDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdatePanelCommand(id, runId, panelId, data), ct)).ToOk();

    [HttpDelete("{id:guid}/runs/{runId:guid}/panels/{panelId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> RemovePanel(Guid id, Guid runId, Guid panelId, CancellationToken ct)
    {
        await _mediator.Send(new RemovePanelCommand(id, runId, panelId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/connections")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> AddConnection(Guid id, [FromBody] AddRunConnectionDto data, CancellationToken ct) =>
        (await _mediator.Send(new AddRunConnectionCommand(id, data), ct)).ToCreated();

    [HttpPut("{id:guid}/connections/{connectionId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> UpdateConnection(Guid id, Guid connectionId, [FromBody] UpdateRunConnectionDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateRunConnectionCommand(id, connectionId, data), ct)).ToOk();

    [HttpDelete("{id:guid}/connections/{connectionId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> RemoveConnection(Guid id, Guid connectionId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveRunConnectionCommand(id, connectionId), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/scene/latest")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> GetSceneLatest(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetSceneLatestQuery(id), ct)).ToOk();

    [HttpGet("{id:guid}/scene/versions")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> GetSceneVersions(Guid id, [FromQuery] int limit = 50, CancellationToken ct = default) =>
        (await _mediator.Send(new GetSceneVersionsQuery(id, limit), ct)).ToOk();

    [HttpGet("{id:guid}/scene/version/{sceneVersion:int}")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> GetSceneByVersion(Guid id, int sceneVersion, CancellationToken ct) =>
        (await _mediator.Send(new GetSceneByVersionQuery(id, sceneVersion), ct)).ToOk();

    [HttpPost("{id:guid}/scene")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> SaveScene(Guid id, [FromBody] SaveSceneDto data, CancellationToken ct) =>
        (await _mediator.Send(new SaveSceneCommand(id, data), ct)).ToCreated();

    [HttpPost("{id:guid}/validate")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> Validate(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new ValidateProjectCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/bom/recompute")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> RecomputeBOM(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new RecomputeBOMCommand(id), ct)).ToOk();

    [HttpGet("{id:guid}/bom")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerPriceVisible)]
    public async Task<IActionResult> GetBOM(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetProjectBOMQuery(id), ct)).ToOk();

    [HttpGet("{id:guid}/bom/preview")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerPriceVisible)]
    public async Task<IActionResult> GetBomPreview(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetBomPreviewQuery(id), ct)).ToOk();

    [HttpPut("{id:guid}/bom/lines/{lineId:guid}/price-override")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> OverrideBomLinePrice(Guid id, Guid lineId, [FromBody] OverrideBomLinePriceBody body, CancellationToken ct) =>
        (await _mediator.Send(new OverrideBomLinePriceCommand(id, lineId, body.UnitPriceOverride), ct)).ToOk();

    [HttpPost("{id:guid}/bom/lines/manual")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> AddManualBomLine(Guid id, [FromBody] AddManualBomLineDto body, CancellationToken ct) =>
        (await _mediator.Send(new AddManualBomLineCommand(id, body), ct)).ToCreated();

    [HttpDelete("{id:guid}/bom/lines/{lineId:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.ProjectUpdate)]
    public async Task<IActionResult> DeleteManualBomLine(Guid id, Guid lineId, CancellationToken ct) =>
        (await _mediator.Send(new DeleteManualBomLineCommand(id, lineId), ct)).ToOk();

    [HttpPost("{id:guid}/bom/lines/{lineId:guid}/push-price-to-catalog")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> PushBomLinePriceToCatalog(Guid id, Guid lineId, CancellationToken ct) =>
        (await _mediator.Send(new PushBomLinePriceToCatalogCommand(id, lineId), ct)).ToOk();

    [HttpPost("{id:guid}/cutting-plan/generate")]
    [Authorize(Policy = GlassEnclosurePolicies.CuttingReportDownload)]
    public async Task<IActionResult> GenerateCuttingPlan(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GenerateCuttingPlanCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/optimize-2d-nesting")]
    [Authorize(Policy = GlassEnclosurePolicies.CuttingReportDownload)]
    public async Task<IActionResult> Optimize2DNesting(Guid id, [FromBody] Optimize2DNestingBody body, CancellationToken ct) =>
        (await _mediator.Send(new Optimize2DNestingCommand(
            id,
            body?.Algorithm ?? "MaxRects",
            body?.Heuristic ?? "BestShortSideFit",
            body?.MinimizeSheets ?? true,
            body?.AcceptableUtilization ?? 0.85m,
            body?.GuillotineOnly ?? false,
            body?.AllowRotation ?? true), ct)).ToOk();

    [HttpGet("{id:guid}/cutting-plan")]
    [Authorize(Policy = GlassEnclosurePolicies.CuttingReportDownload)]
    public async Task<IActionResult> GetCuttingReport(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetCuttingReportQuery(id), ct)).ToOk();

    [HttpGet("{id:guid}/stock-availability")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> GetStockAvailability(Guid id, [FromQuery] Guid? warehouseId, CancellationToken ct) =>
        (await _mediator.Send(new GetProjectStockAvailabilityQuery(id, warehouseId), ct)).ToOk();

    [HttpGet("{id:guid}/technical-summary")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> GetTechnicalSummary(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetTechnicalSummaryQuery(id), ct)).ToOk();

    [HttpPost("{id:guid}/share-tokens")]
    [Authorize(Policy = GlassEnclosurePolicies.QuoteSend)]
    public async Task<IActionResult> GenerateShareToken(Guid id, [FromBody] GenerateShareTokenDto data, CancellationToken ct) =>
        (await _mediator.Send(new GenerateShareTokenCommand(id, data), ct)).ToCreated();

    [HttpGet("{id:guid}/share-tokens")]
    [Authorize(Policy = GlassEnclosurePolicies.QuoteSend)]
    public async Task<IActionResult> GetShareTokens(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetShareTokensQuery(id), ct)).ToOk();

    [HttpPost("{id:guid}/convert-to-order")]
    [Authorize(Policy = GlassEnclosurePolicies.OrderConvert)]
    public async Task<IActionResult> ConvertToOrder(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new ConvertProjectToOrderCommand(id), ct)).ToCreated();

    [HttpPost("{id:guid}/release-to-production")]
    [Authorize(Policy = GlassEnclosurePolicies.ProductionRelease)]
    public async Task<IActionResult> ReleaseToProduction(Guid id, [FromBody] ReleaseToProductionDto data, CancellationToken ct) =>
        (await _mediator.Send(new ReleaseToProductionCommand(id, data), ct)).ToCreated();

    [HttpGet("{id:guid}/work-orders")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> GetWorkOrders(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetWorkOrdersByProjectQuery(id), ct)).ToOk();

    [HttpPut("work-orders/{workOrderId:guid}/status")]
    [Authorize(Policy = GlassEnclosurePolicies.ProductionUpdateStatus)]
    public async Task<IActionResult> UpdateWorkOrderStatus(Guid workOrderId, [FromBody] UpdateWorkOrderStatusBody body, CancellationToken ct) =>
        (await _mediator.Send(new UpdateWorkOrderStatusCommand(workOrderId, body.Status), ct)).ToOk();

    [HttpPost("work-orders/{workOrderId:guid}/defect")]
    [Authorize(Policy = GlassEnclosurePolicies.ProductionRecordDefect)]
    public async Task<IActionResult> RecordWorkOrderDefect(Guid workOrderId, [FromBody] RecordDefectBody body, CancellationToken ct) =>
        (await _mediator.Send(new RecordWorkOrderDefectCommand(workOrderId, body.DefectNotes), ct)).ToOk();

    [HttpGet("{id:guid}/notifications")]
    [Authorize(Policy = GlassEnclosurePolicies.DesignerOpen)]
    public async Task<IActionResult> GetNotificationHistory(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetNotificationHistoryQuery(id), ct)).ToOk();
}

public record UpdateWorkOrderStatusBody(string Status);
public record OverrideBomLinePriceBody(decimal? UnitPriceOverride);
public record RecordDefectBody(string DefectNotes);
public record CreateProjectFromTemplateBody(Guid CustomerId, string ProjectName, string? Currency);
public record Optimize2DNestingBody(
    string? Algorithm,
    string? Heuristic,
    bool? MinimizeSheets,
    decimal? AcceptableUtilization,
    bool? GuillotineOnly,
    bool? AllowRotation);
