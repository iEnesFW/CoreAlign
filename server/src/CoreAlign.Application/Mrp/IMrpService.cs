namespace CoreAlign.Application.Mrp;

public interface IMrpService
{
    Task<DemandForecastDto?> CalculateDemandForecastAsync(Guid productId, int windowDays = 90, CancellationToken cancellationToken = default);
    Task<ReorderPointDto?> CalculateReorderPointAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<StockProjectionDto?> ProjectStockBalanceAsync(Guid productId, int daysAhead = 30, CancellationToken cancellationToken = default);
    Task<MrpSuggestionResultDto> GenerateRequisitionSuggestionsAsync(DateTime asOfDateUtc, CancellationToken cancellationToken = default);
    Task<MrpDashboardDto> GetDashboardAsync(int topN, CancellationToken cancellationToken = default);
}
