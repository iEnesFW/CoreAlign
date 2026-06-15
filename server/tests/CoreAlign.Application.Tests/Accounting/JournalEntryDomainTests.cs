using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Accounting;

public class JournalEntryDomainTests
{
    private static JournalEntry NewEntry() => new(
        "YEV-2026-00001",
        new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        JournalEntryType.Mahsup,
        "Test entry",
        "REF-1");

    private static void AddBalanced(JournalEntry e, decimal amount = 100m)
    {
        e.AddLine(Guid.NewGuid(), "120", "Müşteriler", amount, 0m);
        e.AddLine(Guid.NewGuid(), "600", "Yurtiçi satışlar", 0m, amount);
    }

    [Fact]
    public void Constructor_normalises_dates_to_utc_and_trims_strings()
    {
        var e = new JournalEntry(
            "  YEV-1  ",
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            JournalEntryType.Tahsil,
            "  hello  ",
            "  ref  ");

        e.Number.Should().Be("YEV-1");
        e.Description.Should().Be("hello");
        e.Reference.Should().Be("ref");
        e.EntryDate.Kind.Should().Be(DateTimeKind.Utc);
        e.PostingDate.Kind.Should().Be(DateTimeKind.Utc);
        e.Status.Should().Be(JournalEntryStatus.Draft);
    }

    [Fact]
    public void Constructor_throws_when_number_is_blank()
    {
        var act = () => new JournalEntry(
            "   ", DateTime.UtcNow, DateTime.UtcNow, JournalEntryType.Mahsup);
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("number");
    }

    [Fact]
    public void AddLine_recalculates_totals_and_assigns_sequential_line_numbers()
    {
        var e = NewEntry();
        AddBalanced(e, 250m);

        e.TotalDebit.Should().Be(250m);
        e.TotalCredit.Should().Be(250m);
        e.Lines.Should().HaveCount(2);
        e.Lines.Select(l => l.LineNumber).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void Post_throws_when_fewer_than_two_lines()
    {
        var e = NewEntry();
        e.AddLine(Guid.NewGuid(), "120", "Müşteriler", 100m, 0m);

        var act = () => e.Post(Guid.NewGuid());
        act.Should().Throw<JournalEntryEmptyException>();
    }

    [Fact]
    public void Post_throws_when_debit_does_not_equal_credit()
    {
        var e = NewEntry();
        e.AddLine(Guid.NewGuid(), "120", "Müşteriler", 100m, 0m);
        e.AddLine(Guid.NewGuid(), "600", "Yurtiçi satışlar", 0m, 99m);

        var act = () => e.Post(Guid.NewGuid());
        act.Should().Throw<JournalEntryNotBalancedException>();
    }

    [Fact]
    public void Post_succeeds_when_balanced_and_sets_audit_columns()
    {
        var e = NewEntry();
        AddBalanced(e, 100m);
        var user = Guid.NewGuid();

        e.Post(user);

        e.Status.Should().Be(JournalEntryStatus.Posted);
        e.PostedAtUtc.Should().NotBeNull();
        e.PostedByUserId.Should().Be(user);
    }

    [Fact]
    public void Post_tolerates_micro_rounding_drift_below_rounded_4th_decimal()
    {
        var e = NewEntry();
        e.AddLine(Guid.NewGuid(), "120", "Müşteriler", 100.000001m, 0m);
        e.AddLine(Guid.NewGuid(), "600", "Yurtiçi satışlar", 0m, 100.000002m);

        var act = () => e.Post(Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Fact]
    public void Post_twice_throws_status_transition()
    {
        var e = NewEntry();
        AddBalanced(e);
        e.Post(Guid.NewGuid());

        var act = () => e.Post(Guid.NewGuid());
        act.Should().Throw<JournalEntryStatusTransitionException>();
    }

    [Fact]
    public void AddLine_throws_when_entry_already_posted()
    {
        var e = NewEntry();
        AddBalanced(e);
        e.Post(Guid.NewGuid());

        var act = () => e.AddLine(Guid.NewGuid(), "770", "Genel yön.", 1m, 0m);
        act.Should().Throw<JournalEntryStatusTransitionException>();
    }

    [Fact]
    public void RemoveLine_renumbers_remaining_lines()
    {
        var e = NewEntry();
        e.AddLine(Guid.NewGuid(), "120", "Müşteriler", 100m, 0m);
        e.AddLine(Guid.NewGuid(), "600", "Yurtiçi satışlar", 0m, 50m);
        var third = e.AddLine(Guid.NewGuid(), "391", "Hesaplanan KDV", 0m, 50m);

        e.RemoveLine(e.Lines.First().Id);

        e.Lines.Should().HaveCount(2);
        e.Lines.Select(l => l.LineNumber).Should().BeEquivalentTo(new[] { 1, 2 });
        e.Lines.Should().Contain(l => l.Id == third.Id);
    }

    [Fact]
    public void RemoveLine_throws_when_line_does_not_belong_to_entry()
    {
        var e = NewEntry();
        AddBalanced(e);

        var act = () => e.RemoveLine(Guid.NewGuid());
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void MarkReversed_only_allowed_from_posted()
    {
        var e = NewEntry();
        AddBalanced(e);
        var act = () => e.MarkReversed(Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<JournalEntryStatusTransitionException>();

        e.Post(Guid.NewGuid());
        e.MarkReversed(Guid.NewGuid(), Guid.NewGuid());
        e.Status.Should().Be(JournalEntryStatus.Reversed);

        var twice = () => e.MarkReversed(Guid.NewGuid(), Guid.NewGuid());
        twice.Should().Throw<JournalEntryStatusTransitionException>();
    }

    [Fact]
    public void UpdateHeader_blocked_on_posted_entry()
    {
        var e = NewEntry();
        AddBalanced(e);
        e.Post(Guid.NewGuid());

        var act = () => e.UpdateHeader(DateTime.UtcNow, DateTime.UtcNow, JournalEntryType.Tediye, "x", "y");
        act.Should().Throw<JournalEntryStatusTransitionException>();
    }

    [Fact]
    public void AssignSource_treats_empty_guid_and_blank_number_as_null()
    {
        var e = NewEntry();
        e.AssignSource(JournalSourceType.SalesInvoice, Guid.Empty, "   ");

        e.SourceType.Should().Be(JournalSourceType.SalesInvoice);
        e.SourceDocumentId.Should().BeNull();
        e.SourceDocumentNumber.Should().BeNull();
    }
}
