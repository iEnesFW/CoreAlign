using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Commands;

public record RecomputeBOMCommand(Guid ProjectId) : IRequest<BOMSummaryDto>, ITransactionalRequest;

public record GenerateCuttingPlanCommand(Guid ProjectId) : IRequest<CuttingReportDto>, ITransactionalRequest;

public record Optimize2DNestingCommand(
    Guid ProjectId,
    string Algorithm,
    string Heuristic,
    bool MinimizeSheets,
    decimal AcceptableUtilization,
    bool GuillotineOnly,
    bool AllowRotation) : IRequest<Glass2DNestingReportDto>, ITransactionalRequest;

public record GetProjectBOMQuery(Guid ProjectId) : IRequest<BOMSummaryDto>;

public record GetCuttingReportQuery(Guid ProjectId) : IRequest<CuttingReportDto?>;

public record GetTechnicalSummaryQuery(Guid ProjectId) : IRequest<TechnicalSummaryDto>;
