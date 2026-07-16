using CoreAlign.Application.Common;
using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Commands;

public record RoutingStepInput(
    int StepNumber,
    Guid WorkCenterId,
    string OperationName,
    RoutingOperationType OperationType,
    decimal SetupTimeMinutes,
    decimal RunTimeMinutesPerUnit,
    decimal? RunTimeMinutesPerSqm,
    decimal ScrapPercentage,
    string? Instructions,
    bool IsOptional);

public record CreateProductionRoutingCommand(
    string Code,
    string Name,
    string? Description) : IRequest<ProductionRoutingDto>, ITransactionalRequest;

public record UpdateProductionRoutingCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description) : IRequest<ProductionRoutingDto>, ITransactionalRequest;

public record SetRoutingStepsCommand(
    Guid RoutingId,
    IReadOnlyList<RoutingStepInput> Steps) : IRequest<ProductionRoutingDto>, ITransactionalRequest;

public record ActivateRoutingCommand(Guid Id) : IRequest<ProductionRoutingDto>, ITransactionalRequest;

public record ArchiveRoutingCommand(Guid Id) : IRequest<ProductionRoutingDto>, ITransactionalRequest;

public record RestoreRoutingToDraftCommand(Guid Id) : IRequest<ProductionRoutingDto>, ITransactionalRequest;

public record DeleteProductionRoutingCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record AssignRoutingToProductCommand(
    Guid ProductId,
    Guid? RoutingId) : IRequest<Unit>, ITransactionalRequest;
