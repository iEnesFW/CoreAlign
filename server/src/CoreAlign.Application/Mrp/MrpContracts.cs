using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Mrp;

public record DemandForecastDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    int WindowDays,
    decimal TotalDemand,
    decimal AverageDailyDemand,
    decimal? PeakDailyDemand,
    DateTime AsOfUtc);

public record ReorderPointDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal SafetyStock,
    int LeadTimeDays,
    decimal AverageDailyDemand,
    decimal ComputedReorderPoint,
    decimal StoredReorderPoint);

public record StockProjectionPoint(
    DateTime Date,
    decimal ProjectedQuantity,
    decimal Demand,
    decimal OnOrder,
    decimal Committed);

public record StockProjectionDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal CurrentOnHand,
    decimal CurrentReserved,
    decimal TotalOnOrder,
    decimal TotalCommitted,
    decimal ReorderPoint,
    int DaysAhead,
    IReadOnlyList<StockProjectionPoint> Points,
    bool ShouldReorder,
    decimal SuggestedOrderQuantity);

public record MrpReorderCandidateDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal OnHand,
    decimal Reserved,
    decimal OnOrder,
    decimal Committed,
    decimal ProjectedAvailable,
    decimal ReorderPoint,
    decimal SuggestedOrderQuantity,
    Guid? PreferredSupplierId,
    int LeadTimeDays,
    int DaysUntilStockOut);

public record MrpDashboardDto(
    int TotalProductsTracked,
    int ReorderCandidateCount,
    int PendingRequisitionCount,
    int OpenPurchaseOrderCount,
    IReadOnlyList<MrpReorderCandidateDto> TopCandidates,
    DateTime GeneratedAtUtc);

public record PurchaseRequisitionLineDto(
    Guid Id,
    int LineNumber,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal QuantityRequested,
    decimal EstimatedUnitCost,
    decimal EstimatedLineTotal,
    Guid? PreferredSupplierId,
    DateTime? ExpectedDeliveryDate,
    string? Notes);

public record PurchaseRequisitionDto(
    Guid Id,
    string Number,
    PurchaseRequisitionStatus Status,
    PurchaseRequisitionReason Reason,
    DateTime RequestedAtUtc,
    Guid RequestedByUserId,
    Guid? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? RejectedAtUtc,
    string? RejectReason,
    DateTime? CancelledAtUtc,
    string? CancelReason,
    DateTime? ConvertedAtUtc,
    Guid? ConvertedPurchaseOrderId,
    string? Notes,
    IReadOnlyList<PurchaseRequisitionLineDto> Lines,
    decimal EstimatedTotal,
    DateTime CreatedAtUtc,
    long ConcurrencyToken);

public record PurchaseRequisitionLineInput(
    Guid ProductId,
    decimal QuantityRequested,
    decimal EstimatedUnitCost,
    Guid? PreferredSupplierId = null,
    DateTime? ExpectedDeliveryDate = null,
    string? Notes = null);

public record CreatePurchaseRequisitionCommand(
    PurchaseRequisitionReason Reason,
    List<PurchaseRequisitionLineInput> Lines,
    string? Notes = null) : IRequest<PurchaseRequisitionDto>, ITransactionalRequest;

public record SubmitPurchaseRequisitionCommand(Guid Id) : IRequest<PurchaseRequisitionDto>, ITransactionalRequest;
public record ApprovePurchaseRequisitionCommand(Guid Id) : IRequest<PurchaseRequisitionDto>, ITransactionalRequest;
public record RejectPurchaseRequisitionCommand(Guid Id, string? Reason = null) : IRequest<PurchaseRequisitionDto>, ITransactionalRequest;
public record CancelPurchaseRequisitionCommand(Guid Id, string? Reason = null) : IRequest<PurchaseRequisitionDto>, ITransactionalRequest;
public record ConvertRequisitionToPurchaseOrderCommand(Guid Id, Guid VendorId, string Currency, DateTime? ExpectedDate = null) : IRequest<Guid>, ITransactionalRequest;
public record GenerateMrpSuggestionsCommand(DateTime? AsOfDateUtc = null) : IRequest<MrpSuggestionResultDto>, ITransactionalRequest;

public record MrpSuggestionResultDto(
    int CandidatesEvaluated,
    int RequisitionsCreated,
    int LinesCreated,
    IReadOnlyList<Guid> RequisitionIds,
    DateTime AsOfDateUtc);

public record GetMrpDashboardQuery(int TopN = 20) : IRequest<MrpDashboardDto>;

public record GetStockProjectionQuery(Guid ProductId, int DaysAhead = 30) : IRequest<StockProjectionDto?>;

public record GetDemandForecastQuery(Guid ProductId, int WindowDays = 90) : IRequest<DemandForecastDto?>;

public record ListPurchaseRequisitionsQuery(
    PurchaseRequisitionStatus? Status = null,
    Guid? ProductId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<PurchaseRequisitionDto>>;
