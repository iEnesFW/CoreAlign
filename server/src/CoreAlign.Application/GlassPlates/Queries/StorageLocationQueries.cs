using CoreAlign.Application.GlassPlates.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Queries;

public record ListStorageLocationsQuery(Guid? WarehouseId) : IRequest<IReadOnlyList<StorageLocationDto>>;
