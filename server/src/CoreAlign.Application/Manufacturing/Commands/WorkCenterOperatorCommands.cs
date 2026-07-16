using CoreAlign.Application.Common;
using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Commands;

public record CreateWorkCenterOperatorCommand(
    Guid WorkCenterId,
    Guid EmployeeId,
    OperatorQualificationLevel QualificationLevel,
    bool IsPrimary,
    DateOnly? CertifiedOn,
    string? Notes,
    string? PinCode) : IRequest<WorkCenterOperatorDto>, ITransactionalRequest;

public record UpdateWorkCenterOperatorCommand(
    Guid Id,
    OperatorQualificationLevel QualificationLevel,
    bool IsPrimary,
    bool IsActive,
    DateOnly? CertifiedOn,
    string? Notes,
    string? PinCode) : IRequest<WorkCenterOperatorDto>, ITransactionalRequest;

public record DeactivateWorkCenterOperatorCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record ActivateWorkCenterOperatorCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;
