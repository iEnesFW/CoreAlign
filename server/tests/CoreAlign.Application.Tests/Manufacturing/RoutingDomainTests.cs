using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Manufacturing;

public class RoutingDomainTests
{
    private static RoutingStepDraft Draft(int stepNumber, string name = "Cut") =>
        new(stepNumber, Guid.NewGuid(), name, RoutingOperationType.Cutting, 5m, 2m, null, 0m, null, false);

    private static ProductionRouting NewRouting() => new("  TR-1  ", "  Temper hattı  ", "  notlar  ");

    [Fact]
    public void Constructor_trims_and_starts_in_draft()
    {
        var routing = NewRouting();

        routing.Code.Should().Be("TR-1");
        routing.Name.Should().Be("Temper hattı");
        routing.Description.Should().Be("notlar");
        routing.Status.Should().Be(RoutingStatus.Draft);
        routing.Steps.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "name")]
    [InlineData("  ", "name")]
    [InlineData("code", "")]
    public void Constructor_rejects_blank_code_or_name(string code, string name)
    {
        var act = () => new ProductionRouting(code, name);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReplaceSteps_accepts_gapless_sequence_and_orders()
    {
        var routing = NewRouting();

        routing.ReplaceSteps(new[] { Draft(2, "Edge"), Draft(1, "Cut"), Draft(3, "QC") });

        routing.Steps.Should().HaveCount(3);
        routing.Steps.Select(s => s.StepNumber).Should().ContainInOrder(1, 2, 3);
        routing.Steps.All(s => s.RoutingId == routing.Id).Should().BeTrue();
    }

    [Fact]
    public void ReplaceSteps_rejects_empty()
    {
        var routing = NewRouting();
        var act = () => routing.ReplaceSteps(Array.Empty<RoutingStepDraft>());
        act.Should().Throw<RoutingHasNoStepsException>();
    }

    [Fact]
    public void ReplaceSteps_rejects_duplicate_step_number()
    {
        var routing = NewRouting();
        var act = () => routing.ReplaceSteps(new[] { Draft(1), Draft(1) });
        act.Should().Throw<DuplicateRoutingStepException>();
    }

    [Fact]
    public void ReplaceSteps_rejects_gap()
    {
        var routing = NewRouting();
        var act = () => routing.ReplaceSteps(new[] { Draft(1), Draft(3) });
        act.Should().Throw<RoutingStepsNotSequentialException>();
    }

    [Fact]
    public void ReplaceSteps_rejects_when_not_draft()
    {
        var routing = NewRouting();
        routing.ReplaceSteps(new[] { Draft(1) });
        routing.Activate();

        var act = () => routing.ReplaceSteps(new[] { Draft(1), Draft(2) });
        act.Should().Throw<RoutingNotEditableException>();
    }

    [Fact]
    public void Activate_requires_at_least_one_step()
    {
        var routing = NewRouting();
        var act = () => routing.Activate();
        act.Should().Throw<RoutingHasNoStepsException>();
    }

    [Fact]
    public void Full_fsm_happy_path()
    {
        var routing = NewRouting();
        routing.ReplaceSteps(new[] { Draft(1) });

        routing.Activate();
        routing.Status.Should().Be(RoutingStatus.Active);

        routing.Archive();
        routing.Status.Should().Be(RoutingStatus.Archived);

        routing.RestoreToDraft();
        routing.Status.Should().Be(RoutingStatus.Draft);
    }

    [Fact]
    public void Draft_can_be_archived_directly()
    {
        var routing = NewRouting();
        routing.Archive();
        routing.Status.Should().Be(RoutingStatus.Archived);
    }

    [Fact]
    public void Active_cannot_return_to_draft()
    {
        var routing = NewRouting();
        routing.ReplaceSteps(new[] { Draft(1) });
        routing.Activate();

        var act = () => routing.RestoreToDraft();
        act.Should().Throw<InvalidRoutingTransitionException>();
    }

    [Fact]
    public void Archived_cannot_go_directly_active()
    {
        var routing = NewRouting();
        routing.Archive();

        var act = () => routing.Activate();
        act.Should().Throw<InvalidRoutingTransitionException>();
    }

    [Fact]
    public void UpdateHeader_rejected_when_archived()
    {
        var routing = NewRouting();
        routing.Archive();

        var act = () => routing.UpdateHeader("X", "Y", null);
        act.Should().Throw<RoutingNotEditableException>();
    }

    [Fact]
    public void UpdateHeader_allowed_when_active()
    {
        var routing = NewRouting();
        routing.ReplaceSteps(new[] { Draft(1) });
        routing.Activate();

        routing.UpdateHeader("TR-2", "Yeni ad", "d");
        routing.Code.Should().Be("TR-2");
        routing.Name.Should().Be("Yeni ad");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RoutingStep_rejects_non_positive_step_number(int stepNumber)
    {
        var act = () => new RoutingStep(Guid.NewGuid(), stepNumber, Guid.NewGuid(), "Cut",
            RoutingOperationType.Cutting, 0m, 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RoutingStep_rejects_empty_work_center()
    {
        var act = () => new RoutingStep(Guid.NewGuid(), 1, Guid.Empty, "Cut",
            RoutingOperationType.Cutting, 0m, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RoutingStep_rejects_scrap_out_of_range(decimal scrap)
    {
        var act = () => new RoutingStep(Guid.NewGuid(), 1, Guid.NewGuid(), "Cut",
            RoutingOperationType.Cutting, 0m, 0m, null, scrap);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RoutingStep_rejects_negative_times()
    {
        var setup = () => new RoutingStep(Guid.NewGuid(), 1, Guid.NewGuid(), "Cut",
            RoutingOperationType.Cutting, -1m, 0m);
        var run = () => new RoutingStep(Guid.NewGuid(), 1, Guid.NewGuid(), "Cut",
            RoutingOperationType.Cutting, 0m, -1m);
        setup.Should().Throw<ArgumentOutOfRangeException>();
        run.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RoutingStep_normalizes_operation_name_whitespace()
    {
        var step = new RoutingStep(Guid.NewGuid(), 1, Guid.NewGuid(), "  Kenar   Rodaj  ",
            RoutingOperationType.Edging, 0m, 0m);
        step.OperationName.Should().Be("Kenar Rodaj");
    }

    [Fact]
    public void WorkCenterOperator_deactivate_clears_primary()
    {
        var op = new WorkCenterOperator(Guid.NewGuid(), Guid.NewGuid(),
            OperatorQualificationLevel.Expert, isPrimary: true);
        op.IsPrimary.Should().BeTrue();

        op.Deactivate();

        op.IsActive.Should().BeFalse();
        op.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void WorkCenterOperator_update_cannot_be_primary_while_inactive()
    {
        var op = new WorkCenterOperator(Guid.NewGuid(), Guid.NewGuid(), OperatorQualificationLevel.Qualified);

        op.Update(OperatorQualificationLevel.Trainee, isPrimary: true, isActive: false, null, "note", null);

        op.IsActive.Should().BeFalse();
        op.IsPrimary.Should().BeFalse();
        op.Notes.Should().Be("note");
        op.QualificationLevel.Should().Be(OperatorQualificationLevel.Trainee);
    }

    [Fact]
    public void Product_assign_routing_does_not_touch_single_op_fields()
    {
        var product = new Product("SKU", "Cam", price: 100m);
        var workCenter = Guid.NewGuid();
        product.SetRouting(workCenter, 7m);
        var routingId = Guid.NewGuid();

        product.AssignRouting(routingId);

        product.RoutingId.Should().Be(routingId);
        product.WorkCenterId.Should().Be(workCenter);
        product.RunTimeMinutesPerUnit.Should().Be(7m);

        product.AssignRouting(null);
        product.RoutingId.Should().BeNull();
        product.WorkCenterId.Should().Be(workCenter);
    }
}
