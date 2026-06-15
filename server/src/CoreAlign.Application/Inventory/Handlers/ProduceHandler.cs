using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Inventory.Handlers;

public class ProduceHandler : IRequestHandler<ProduceCommand, Unit>
{
    private readonly IProductionExecutionService _execution;
    private readonly IUnitOfWork _uow;

    public ProduceHandler(
        IProductionExecutionService execution,
        IUnitOfWork uow)
    {
        _execution = execution;
        _uow = uow;
    }

    public async Task<Unit> Handle(ProduceCommand request, CancellationToken cancellationToken)
    {
        await _execution.ExecuteAsync(
            request.ParentProductId,
            request.WarehouseId,
            request.Quantity,
            request.Reference,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
