using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.GlassPlates.Commands;

public record AssignUserWarehousesCommand(
    Guid UserId,
    IReadOnlyList<Guid> WarehouseIds,
    Guid GrantedByUserId) : IRequest<IReadOnlyList<Guid>>, ITransactionalRequest;

public record GetUserWarehouseAccessQuery(Guid UserId) : IRequest<IReadOnlyList<Guid>>;
