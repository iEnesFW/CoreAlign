using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Manufacturing;

public class ProductionJobDomainTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid OperatorId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 7, 26, 8, 0, 0, DateTimeKind.Utc);

    private static ProductionJob NewJob(decimal quantity = 100m) =>
        new("JOB-100", ProductId, quantity, "PCS", null, null, null, null, null);

    private static ProductionJobStepSnapshot Step(int number, string name, RoutingOperationType type) =>
        new(number, null, null, name, type, 10m, 5m, null, 0m, null, false);

    private static ProductionJob ReleasedJob()
    {
        var job = NewJob();
        job.SnapshotRouting(
            null,
            "R-1",
            "Test routing",
            1,
            new[] { Step(1, "Cut", RoutingOperationType.Cutting), Step(2, "Edge", RoutingOperationType.Edging) });
        job.Release(WarehouseId, Now);
        return job;
    }

    [Fact]
    public void Creating_a_job_starts_in_draft()
    {
        var job = NewJob();

        job.Status.Should().Be(ProductionJobStatus.Draft);
        job.JobNumber.Should().Be("JOB-100");
        job.PlannedQuantity.Should().Be(100m);
        job.Steps.Should().BeEmpty();
    }

    [Fact]
    public void Creating_a_job_without_a_number_is_rejected()
    {
        var act = () => new ProductionJob("  ", ProductId, 10m, "PCS", null, null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Creating_a_job_with_non_positive_quantity_is_rejected()
    {
        var act = () => new ProductionJob("JOB-1", ProductId, 0m, "PCS", null, null, null, null, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Snapshotting_a_routing_on_a_draft_job_seeds_the_steps()
    {
        var job = NewJob();

        job.SnapshotRouting(
            null,
            "R-1",
            "Test routing",
            3,
            new[] { Step(1, "Cut", RoutingOperationType.Cutting), Step(2, "Edge", RoutingOperationType.Edging) });

        job.RoutingCodeSnapshot.Should().Be("R-1");
        job.RoutingSnapshotVersion.Should().Be(3);
        job.Steps.Should().HaveCount(2);
        job.Steps.Single(s => s.StepNumber == 1).InputQuantity.Should().Be(100m);
        job.Steps.Single(s => s.StepNumber == 2).InputQuantity.Should().Be(0m);
    }

    [Fact]
    public void Snapshotting_a_routing_without_steps_is_rejected()
    {
        var job = NewJob();

        var act = () => job.SnapshotRouting(null, "R-1", "Test routing", 1, Array.Empty<ProductionJobStepSnapshot>());

        act.Should().Throw<ProductionJobHasNoStepsException>();
    }

    [Fact]
    public void Releasing_a_job_without_steps_is_rejected()
    {
        var job = NewJob();

        var act = () => job.Release(WarehouseId, Now);

        act.Should().Throw<ProductionJobHasNoStepsException>();
        job.Status.Should().Be(ProductionJobStatus.Draft);
    }

    [Fact]
    public void Releasing_a_snapshotted_job_moves_it_to_released()
    {
        var job = ReleasedJob();

        job.Status.Should().Be(ProductionJobStatus.Released);
        job.WarehouseId.Should().Be(WarehouseId);
        job.ReleasedAtUtc.Should().Be(Now);
        job.CurrentStepNumber.Should().Be(1);
    }

    [Fact]
    public void Snapshotting_a_routing_after_release_is_rejected()
    {
        var job = ReleasedJob();

        var act = () =>
            job.SnapshotRouting(null, "R-2", "Other", 1, new[] { Step(1, "Cut", RoutingOperationType.Cutting) });

        act.Should().Throw<ProductionJobNotEditableException>();
        job.RoutingCodeSnapshot.Should().Be("R-1");
    }

    [Fact]
    public void Starting_a_step_moves_the_job_to_in_progress()
    {
        var job = ReleasedJob();

        job.StartStep(1, OperatorId, Now);

        job.Status.Should().Be(ProductionJobStatus.InProgress);
        job.StartedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Holding_and_resuming_a_released_job_returns_it_to_released()
    {
        var job = ReleasedJob();

        job.PutOnHold(Now);
        job.Status.Should().Be(ProductionJobStatus.OnHold);

        job.Resume(Now);
        job.Status.Should().Be(ProductionJobStatus.Released);
    }

    [Fact]
    public void Resuming_a_job_that_is_not_on_hold_is_rejected()
    {
        var job = ReleasedJob();

        var act = () => job.Resume(Now);

        act.Should().Throw<InvalidProductionJobTransitionException>();
        job.Status.Should().Be(ProductionJobStatus.Released);
    }

    [Fact]
    public void Cancelling_a_job_clears_the_current_step()
    {
        var job = ReleasedJob();

        job.Cancel("  customer withdrew  ", Now);

        job.Status.Should().Be(ProductionJobStatus.Cancelled);
        job.CancellationReason.Should().Be("customer withdrew");
        job.CancelledAtUtc.Should().Be(Now);
        job.CurrentStepNumber.Should().BeNull();
    }

    [Fact]
    public void Cancelling_an_already_cancelled_job_is_rejected()
    {
        var job = ReleasedJob();
        job.Cancel("first", Now);

        var act = () => job.Cancel("second", Now);

        act.Should().Throw<InvalidProductionJobTransitionException>();
        job.CancellationReason.Should().Be("first");
    }
}
