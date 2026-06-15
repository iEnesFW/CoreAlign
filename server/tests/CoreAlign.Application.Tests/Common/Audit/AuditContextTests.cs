using CoreAlign.Application.Common.Audit;

namespace CoreAlign.Application.Tests.Common.Audit;

public class AuditContextTests
{
    private readonly IAuditFieldRedactor _redactor = Substitute.For<IAuditFieldRedactor>();

    public AuditContextTests()
    {
        _redactor.Redact(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(callInfo => callInfo.ArgAt<string?>(1));
    }

    [Fact]
    public void Capture_appends_field_update_entry()
    {
        var sut = new AuditContext(_redactor);
        var aggregateId = Guid.NewGuid();

        sut.Capture(aggregateId, "GlassProject", "Name", "old", "new");

        sut.PendingEntries.Should().HaveCount(1);
        var entry = sut.PendingEntries[0];
        entry.AggregateId.Should().Be(aggregateId);
        entry.AggregateType.Should().Be("GlassProject");
        entry.ChangeKind.Should().Be("FieldUpdate");
        entry.Field.Should().Be("Name");
        entry.OldValue.Should().Be("old");
        entry.NewValue.Should().Be("new");
    }

    [Fact]
    public void CaptureCustom_appends_custom_change_entry()
    {
        var sut = new AuditContext(_redactor);
        var aggregateId = Guid.NewGuid();

        sut.CaptureCustom(aggregateId, "GlassProject", "StatusTransition", "Draft->Active");

        sut.PendingEntries.Should().HaveCount(1);
        var entry = sut.PendingEntries[0];
        entry.AggregateId.Should().Be(aggregateId);
        entry.AggregateType.Should().Be("GlassProject");
        entry.ChangeKind.Should().Be("StatusTransition");
        entry.Field.Should().BeNull();
        entry.OldValue.Should().BeNull();
        entry.NewValue.Should().Be("Draft->Active");
    }

    [Fact]
    public void Clear_empties_pending_entries()
    {
        var sut = new AuditContext(_redactor);
        sut.Capture(Guid.NewGuid(), "GlassProject", "Name", "old", "new");
        sut.CaptureCustom(Guid.NewGuid(), "GlassProject", "StatusTransition", "Draft->Active");

        sut.Clear();

        sut.PendingEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Concurrent_capture_calls_record_every_entry()
    {
        var sut = new AuditContext(_redactor);
        var aggregateId = Guid.NewGuid();

        await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Task.Run(() =>
            sut.Capture(aggregateId, "GlassProject", $"Field{i}", "old", $"new-{i}"))));

        sut.PendingEntries.Should().HaveCount(10);
    }
}
