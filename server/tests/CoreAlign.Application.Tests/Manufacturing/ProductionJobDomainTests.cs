using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CoreAlign.Application.Tests.Manufacturing;

public class ProductionJobDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_InitializesCorrectly()
    {
        var job = ProductionJob.Create(TenantId, "JOB-100", ProductId, 100, "PCS", null, null, null);

        job.Status.Should().Be(ProductionJobStatus.Draft);
        job.JobNumber.Should().Be("JOB-100");
        job.PlannedQuantity.Should().Be(100);
        job.Steps.Should().BeEmpty();
    }

    [Fact]
    public void SnapshotRouting_WhenDraft_SnapshotsCorrectly()
    {
        var job = ProductionJob.Create(TenantId, "JOB-100", ProductId, 100, "PCS", null, null, null);
        var routing = ProductionRouting.Create(TenantId, "R-1", "Test", 1, null);
        routing.AddStep(null, "Op1", RoutingOperationType.Assembly, false, 10, 5, null, 0, "");
        
        job.SnapshotRouting(routing);

        job.RoutingCodeSnapshot.Should().Be("R-1");
        job.Steps.Should().HaveCount(1);
        job.Steps[0].StepNumber.Should().Be(10);
        job.Steps[0].Status.Should().Be(ProductionJobStepStatus.Pending);
    }

    [Fact]
    public void Release_WhenDraft_SetsStatusToReleased()
    {
        var job = ProductionJob.Create(TenantId, "JOB-100", ProductId, 100, "PCS", null, null, null);
        
        job.Release(Guid.NewGuid());

        job.Status.Should().Be(ProductionJobStatus.Released);
    }

    [Fact]
    public void StartStep_ValidStep_UpdatesStatusAndCurrentStep()
    {
        var job = ProductionJob.Create(TenantId, "JOB-100", ProductId, 100, "PCS", null, null, null);
        var routing = ProductionRouting.Create(TenantId, "R-1", "Test", 1, null);
        routing.AddStep(null, "Op1", RoutingOperationType.Assembly, false, 10, 5, null, 0, "");
        job.SnapshotRouting(routing);
        job.Release(Guid.NewGuid());

        job.StartStep(10, Guid.NewGuid());

        job.Status.Should().Be(ProductionJobStatus.InProgress);
        job.CurrentStepNumber.Should().Be(10);
        job.Steps[0].Status.Should().Be(ProductionJobStepStatus.InProgress);
        job.Steps[0].StartedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void FinishStep_AllStepsCompleted_SetsJobToReadyToComplete()
    {
        var job = ProductionJob.Create(TenantId, "JOB-100", ProductId, 100, "PCS", null, null, null);
        var routing = ProductionRouting.Create(TenantId, "R-1", "Test", 1, null);
        routing.AddStep(null, "Op1", RoutingOperationType.Assembly, false, 10, 5, null, 0, "");
        job.SnapshotRouting(routing);
        job.Release(Guid.NewGuid());
        job.StartStep(10, Guid.NewGuid());

        job.FinishStep(10, 100, 0, null, 10, 60, Guid.NewGuid());

        job.Steps[0].Status.Should().Be(ProductionJobStepStatus.Completed);
        job.Status.Should().Be(ProductionJobStatus.ReadyToComplete);
    }

    [Fact]
    public void Complete_WhenReady_CompletesJob()
    {
        var job = ProductionJob.Create(TenantId, "JOB-100", ProductId, 100, "PCS", null, null, null);
        var routing = ProductionRouting.Create(TenantId, "R-1", "Test", 1, null);
        routing.AddStep(null, "Op1", RoutingOperationType.Assembly, false, 10, 5, null, 0, "");
        job.SnapshotRouting(routing);
        job.Release(Guid.NewGuid());
        job.StartStep(10, Guid.NewGuid());
        job.FinishStep(10, 100, 0, null, 10, 60, Guid.NewGuid());

        job.Complete(100, Guid.NewGuid());

        job.Status.Should().Be(ProductionJobStatus.Completed);
    }
}
