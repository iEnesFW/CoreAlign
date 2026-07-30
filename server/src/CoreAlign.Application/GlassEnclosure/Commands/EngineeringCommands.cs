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

// Read-only, always-live BOM compose of the current (autosaved) scene — the single source of truth
// for the designer's live cost preview. Unlike GetProjectBOMQuery it never returns stale persisted
// lines and never persists (a query never triggers SaveChanges), so it is safe to call on every edit.
public record GetBomPreviewQuery(Guid ProjectId) : IRequest<BOMSummaryDto>;

public record GetCuttingReportQuery(Guid ProjectId) : IRequest<CuttingReportDto?>;

/// <summary>Latest persisted advanced-nesting run. The optimisation was written to a plan row but never read back, so it lived only in React state and vanished on the next tab switch.</summary>
public record GetGlass2DNestingReportQuery(Guid ProjectId) : IRequest<Glass2DNestingReportDto?>;

public record GetTechnicalSummaryQuery(Guid ProjectId) : IRequest<TechnicalSummaryDto>;
