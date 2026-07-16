using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Manufacturing;

public class RoutingHandlerTests
{
    private readonly IProductionRoutingRepository _routings = Substitute.For<IProductionRoutingRepository>();
    private readonly IWorkCenterRepository _workCenters = Substitute.For<IWorkCenterRepository>();
    private readonly IWorkCenterOperatorRepository _operators = Substitute.For<IWorkCenterOperatorRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    private static readonly Guid TenantId = Guid.NewGuid();

    public RoutingHandlerTests() => _tenant.RequireTenantId().Returns(TenantId);

    private static WorkCenter ActiveWorkCenter() => new("WC-1", "Kesim", 480m);
    private static Employee ActiveEmployee() =>
        new("E-1", "Ali", "Veli", "12345678901", DateOnly.FromDateTime(DateTime.UtcNow.Date), 30000m);

    private static RoutingStepInput Step(int n, Guid wc) =>
        new(n, wc, "Kesim", RoutingOperationType.Cutting, 5m, 2m, null, 0m, null, false);

    [Fact]
    public async Task Create_routing_rejects_duplicate_code()
    {
        _routings.CodeExistsAsync(TenantId, "TR-1", null, Arg.Any<CancellationToken>()).Returns(true);
        var sut = new CreateProductionRoutingHandler(_routings, _tenant);

        var act = () => sut.Handle(new CreateProductionRoutingCommand("TR-1", "Hat", null), default);

        await act.Should().ThrowAsync<RoutingCodeConflictException>();
        await _routings.DidNotReceive().AddAsync(Arg.Any<ProductionRouting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_routing_adds_draft()
    {
        _routings.CodeExistsAsync(TenantId, "TR-1", null, Arg.Any<CancellationToken>()).Returns(false);
        var sut = new CreateProductionRoutingHandler(_routings, _tenant);

        var dto = await sut.Handle(new CreateProductionRoutingCommand("TR-1", "Hat", "d"), default);

        dto.Status.Should().Be(RoutingStatus.Draft);
        dto.Code.Should().Be("TR-1");
        await _routings.Received(1).AddAsync(Arg.Any<ProductionRouting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_steps_rejects_missing_work_center()
    {
        var routing = new ProductionRouting("TR-1", "Hat");
        _routings.GetByIdAsync(TenantId, routing.Id, Arg.Any<CancellationToken>()).Returns(routing);
        var wc = Guid.NewGuid();
        _workCenters.GetActiveIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());
        var sut = new SetRoutingStepsHandler(_routings, _workCenters, _tenant);

        var act = () => sut.Handle(new SetRoutingStepsCommand(routing.Id, new[] { Step(1, wc) }), default);

        await act.Should().ThrowAsync<WorkCenterNotFoundException>();
        await _routings.DidNotReceive().AddStepsAsync(Arg.Any<IEnumerable<RoutingStep>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_steps_manages_step_rows_explicitly()
    {
        var routing = new ProductionRouting("TR-1", "Hat");
        var wc = Guid.NewGuid();
        _routings.GetByIdAsync(TenantId, routing.Id, Arg.Any<CancellationToken>()).Returns(routing);
        _workCenters.GetActiveIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { wc });
        _workCenters.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WorkCenter>());
        var sut = new SetRoutingStepsHandler(_routings, _workCenters, _tenant);

        var dto = await sut.Handle(
            new SetRoutingStepsCommand(routing.Id, new[] { Step(1, wc), Step(2, wc) }), default);

        dto.Steps.Should().HaveCount(2);
        _routings.Received(1).RemoveSteps(Arg.Any<IEnumerable<RoutingStep>>());
        await _routings.Received(1).AddStepsAsync(
            Arg.Is<IEnumerable<RoutingStep>>(s => s.Count() == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_routing_rejects_non_active()
    {
        var product = new Product("SKU", "Cam", price: 100m);
        var routing = new ProductionRouting("TR-1", "Hat");
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _routings.GetByIdReadAsync(TenantId, routing.Id, Arg.Any<CancellationToken>()).Returns(routing);
        var sut = new AssignRoutingToProductHandler(_products, _routings, _tenant);

        var act = () => sut.Handle(new AssignRoutingToProductCommand(product.Id, routing.Id), default);

        await act.Should().ThrowAsync<RoutingNotActiveException>();
        product.RoutingId.Should().BeNull();
    }

    [Fact]
    public async Task Assign_routing_clears_when_null()
    {
        var product = new Product("SKU", "Cam", price: 100m);
        product.AssignRouting(Guid.NewGuid());
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var sut = new AssignRoutingToProductHandler(_products, _routings, _tenant);

        await sut.Handle(new AssignRoutingToProductCommand(product.Id, null), default);

        product.RoutingId.Should().BeNull();
    }

    [Fact]
    public async Task Create_operator_rejects_inactive_work_center()
    {
        _workCenters.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new WorkCenter("WC-1", "Kesim", 480m, isActive: false) });
        var sut = new CreateWorkCenterOperatorHandler(_operators, _workCenters, _employees, _tenant);

        var act = () => sut.Handle(new CreateWorkCenterOperatorCommand(
            Guid.NewGuid(), Guid.NewGuid(), OperatorQualificationLevel.Qualified, false, null, null), default);

        await act.Should().ThrowAsync<WorkCenterNotFoundException>();
    }

    [Fact]
    public async Task Create_operator_rejects_terminated_employee()
    {
        var wc = ActiveWorkCenter();
        var emp = ActiveEmployee();
        emp.Terminate(DateOnly.FromDateTime(DateTime.UtcNow.Date), "left");
        _workCenters.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { wc });
        _employees.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(emp);
        var sut = new CreateWorkCenterOperatorHandler(_operators, _workCenters, _employees, _tenant);

        var act = () => sut.Handle(new CreateWorkCenterOperatorCommand(
            wc.Id, emp.Id, OperatorQualificationLevel.Qualified, false, null, null), default);

        await act.Should().ThrowAsync<EmployeeNotFoundException>();
    }

    [Fact]
    public async Task Create_operator_rejects_duplicate_active_assignment()
    {
        var wc = ActiveWorkCenter();
        var emp = ActiveEmployee();
        _workCenters.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { wc });
        _employees.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(emp);
        _operators.ActiveAssignmentExistsAsync(TenantId, wc.Id, emp.Id, null, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = new CreateWorkCenterOperatorHandler(_operators, _workCenters, _employees, _tenant);

        var act = () => sut.Handle(new CreateWorkCenterOperatorCommand(
            wc.Id, emp.Id, OperatorQualificationLevel.Expert, true, null, null), default);

        await act.Should().ThrowAsync<WorkCenterOperatorAlreadyAssignedException>();
        await _operators.DidNotReceive().AddAsync(Arg.Any<WorkCenterOperator>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_operator_happy_path_adds_and_maps()
    {
        var wc = ActiveWorkCenter();
        var emp = ActiveEmployee();
        _workCenters.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { wc });
        _employees.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(emp);
        _operators.ActiveAssignmentExistsAsync(TenantId, wc.Id, emp.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);
        var sut = new CreateWorkCenterOperatorHandler(_operators, _workCenters, _employees, _tenant);

        var dto = await sut.Handle(new CreateWorkCenterOperatorCommand(
            wc.Id, emp.Id, OperatorQualificationLevel.Expert, true, null, "usta"), default);

        dto.WorkCenterCode.Should().Be("WC-1");
        dto.EmployeeName.Should().Be("Ali Veli");
        dto.EmployeeActive.Should().BeTrue();
        dto.IsPrimary.Should().BeTrue();
        await _operators.Received(1).AddAsync(Arg.Any<WorkCenterOperator>(), Arg.Any<CancellationToken>());
    }
}
