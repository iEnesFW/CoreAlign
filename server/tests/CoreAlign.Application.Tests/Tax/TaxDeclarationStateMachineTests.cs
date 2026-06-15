using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Tax;

public class TaxDeclarationStateMachineTests
{
    [Fact]
    public void Cannot_submit_draft_declaration()
    {
        var declaration = new TaxDeclaration(2026, 5, TaxDeclarationType.Kdv1);

        Action act = () => declaration.MarkSubmitted();

        act.Should().Throw<TaxDeclarationInvalidStateException>();
    }

    [Fact]
    public void Cannot_submit_already_submitted_declaration()
    {
        var declaration = BuildSubmittedDeclaration();

        Action act = () => declaration.MarkSubmitted();

        act.Should().Throw<TaxDeclarationInvalidStateException>();
    }

    [Fact]
    public void Cannot_accept_generated_declaration_without_submitting()
    {
        var declaration = BuildGeneratedDeclaration();

        Action act = () => declaration.MarkAccepted();

        act.Should().Throw<TaxDeclarationInvalidStateException>();
    }

    [Fact]
    public void Mark_rejected_requires_reason()
    {
        var declaration = BuildSubmittedDeclaration();

        Action act = () => declaration.MarkRejected(string.Empty);

        act.Should().Throw<TaxDeclarationRejectionReasonRequiredException>();
    }

    [Fact]
    public void Mark_rejected_truncates_long_reason_to_500_chars()
    {
        var declaration = BuildSubmittedDeclaration();
        var longReason = new string('x', 1000);

        declaration.MarkRejected(longReason);

        declaration.Status.Should().Be(TaxDeclarationStatus.Rejected);
        declaration.FailureReason!.Length.Should().Be(500);
    }

    [Fact]
    public void Generated_to_submitted_to_accepted_succeeds()
    {
        var declaration = BuildGeneratedDeclaration();

        declaration.MarkSubmitted(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        declaration.MarkAccepted();

        declaration.Status.Should().Be(TaxDeclarationStatus.Accepted);
        declaration.SubmittedAtUtc.Should().NotBeNull();
        declaration.AcceptedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_rejects_invalid_year()
    {
        Action act = () => new TaxDeclaration(1999, 1, TaxDeclarationType.Kdv1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_rejects_invalid_month()
    {
        Action act = () => new TaxDeclaration(2026, 13, TaxDeclarationType.Kdv1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static TaxDeclaration BuildGeneratedDeclaration()
    {
        var d = new TaxDeclaration(2026, 5, TaxDeclarationType.Kdv1);
        d.Generate("<Beyanname/>", 1000m, 200m, 0m, 1);
        return d;
    }

    private static TaxDeclaration BuildSubmittedDeclaration()
    {
        var d = BuildGeneratedDeclaration();
        d.MarkSubmitted();
        return d;
    }
}
