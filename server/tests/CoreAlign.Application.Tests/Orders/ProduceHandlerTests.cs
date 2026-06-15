using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Inventory.Handlers;
using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Orders;

public class ProduceHandlerTests
{
    private readonly IProductionExecutionService _execution = Substitute.For<IProductionExecutionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ProduceHandler _sut;

    private static readonly Guid ParentId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    public ProduceHandlerTests()
    {
        _sut = new ProduceHandler(_execution, _uow);
    }

    [Fact]
    public async Task Produce_delegates_to_execution_service_and_persists()
    {
        _execution.ExecuteAsync(ParentId, WarehouseId, 5m, "ref", Arg.Any<CancellationToken>())
            .Returns(new ProductionExecutionResult(ParentId, WarehouseId, 5m, 2, 50m, 250m));

        await _sut.Handle(new ProduceCommand(ParentId, WarehouseId, 5m, "ref"), default);

        await _execution.Received(1).ExecuteAsync(
            ParentId, WarehouseId, 5m, "ref", Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
