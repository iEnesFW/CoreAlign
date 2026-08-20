using CoreAlign.Application.B2B;
using CoreAlign.Application.Payroll.Calculation;
using CoreAlign.Application.Payroll.Runs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Payroll;

public static class PayrollRunTestData
{
    public static PayrollParameters SeededParameters()
    {
        var parameters = new PayrollParameters(
            effectiveYear: 2026,
            effectiveFrom: new DateOnly(2026, 1, 1),
            sgkEmployeeRate: 0.14m,
            sgkEmployerRate: 0.205m,
            sgkEmployer5PointIncentiveRate: 0.155m,
            unemploymentEmployeeRate: 0.01m,
            unemploymentEmployerRate: 0.02m,
            sgkFloorMonthly: 26005.50m,
            sgkCeilingMultiplier: 7.5m,
            sgkCeilingMonthly: 195041.25m,
            stampTaxRate: 0.00759m,
            grossMinimumWage: 26005.50m,
            disability1Amount: 0m,
            disability2Amount: 0m,
            disability3Amount: 0m,
            minWageExemptionEnabled: true)
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        parameters.AddTaxBracket(new PayrollTaxBracket(15m, 1, 158000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(20m, 2, 330000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(27m, 3, 1200000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(35m, 4, 4300000m));
        parameters.AddTaxBracket(new PayrollTaxBracket(40m, 5, null));
        return parameters;
    }

    public static Employee WorkedExampleEmployee() =>
        new("PER-2026-00001", "Ada", "Yilmaz", "12345678901", new DateOnly(2025, 1, 1), 60000m)
        {
            Id = Guid.NewGuid(),
        };
}

public sealed class CreatePayrollRunHandlerTests
{
    private readonly IPayrollRunRepository _runs = Substitute.For<IPayrollRunRepository>();
    private readonly IPayrollParametersRepository _parameters = Substitute.For<IPayrollParametersRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CreatePayrollRunHandler _sut;

    public CreatePayrollRunHandlerTests()
    {
        _sut = new CreatePayrollRunHandler(_runs, _parameters, _sequences, _uow);
        _parameters.ResolveAsync(Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(PayrollRunTestData.SeededParameters());
        _sequences.GetAsync(DocumentSequenceType.PayrollRunNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.PayrollRunNumber, "BORD", 2026, 1, 5));
    }

    [Fact]
    public async Task Create_assigns_BORD_number_and_starts_draft()
    {
        PayrollRun? captured = null;
        await _runs.AddAsync(Arg.Do<PayrollRun>(r => captured = r), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(new CreatePayrollRunCommand(2026, 6), default);

        captured.Should().NotBeNull();
        captured!.RunNumber.Should().Be("BORD-2026-00001");
        result.Status.Should().Be(PayrollRunStatus.Draft);
        result.PeriodYear.Should().Be(2026);
        result.PeriodMonth.Should().Be(6);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_rejects_duplicate_period()
    {
        _runs.ExistsForPeriodAsync(2026, 6, PayrollRunType.Regular, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _sut.Handle(new CreatePayrollRunCommand(2026, 6), default);

        await act.Should().ThrowAsync<DuplicatePayrollRunException>();
        await _runs.DidNotReceive().AddAsync(Arg.Any<PayrollRun>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_throws_when_no_parameters_resolve()
    {
        _parameters.ResolveAsync(Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((PayrollParameters?)null);

        var act = () => _sut.Handle(new CreatePayrollRunCommand(2026, 6), default);

        await act.Should().ThrowAsync<PayrollParametersNotResolvedException>();
    }
}

public sealed class CalculatePayrollRunHandlerTests
{
    private readonly IPayrollRunRepository _runs = Substitute.For<IPayrollRunRepository>();
    private readonly IPayslipRepository _payslips = Substitute.For<IPayslipRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeYtdTaxBaseRepository _ytd = Substitute.For<IEmployeeYtdTaxBaseRepository>();
    private readonly IPayrollParametersRepository _parameters = Substitute.For<IPayrollParametersRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CalculatePayrollRunHandler _sut;

    private readonly List<Payslip> _added = new();

    public CalculatePayrollRunHandlerTests()
    {
        _sut = new CalculatePayrollRunHandler(
            _runs, _payslips, _employees, _ytd, _parameters, new PayrollCalculationService(), _sequences, _uow);

        _parameters.ResolveAsync(Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(PayrollRunTestData.SeededParameters());
        _sequences.GetAsync(DocumentSequenceType.PayslipNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.PayslipNumber, "UCRET", 2026, 1, 5));

        var seq = 0;
        _sequences.ConsumeAsync(DocumentSequenceType.PayslipNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ => $"UCRET-2026-{(++seq):D5}");

        _payslips.When(p => p.AddAsync(Arg.Any<Payslip>(), Arg.Any<CancellationToken>()))
            .Do(ci => _added.Add(ci.Arg<Payslip>()));
    }

    private static PayrollRun DraftRun() =>
        new("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = Guid.NewGuid() };

    [Fact]
    public async Task Calculate_produces_one_payslip_with_worked_example_net()
    {
        var run = DraftRun();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var employee = PayrollRunTestData.WorkedExampleEmployee();
        _employees.GetActiveForPayrollAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { employee });

        var prior = new EmployeeYtdTaxBase(employee.Id, 2026);
        prior.Accumulate(150000m, 132628.08m, 5);
        _ytd.GetByYearAsync(2026, Arg.Any<CancellationToken>())
            .Returns(new List<EmployeeYtdTaxBase> { prior });

        var result = await _sut.Handle(new CalculatePayrollRunCommand(run.Id), default);

        result.Status.Should().Be(PayrollRunStatus.Calculated);
        result.PayslipCount.Should().Be(1);

        _added.Should().ContainSingle();
        var payslip = _added.Single();
        payslip.GrossEarnings.Should().Be(60000.00m);
        payslip.SgkEmployee.Should().Be(8400.00m);
        payslip.IncomeTaxNet.Should().Be(6484.30m);
        payslip.StampTax.Should().Be(258.02m);
        payslip.NetPay.Should().Be(44257.68m);
        payslip.EmployerCost.Should().Be(73500.00m);
        payslip.CumulativeIncomeTaxBaseBefore.Should().Be(150000.00m);
        payslip.CumulativeIncomeTaxBaseAfter.Should().Be(201000.00m);
        payslip.PayslipNumber.Should().Be("UCRET-2026-00001");

        result.TotalNet.Should().Be(44257.68m);
        result.TotalGross.Should().Be(60000.00m);
        result.TotalEmployerCost.Should().Be(73500.00m);
    }

    [Fact]
    public async Task Calculate_treats_missing_ytd_as_mid_year_hire_with_zero_priors()
    {
        var run = DraftRun();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var employee = PayrollRunTestData.WorkedExampleEmployee();
        _employees.GetActiveForPayrollAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { employee });
        _ytd.GetByYearAsync(2026, Arg.Any<CancellationToken>())
            .Returns(new List<EmployeeYtdTaxBase>());

        await _sut.Handle(new CalculatePayrollRunCommand(run.Id), default);

        var payslip = _added.Single();
        payslip.CumulativeIncomeTaxBaseBefore.Should().Be(0m);
        payslip.CumulativeIncomeTaxBaseAfter.Should().Be(51000.00m);
    }

    [Fact]
    public async Task Calculate_pins_resolved_parameters_and_clears_old_payslips()
    {
        var run = DraftRun();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        _employees.GetActiveForPayrollAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee>());
        _ytd.GetByYearAsync(2026, Arg.Any<CancellationToken>())
            .Returns(new List<EmployeeYtdTaxBase>());

        var result = await _sut.Handle(new CalculatePayrollRunCommand(run.Id), default);

        await _payslips.Received(1).RemoveByRunAsync(run.Id, Arg.Any<CancellationToken>());
        result.PayslipCount.Should().Be(0);
        result.ParametersId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Calculate_does_not_advance_ytd()
    {
        var run = DraftRun();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var employee = PayrollRunTestData.WorkedExampleEmployee();
        _employees.GetActiveForPayrollAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { employee });
        _ytd.GetByYearAsync(2026, Arg.Any<CancellationToken>())
            .Returns(new List<EmployeeYtdTaxBase>());

        await _sut.Handle(new CalculatePayrollRunCommand(run.Id), default);

        await _ytd.DidNotReceive().AddAsync(Arg.Any<EmployeeYtdTaxBase>(), Arg.Any<CancellationToken>());
        _ytd.DidNotReceive().Update(Arg.Any<EmployeeYtdTaxBase>());
    }

    [Fact]
    public async Task Calculate_aggregates_taxable_recurring_components_into_gross()
    {
        var run = DraftRun();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var employee = PayrollRunTestData.WorkedExampleEmployee();
        employee.AddSalaryComponent(new SalaryComponent(
            SalaryComponentType.Bonus, 5000m, new DateOnly(2026, 1, 1), isRecurring: true, taxExempt: false));
        employee.AddSalaryComponent(new SalaryComponent(
            SalaryComponentType.Meal, 1000m, new DateOnly(2026, 1, 1), isRecurring: true, taxExempt: true));
        _employees.GetActiveForPayrollAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { employee });
        _ytd.GetByYearAsync(2026, Arg.Any<CancellationToken>())
            .Returns(new List<EmployeeYtdTaxBase>());

        await _sut.Handle(new CalculatePayrollRunCommand(run.Id), default);

        var payslip = _added.Single();
        payslip.GrossEarnings.Should().Be(65000.00m);
        payslip.EarningLines.Should().HaveCount(3);
    }
}

public sealed class PayrollRunFsmTests
{
    private static PayrollRun DraftRun() =>
        new("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = Guid.NewGuid() };

    [Fact]
    public void Approve_from_draft_is_illegal()
    {
        var run = DraftRun();
        var act = () => run.Approve(Guid.NewGuid());
        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void Calculated_then_approved_then_posted_is_legal()
    {
        var run = DraftRun();
        run.Calculate();
        run.Status.Should().Be(PayrollRunStatus.Calculated);
        run.Approve(Guid.NewGuid());
        run.Status.Should().Be(PayrollRunStatus.Approved);
        run.MarkPosted();
        run.Status.Should().Be(PayrollRunStatus.Posted);
    }

    [Fact]
    public void Post_from_calculated_is_illegal_without_approval()
    {
        var run = DraftRun();
        run.Calculate();
        var act = () => run.MarkPosted();
        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void Reopen_from_calculated_returns_to_draft()
    {
        var run = DraftRun();
        run.Calculate();
        run.Reopen();
        run.Status.Should().Be(PayrollRunStatus.Draft);
        run.CalculatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Posted_run_raises_posted_event()
    {
        var run = DraftRun();
        run.Calculate();
        run.Approve(Guid.NewGuid());
        run.MarkPosted();
        run.DomainEvents.Should().ContainSingle(e => e is CoreAlign.Domain.Events.PayrollRunPostedEvent);
    }
}

public sealed class ReopenPayrollRunHandlerTests
{
    private readonly IPayrollRunRepository _runs = Substitute.For<IPayrollRunRepository>();
    private readonly IPayslipRepository _payslips = Substitute.For<IPayslipRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ReopenPayrollRunHandler _sut;

    public ReopenPayrollRunHandlerTests()
    {
        _sut = new ReopenPayrollRunHandler(_runs, _payslips, _uow);
    }

    private static PayrollRun CalculatedRun()
    {
        var run = new PayrollRun("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = Guid.NewGuid() };
        run.ApplyTotals(60000m, 8400m, 12300m, 600m, 1200m, 6484.30m, 258.02m, 15742.32m, 44257.68m, 73500m, 1);
        run.Calculate();
        return run;
    }

    [Fact]
    public async Task Reopen_clears_payslips_and_resets_totals()
    {
        var run = CalculatedRun();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _sut.Handle(new ReopenPayrollRunCommand(run.Id), default);

        await _payslips.Received(1).RemoveByRunAsync(run.Id, Arg.Any<CancellationToken>());
        result.Status.Should().Be(PayrollRunStatus.Draft);
        result.PayslipCount.Should().Be(0);
        result.TotalNet.Should().Be(0m);
    }

    [Fact]
    public async Task Reopen_rejects_posted_run()
    {
        var run = CalculatedRun();
        run.Approve(Guid.NewGuid());
        run.MarkPosted();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var act = () => _sut.Handle(new ReopenPayrollRunCommand(run.Id), default);

        await act.Should().ThrowAsync<PayrollRunReopenNotAllowedException>();
        await _payslips.DidNotReceive().RemoveByRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

public sealed class PayPayrollRunHandlerTests
{
    private readonly IPayrollRunRepository _runs = Substitute.For<IPayrollRunRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PayPayrollRunHandler _sut;

    public PayPayrollRunHandlerTests()
    {
        _sut = new PayPayrollRunHandler(_runs, _uow);
    }

    private static PayrollRun PostedRun()
    {
        var run = new PayrollRun("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = Guid.NewGuid() };
        run.ApplyTotals(60000m, 8400m, 12300m, 600m, 1200m, 6484.30m, 258.02m, 15742.32m, 44257.68m, 73500m, 1);
        run.Calculate();
        run.Approve(Guid.NewGuid());
        run.MarkPosted();
        return run;
    }

    [Fact]
    public async Task Pay_from_posted_marks_paid_and_raises_paid_event()
    {
        var run = PostedRun();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _sut.Handle(new PayPayrollRunCommand(run.Id), default);

        result.Status.Should().Be(PayrollRunStatus.Paid);
        result.PaidAtUtc.Should().NotBeNull();
        run.DomainEvents.Should().ContainSingle(e => e is CoreAlign.Domain.Events.PayrollRunPaidEvent);
        _runs.Received(1).Update(run);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pay_from_approved_is_illegal()
    {
        var run = new PayrollRun("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = Guid.NewGuid() };
        run.ApplyTotals(60000m, 8400m, 12300m, 600m, 1200m, 6484.30m, 258.02m, 15742.32m, 44257.68m, 73500m, 1);
        run.Calculate();
        run.Approve(Guid.NewGuid());
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var act = () => _sut.Handle(new PayPayrollRunCommand(run.Id), default);

        await act.Should().ThrowAsync<InvalidOrderStatusTransitionException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class PostPayrollRunHandlerTests
{
    private readonly IPayrollRunRepository _runs = Substitute.For<IPayrollRunRepository>();
    private readonly IPayslipRepository _payslips = Substitute.For<IPayslipRepository>();
    private readonly IEmployeeYtdTaxBaseRepository _ytd = Substitute.For<IEmployeeYtdTaxBaseRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PostPayrollRunHandler _sut;

    public PostPayrollRunHandlerTests()
    {
        _sut = new PostPayrollRunHandler(_runs, _payslips, _ytd, _uow);
    }

    private static PayrollRun ApprovedRun()
    {
        var run = new PayrollRun("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = Guid.NewGuid() };
        run.ApplyTotals(60000m, 8400m, 12300m, 600m, 1200m, 6484.30m, 258.02m, 15742.32m, 44257.68m, 73500m, 1);
        run.Calculate();
        run.Approve(Guid.NewGuid());
        return run;
    }

    private static Payslip PayslipFor(Guid runId, Guid employeeId)
    {
        var payslip = new Payslip(
            "UCRET-2026-00001", runId, employeeId, "PER-2026-00001", "Ada Yilmaz", "12345678901", 2026, 6, Guid.NewGuid());
        payslip.ApplyComputation(
            60000m, 60000m, 51000m, 150000m, 201000m, 132628.08m, 154732.76m,
            8400m, 600m, 9800m, 3315.70m, 197.38m, 0m, 6484.30m, 258.02m, 0m, 44257.68m, 12300m, 1200m, 73500m);
        return payslip;
    }

    [Fact]
    public async Task Post_advances_ytd_to_cumulative_after()
    {
        var run = ApprovedRun();
        var employeeId = Guid.NewGuid();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        _payslips.GetByRunAsync(run.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Payslip> { PayslipFor(run.Id, employeeId) });
        _ytd.GetAsync(employeeId, 2026, Arg.Any<CancellationToken>()).Returns((EmployeeYtdTaxBase?)null);

        EmployeeYtdTaxBase? captured = null;
        await _ytd.AddAsync(Arg.Do<EmployeeYtdTaxBase>(y => captured = y), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(new PostPayrollRunCommand(run.Id), default);

        result.Status.Should().Be(PayrollRunStatus.Posted);
        captured.Should().NotBeNull();
        captured!.CumulativeIncomeTaxBase.Should().Be(51000.00m);
        captured.CumulativeMinWageBase.Should().Be(22104.68m);
        captured.LastPeriodMonth.Should().Be(6);
    }

    [Fact]
    public async Task Post_is_idempotent_when_ytd_already_at_or_past_period()
    {
        var run = ApprovedRun();
        var employeeId = Guid.NewGuid();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        _payslips.GetByRunAsync(run.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Payslip> { PayslipFor(run.Id, employeeId) });

        var existing = new EmployeeYtdTaxBase(employeeId, 2026);
        existing.Accumulate(51000m, 22104.68m, 6);
        _ytd.GetAsync(employeeId, 2026, Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.Handle(new PostPayrollRunCommand(run.Id), default);

        existing.CumulativeIncomeTaxBase.Should().Be(51000.00m);
        _ytd.DidNotReceive().Update(Arg.Any<EmployeeYtdTaxBase>());
        await _ytd.DidNotReceive().AddAsync(Arg.Any<EmployeeYtdTaxBase>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_advances_existing_ytd_when_prior_month_is_earlier()
    {
        var run = ApprovedRun();
        var employeeId = Guid.NewGuid();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        _payslips.GetByRunAsync(run.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Payslip> { PayslipFor(run.Id, employeeId) });

        var existing = new EmployeeYtdTaxBase(employeeId, 2026);
        existing.Accumulate(150000m, 132628.08m, 5);
        _ytd.GetAsync(employeeId, 2026, Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.Handle(new PostPayrollRunCommand(run.Id), default);

        existing.CumulativeIncomeTaxBase.Should().Be(201000.00m);
        existing.LastPeriodMonth.Should().Be(6);
        _ytd.Received(1).Update(existing);
    }

    // Posting an earlier month after a later one walks the tax brackets in the wrong order.
    [Fact]
    public async Task Post_of_a_month_earlier_than_the_last_posted_one_is_rejected()
    {
        var run = ApprovedRun();
        var employeeId = Guid.NewGuid();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        _payslips.GetByRunAsync(run.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Payslip> { PayslipFor(run.Id, employeeId) });

        var existing = new EmployeeYtdTaxBase(employeeId, 2026);
        existing.Accumulate(120000m, 110000m, 7);
        _ytd.GetAsync(employeeId, 2026, Arg.Any<CancellationToken>()).Returns(existing);

        var act = () => _sut.Handle(new PostPayrollRunCommand(run.Id), default);

        await act.Should().ThrowAsync<PayrollOutOfSequencePostException>();
        existing.CumulativeIncomeTaxBase.Should().Be(120000m);
        _ytd.DidNotReceive().Update(Arg.Any<EmployeeYtdTaxBase>());
    }

    // A gap in ONE employee's history (rehired mid-year, or left out of a month's run) is
    // legitimate. Refusing it blocked the whole company's payroll for that month.
    [Fact]
    public async Task A_gap_in_one_employees_history_does_not_block_the_run()
    {
        var run = ApprovedRun();
        var employeeId = Guid.NewGuid();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        _payslips.GetByRunAsync(run.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Payslip> { PayslipFor(run.Id, employeeId) });

        var existing = new EmployeeYtdTaxBase(employeeId, 2026);
        existing.Accumulate(150000m, 132628.08m, 4);
        _ytd.GetAsync(employeeId, 2026, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.Handle(new PostPayrollRunCommand(run.Id), default);

        result.Status.Should().Be(PayrollRunStatus.Posted);
        existing.LastPeriodMonth.Should().Be(6);
        existing.CumulativeIncomeTaxBase.Should().Be(201000.00m);
        _ytd.Received(1).Update(existing);
    }

    [Fact]
    public async Task Post_from_draft_is_illegal()
    {
        var run = new PayrollRun("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = Guid.NewGuid() };
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var act = () => _sut.Handle(new PostPayrollRunCommand(run.Id), default);

        await act.Should().ThrowAsync<InvalidOrderStatusTransitionException>();
    }
}

// Turkish statutory payroll is TRY by construction: the minimum wage, the SGK ceiling and the
// income-tax brackets are lira amounts, and the accrual GL posting carries no exchange rate — so
// a EUR run computed meaningless tax and booked euros into the ledger as if they were lira.
public sealed class PayrollRunCurrencyValidatorTests
{
    private readonly CreatePayrollRunCommandValidator _sut = new();

    [Theory]
    [InlineData("TRY")]
    [InlineData("try")]
    [InlineData(" TRY ")]
    public void Try_is_accepted(string currency)
    {
        _sut.Validate(new CreatePayrollRunCommand(2026, 6, Currency: currency)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("XXX")]
    public void Any_other_currency_is_refused(string currency)
    {
        var result = _sut.Validate(new CreatePayrollRunCommand(2026, 6, Currency: currency));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.PayrollCurrencyMustBeTry");
    }
}
