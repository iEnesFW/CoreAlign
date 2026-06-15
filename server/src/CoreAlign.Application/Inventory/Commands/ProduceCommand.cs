using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Inventory.Commands;

public record ProduceCommand(
    Guid ParentProductId,
    Guid WarehouseId,
    decimal Quantity,
    string? Reference) : IRequest<Unit>, ITransactionalRequest;
