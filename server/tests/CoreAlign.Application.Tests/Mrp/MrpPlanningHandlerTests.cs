using CoreAlign.Application.B2B;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Mrp;

public class MrpPlanningHandlerTests
{
    private static MrpPlannedOrder NewPlannedOrder(Guid planRunId) =>
        new(Guid.NewGuid(), 0, 40m,
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 26, 0, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(), 5m, LotSizingPolicy.MinMax)
        { TenantId = Guid.NewGuid() };

    [Fact]
    public async Task GetPegging_throws_404_when_plan_run_missing()
    {
        var repo = Substitute.For<IMrpPlanRunRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), false, Arg.Any<CancellationToken>())
            .Returns((MrpPlanRun?)null);
        var handler = new GetMrpPeggingHandler(repo);

        var act = () => handler.Handle(new GetMrpPeggingQuery(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<MrpPlanRunNotFoundException>();
    }

    [Fact]
    public async Task FirmPlannedOrder_throws_404_when_order_missing()
    {
        var repo = Substitute.For<IMrpPlanRunRepository>();
        repo.GetPlannedOrderByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MrpPlannedOrder?)null);
        var handler = new FirmMrpPlannedOrderHandler(repo);

        var act = () => handler.Handle(new FirmMrpPlannedOrderCommand(Guid.NewGuid(), Guid.NewGuid(), 5m), default);

        await act.Should().ThrowAsync<MrpPlannedOrderNotFoundException>();
    }

    [Fact]
    public async Task FirmPlannedOrder_applies_override_and_returns_dto()
    {
        var planRunId = Guid.NewGuid();
        var order = NewPlannedOrder(planRunId);
        var repo = Substitute.For<IMrpPlanRunRepository>();
        repo.GetPlannedOrderByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var handler = new FirmMrpPlannedOrderHandler(repo);

        var dto = await handler.Handle(
            new FirmMrpPlannedOrderCommand(order.Id, Guid.NewGuid(), OverrideQuantity: 70m), default);

        dto.IsFirmed.Should().BeTrue();
        dto.Quantity.Should().Be(70m);
    }

    [Fact]
    public async Task DismissActionMessage_throws_404_when_missing()
    {
        var repo = Substitute.For<IMrpPlanRunRepository>();
        repo.GetActionMessageByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MrpActionMessage?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var handler = new DismissMrpActionMessageHandler(repo, currentUser);

        var act = () => handler.Handle(new DismissMrpActionMessageCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<MrpActionMessageNotFoundException>();
    }

    [Fact]
    public async Task DismissActionMessage_marks_dismissed_with_current_user()
    {
        var message = new MrpActionMessage(
            Guid.NewGuid(), MrpActionType.BelowSafetyStock, MrpActionSeverity.Warning, 1m,
            null, null, null, null, 4, "Below safety")
        { TenantId = Guid.NewGuid() };
        var repo = Substitute.For<IMrpPlanRunRepository>();
        repo.GetActionMessageByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);
        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.UserId.Returns(userId);
        var handler = new DismissMrpActionMessageHandler(repo, currentUser);

        await handler.Handle(new DismissMrpActionMessageCommand(message.Id), default);

        message.IsDismissed.Should().BeTrue();
        message.DismissedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task ReleasePlannedOrders_throws_404_when_plan_run_missing()
    {
        var workbench = Substitute.For<IMrpWorkbenchService>();
        var repo = Substitute.For<IMrpPlanRunRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), false, Arg.Any<CancellationToken>())
            .Returns((MrpPlanRun?)null);
        var handler = new ReleasePlannedOrdersHandler(workbench, repo);

        var act = () => handler.Handle(
            new ReleasePlannedOrdersCommand(Guid.NewGuid(), new[] { Guid.NewGuid() }, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<MrpPlanRunNotFoundException>();
        await workbench.DidNotReceiveWithAnyArgs().ReleaseAsync(default, default!, default, default);
    }

    [Fact]
    public async Task CommitPlan_delegates_to_workbench_and_maps_dto()
    {
        var run = new MrpPlanRun("MRP00001", DateTime.UtcNow, MrpBucketKind.Day, 60, Guid.NewGuid())
        { TenantId = Guid.NewGuid() };
        var workbench = Substitute.For<IMrpWorkbenchService>();
        workbench.CommitAsync(Arg.Any<DateTime>(), MrpBucketKind.Day, 60, Arg.Any<Guid>(), Arg.Any<MrpPlanningMode>(), Arg.Any<CancellationToken>())
            .Returns(run);
        var handler = new CommitMrpPlanHandler(workbench);

        var dto = await handler.Handle(
            new CommitMrpPlanCommand(Guid.NewGuid(), DateTime.UtcNow, MrpBucketKind.Day, 60), default);

        dto.Id.Should().Be(run.Id);
        dto.Number.Should().Be("MRP00001");
    }
}
