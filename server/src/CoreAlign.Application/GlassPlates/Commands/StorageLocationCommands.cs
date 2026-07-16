using CoreAlign.Application.Common;
using CoreAlign.Application.GlassPlates.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Commands;

public record CreateStorageLocationCommand(
    Guid WarehouseId,
    string Code,
    string Name,
    StorageLocationKind Kind,
    Guid? ParentLocationId,
    string? Notes) : IRequest<StorageLocationDto>, ITransactionalRequest;

public record UpdateStorageLocationCommand(
    Guid Id,
    string Code,
    string Name,
    StorageLocationKind Kind,
    Guid? ParentLocationId,
    bool IsActive,
    string? Notes) : IRequest<StorageLocationDto>, ITransactionalRequest;
