using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Manufacturing;

public class ProductionJobHandlerTests
{
    private readonly IProductionJobRepository _jobs = Substitute.For<IProductionJobRepository>();
    private readonly IProductionRoutingRepository _routings = Substitute.For<IProductionRoutingRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IWorkCenterRepository _workCenters = Substitute.For<IWorkCenterRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IPlannedProductionOrderRepository _plannedOrders =
        Substitute.For<IPlannedProductionOrderRepository>();
    private readonly IStockMovementRepository _stockMovements = Substitute.For<IStockMovementRepository>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    private readonly Guid _tenantId = Guid.NewGuid();

    private ProductionJobCommandHandlers CreateSut()
    {
        _tenant.CurrentTenantId.Returns(_tenantId);
        _dateTime.UtcNow.Returns(new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc));

        return new ProductionJobCommandHandlers(
            _tenant,
            _jobs,
            _routings,
            _products,
            _workCenters,
            _sequences,
            _plannedOrders,
            _stockMovements,
            _dateTime);
    }

    [Fact]
    public async Task Creating_a_job_consumes_the_sequence_and_persists_the_job()
    {
        var productId = Guid.NewGuid();
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new Product("SKU-1", "Widget"));
        _sequences
            .ConsumeAsync(
                DocumentSequenceType.ProductionJobNumber,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns("JOB-100");

        var sut = CreateSut();
        var command = new CreateProductionJobCommand(productId, 100m, "PCS", null, null, null, null, null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.JobNumber.Should().Be("JOB-100");
        await _jobs.Received(1).AddAsync(Arg.Any<ProductionJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creating_a_job_for_an_unknown_product_is_rejected()
    {
        _products.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var sut = CreateSut();
        var command = new CreateProductionJobCommand(Guid.NewGuid(), 5m, "PCS", null, null, null, null, null);

        await FluentActions
            .Awaiting(() => sut.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<ProductNotFoundException>();

        await _jobs.DidNotReceive().AddAsync(Arg.Any<ProductionJob>(), Arg.Any<CancellationToken>());
    }
}
