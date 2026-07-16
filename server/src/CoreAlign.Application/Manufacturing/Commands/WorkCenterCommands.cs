using CoreAlign.Application.Common;
using CoreAlign.Application.Manufacturing.DTOs;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Commands;

public record CreateWorkCenterCommand(
    string Code,
    string Name,
    decimal DailyCapacityMinutes) : IRequest<WorkCenterDto>, ITransactionalRequest;

public record UpdateWorkCenterCommand(
    Guid Id,
    string Code,
    string Name,
    decimal DailyCapacityMinutes,
    bool IsActive) : IRequest<WorkCenterDto>, ITransactionalRequest;
