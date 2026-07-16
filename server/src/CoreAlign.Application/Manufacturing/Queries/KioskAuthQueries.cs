using CoreAlign.Application.Manufacturing.DTOs;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Queries;

public record VerifyOperatorPinQuery(Guid OperatorId, string PinCode) : IRequest<Guid?>;

public record GetActiveKioskStepsQuery(Guid WorkCenterId) : IRequest<IReadOnlyList<KioskStepDto>>;

public record KioskStepDto(
    Guid JobId,
    string JobNumber,
    string ProductName,
    int StepNumber,
    string OperationName,
    decimal InputQuantity,
    string Status,
    DateTime? StartedAtUtc,
    Guid? AssignedOperatorId
);
