using CoreAlign.Application.Common;
using CoreAlign.Application.Manufacturing.DTOs;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Commands;

public record CreateProductionJobCommand(
    Guid ProductId,
    decimal PlannedQuantity,
    string UnitOfMeasure,
    Guid? WarehouseId,
    Guid? RoutingId,
    DateTime? PlannedStartDateUtc,
    DateTime? DueDateUtc,
    string? Notes) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record ReleaseProductionJobCommand(
    Guid Id,
    Guid WarehouseId) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record StartJobStepCommand(
    Guid JobId,
    int StepNumber,
    Guid OperatorId) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record FinishJobStepCommand(
    Guid JobId,
    int StepNumber,
    decimal GoodQuantity,
    decimal ScrappedQuantity,
    Guid? ScrapReasonCodeId,
    decimal? ActualSetupMinutes,
    decimal? ActualRunMinutes,
    Guid OperatorId) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record SkipJobStepCommand(
    Guid JobId,
    int StepNumber) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record ReworkToStepCommand(
    Guid JobId,
    int TargetStepNumber,
    int FromStepNumber,
    string Reason) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record PutJobOnHoldCommand(Guid Id) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record ResumeJobCommand(Guid Id) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record CancelProductionJobCommand(
    Guid Id,
    string? Reason) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;

public record CompleteProductionJobCommand(
    Guid Id,
    decimal CompletedQuantity,
    Guid WarehouseId) : IRequest<ProductionJobDetailDto>, ITransactionalRequest;
