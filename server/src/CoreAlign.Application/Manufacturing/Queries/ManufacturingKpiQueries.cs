using MediatR;

namespace CoreAlign.Application.Manufacturing.Queries;

public record GetManufacturingKpiSummaryQuery(DateTime StartDateUtc, DateTime EndDateUtc) : IRequest<ManufacturingKpiSummaryDto>;

public record ManufacturingKpiSummaryDto(
    int TotalJobsCompleted,
    decimal TotalGoodQuantity,
    decimal TotalScrappedQuantity,
    decimal OverallYieldPercentage,
    IReadOnlyList<WorkCenterKpiDto> WorkCenterKpis
);

public record WorkCenterKpiDto(
    Guid WorkCenterId,
    string WorkCenterName,
    decimal TotalScrappedQuantity,
    decimal TotalGoodQuantity,
    decimal YieldPercentage,
    decimal TotalRunMinutes
);
