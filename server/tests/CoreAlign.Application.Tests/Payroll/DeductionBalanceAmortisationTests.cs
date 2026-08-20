using CoreAlign.Application.Payroll.Runs;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Payroll;

// PayrollPayslipFactory caps an instalment at the deduction's RemainingBalance, but nothing ever
// reduced that balance — EmployeeDeduction.ReduceBalance had no caller. An advance of 5000 repaid
// at 500 a month was therefore withheld from the employee's net pay every month forever: the cap
// never bit because the balance it caps against never moved.
public class DeductionBalanceAmortisationTests
{
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private readonly IPayrollRunRepository _runs = Substitute.For<IPayrollRunRepository>();
    private readonly IPayslipRepository _payslips = Substitute.For<IPayslipRepository>();
    private readonly IEmployeeYtdTaxBaseRepository _ytd = Substitute.For<IEmployeeYtdTaxBaseRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PostPayrollRunHandler _sut;

    public DeductionBalanceAmortisationTests()
    {
        _employees.GetDeductionsByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EmployeeDeduction>());
        _sut = new PostPayrollRunHandler(_runs, _payslips, _ytd, _employees, _uow);
    }

    private static PayrollRun ApprovedRun()
    {
        var run = new PayrollRun("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = RunId };
        run.ApplyTotals(60000m, 8400m, 12300m, 600m, 1200m, 6484.30m, 258.02m, 15742.32m, 44257.68m, 73500m, 1);
        run.Calculate();
        run.Approve(Guid.NewGuid());
        return run;
    }

    private static Payslip PayslipWith(params PayslipDeductionLine[] deductionLines)
    {
        var payslip = new Payslip(
            "UCRET-2026-00001", RunId, EmployeeId, "PER-2026-00001", "Ada Yilmaz", "12345678901", 2026, 6, Guid.NewGuid());
        payslip.ApplyComputation(
            60000m, 60000m, 51000m, 150000m, 201000m, 132628.08m, 154732.76m,
            8400m, 600m, 9800m, 3315.70m, 197.38m, 0m, 6484.30m, 258.02m, 0m, 44257.68m, 12300m, 1200m, 73500m);
        foreach (var line in deductionLines)
        {
            payslip.AddDeductionLine(line);
        }
        return payslip;
    }

    private static EmployeeDeduction Advance(decimal instalment, decimal balance) =>
        new(DeductionType.Advance, new DateOnly(2026, 1, 1), amount: instalment, remainingBalance: balance)
        { Id = Guid.NewGuid() };

    private void Arrange(PayrollRun run, Payslip payslip, params EmployeeDeduction[] deductions)
    {
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        _payslips.GetByRunAsync(run.Id, Arg.Any<CancellationToken>()).Returns(new List<Payslip> { payslip });
        _employees.GetDeductionsByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(deductions);
    }

    [Fact]
    public async Task Posting_amortises_the_instalment_against_the_outstanding_balance()
    {
        var advance = Advance(instalment: 500m, balance: 5000m);
        var payslip = PayslipWith(new PayslipDeductionLine(DeductionType.Advance, 500m, false, advance.Id));
        Arrange(ApprovedRun(), payslip, advance);

        await _sut.Handle(new PostPayrollRunCommand(RunId), default);

        advance.RemainingBalance.Should().Be(4500m);
        advance.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task The_final_instalment_closes_the_deduction()
    {
        var advance = Advance(instalment: 500m, balance: 500m);
        var payslip = PayslipWith(new PayslipDeductionLine(DeductionType.Advance, 500m, false, advance.Id));
        Arrange(ApprovedRun(), payslip, advance);

        await _sut.Handle(new PostPayrollRunCommand(RunId), default);

        advance.RemainingBalance.Should().Be(0m);
        advance.IsActive.Should().BeFalse("a repaid advance must stop being withheld");
    }

    // A percentage deduction (union dues and the like) has no balance to amortise.
    [Fact]
    public async Task A_deduction_without_a_balance_is_left_alone()
    {
        var dues = new EmployeeDeduction(DeductionType.UnionDues, new DateOnly(2026, 1, 1), percent: 1m)
        { Id = Guid.NewGuid() };
        var payslip = PayslipWith(new PayslipDeductionLine(DeductionType.UnionDues, 600m, true, dues.Id));
        Arrange(ApprovedRun(), payslip, dues);

        await _sut.Handle(new PostPayrollRunCommand(RunId), default);

        dues.RemainingBalance.Should().Be(0m);
        dues.IsActive.Should().BeTrue("a recurring percentage deduction is not amortised away");
    }

    [Fact]
    public async Task A_legacy_line_without_a_source_link_touches_nothing()
    {
        var payslip = PayslipWith(new PayslipDeductionLine(DeductionType.Advance, 500m));
        Arrange(ApprovedRun(), payslip);

        await _sut.Handle(new PostPayrollRunCommand(RunId), default);

        await _employees.DidNotReceive().GetDeductionsByIdsAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Two_advances_of_the_same_type_are_amortised_separately()
    {
        var first = Advance(instalment: 300m, balance: 900m);
        var second = Advance(instalment: 200m, balance: 200m);
        var payslip = PayslipWith(
            new PayslipDeductionLine(DeductionType.Advance, 300m, false, first.Id),
            new PayslipDeductionLine(DeductionType.Advance, 200m, false, second.Id));
        Arrange(ApprovedRun(), payslip, first, second);

        await _sut.Handle(new PostPayrollRunCommand(RunId), default);

        first.RemainingBalance.Should().Be(600m);
        second.RemainingBalance.Should().Be(0m);
    }
}
