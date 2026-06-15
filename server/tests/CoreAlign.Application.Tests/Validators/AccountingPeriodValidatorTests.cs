using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.Validators;

namespace CoreAlign.Application.Tests.Validators;

public class AccountingPeriodValidatorTests
{
    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void CreatePeriod_rejects_out_of_range_year(int year)
    {
        var v = new CreateAccountingPeriodCommandValidator();
        var result = v.Validate(new CreateAccountingPeriodCommand(year, 6));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.YearOutOfRange");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void CreatePeriod_rejects_out_of_range_month(int month)
    {
        var v = new CreateAccountingPeriodCommandValidator();
        var result = v.Validate(new CreateAccountingPeriodCommand(2026, month));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.MonthOutOfRange");
    }

    [Fact]
    public void CreatePeriod_accepts_valid_year_and_month()
    {
        var v = new CreateAccountingPeriodCommandValidator();
        v.Validate(new CreateAccountingPeriodCommand(2026, 12)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ClosePeriod_rejects_empty_id()
    {
        var v = new ClosePeriodCommandValidator();
        v.Validate(new ClosePeriodCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ClosePeriod_rejects_oversized_notes()
    {
        var v = new ClosePeriodCommandValidator();
        v.Validate(new ClosePeriodCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 1001)))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReopenPeriod_rejects_empty_id()
    {
        var v = new ReopenPeriodCommandValidator();
        v.Validate(new ReopenPeriodCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void LockPeriod_rejects_empty_id()
    {
        var v = new LockPeriodCommandValidator();
        v.Validate(new LockPeriodCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
