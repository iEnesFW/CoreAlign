using CoreAlign.Application.Billing;
using CoreAlign.Application.Billing.Handlers;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Billing;

public class CreateSubscriptionOrderHandlerTests
{
    private readonly IModuleRepository _modules = Substitute.For<IModuleRepository>();
    private readonly IModulePricePlanRepository _plans = Substitute.For<IModulePricePlanRepository>();
    private readonly ISubscriptionOrderRepository _orders = Substitute.For<ISubscriptionOrderRepository>();
    private readonly IPaymentAttemptRepository _attempts = Substitute.For<IPaymentAttemptRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly CreateSubscriptionOrderHandler _sut;

    public CreateSubscriptionOrderHandlerTests()
    {
        _tenant.RequireTenantId().Returns(_tenantId);
        _tenant.CurrentTenantId.Returns(_tenantId);

        _gateway.Name.Returns("mock");
        _gateway.CreateIntentAsync(Arg.Any<PaymentIntentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentIntentResult("intent-1", "/redirect", PaymentIntentStatus.Pending, null, "{}"));

        _sequences.ConsumeAsync(
                DocumentSequenceType.SubscriptionOrderNumber,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns("SUB-2026-00001");

        var registry = new PaymentGatewayRegistry(new[] { _gateway });
        var options = Options.Create(new BillingOptions { DefaultGatewayName = "mock" });
        _sut = new CreateSubscriptionOrderHandler(_modules, _plans, _orders, _attempts, _sequences, registry, _tenant, _uow, options);
    }

    [Fact]
    public async Task Snapshots_module_and_plan_into_order_items()
    {
        var moduleId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var module = new Module("Sales", "Sales", null, "Sales", null, 0, true, false);
        typeof(Module).GetProperty(nameof(Module.Id))!.SetValue(module, moduleId);

        var plan = new ModulePricePlan(moduleId, "Yearly", "Yıllık", 365, 999m, "TRY", true, 1);
        typeof(ModulePricePlan).GetProperty(nameof(ModulePricePlan.Id))!.SetValue(plan, planId);

        _modules.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { module });
        _plans.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { plan });

        SubscriptionOrder? captured = null;
        await _orders.AddAsync(Arg.Do<SubscriptionOrder>(o => captured = o), Arg.Any<CancellationToken>());

        var command = new CreateSubscriptionOrderCommand(
            new[] { new OrderItemInput(moduleId, planId) },
            GatewayName: "mock",
            CurrentUserId: Guid.NewGuid());

        var result = await _sut.Handle(command, default);

        result.Should().NotBeNull();
        result.GatewayName.Should().Be("mock");
        result.RedirectUrl.Should().Be("/redirect");
        result.Order.Status.Should().Be(SubscriptionOrderStatus.PendingPayment);
        result.Order.TotalAmount.Should().Be(999m);
        result.Order.OrderNumber.Should().Be("SUB-2026-00001");

        captured.Should().NotBeNull();
        captured!.Items.Should().HaveCount(1);
        var item = captured.Items.First();
        item.ModuleCode.Should().Be("Sales");
        item.ModuleName.Should().Be("Sales");
        item.PlanLabel.Should().Be("Yıllık");
        item.DurationDays.Should().Be(365);
        item.UnitPrice.Should().Be(999m);
        item.Currency.Should().Be("TRY");
        captured.GatewayName.Should().Be("mock");
        captured.GatewayIntentId.Should().Be("intent-1");

        await _attempts.Received(1).AddAsync(
            Arg.Is<PaymentAttempt>(a => a.Status == PaymentAttemptStatus.Initiated && a.IntentId == "intent-1"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_inactive_module()
    {
        var moduleId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var module = new Module("Sales", "Sales", null, null, null, 0, isActive: false, isCore: false);
        typeof(Module).GetProperty(nameof(Module.Id))!.SetValue(module, moduleId);
        var plan = new ModulePricePlan(moduleId, "Yearly", "Yıllık", 365, 999m, "TRY", true, 1);
        typeof(ModulePricePlan).GetProperty(nameof(ModulePricePlan.Id))!.SetValue(plan, planId);

        _modules.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { module });
        _plans.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { plan });

        var command = new CreateSubscriptionOrderCommand(
            new[] { new OrderItemInput(moduleId, planId) },
            GatewayName: "mock",
            CurrentUserId: Guid.NewGuid());

        var act = async () => await _sut.Handle(command, default);
        await act.Should().ThrowAsync<Domain.Exceptions.ModuleNotFoundException>();
    }
}
