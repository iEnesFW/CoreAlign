using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Application.Payroll.Calculation;
using CoreAlign.Application.Payroll.Employees;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Payroll.Runs;

internal static class PayrollRunMapper
{
    public static PayrollRunDetailDto ToDetailDto(PayrollRun r) => new(
        r.Id, r.RunNumber, r.PeriodYear, r.PeriodMonth, r.RunType, r.Status, r.Currency, r.ParametersId,
        r.TotalGross, r.TotalSgkEmployee, r.TotalSgkEmployer, r.TotalUnemploymentEmployee, r.TotalUnemploymentEmployer,
        r.TotalIncomeTax, r.TotalStampTax, r.TotalDeductions, r.TotalNet, r.TotalEmployerCost, r.PayslipCount,
        r.CalculatedAtUtc, r.ApprovedByUserId, r.ApprovedAtUtc, r.PostedAtUtc, r.PaidAtUtc, r.CreatedAtUtc);

    public static PayrollRunListItemDto ToListItemDto(PayrollRun r) => new(
        r.Id, r.RunNumber, r.PeriodYear, r.PeriodMonth, r.RunType, r.Status, r.Currency,
        r.TotalGross, r.TotalNet, r.TotalEmployerCost, r.PayslipCount,
        r.CalculatedAtUtc, r.ApprovedAtUtc, r.PostedAtUtc, r.PaidAtUtc, r.CreatedAtUtc);

    public static PayslipDto ToDto(Payslip p) => new(
        p.Id, p.PayslipNumber, p.RunId, p.EmployeeId, p.EmployeeNumber, p.EmployeeFullName,
        PiiMasking.MaskNationalId(p.NationalId), p.PeriodYear, p.PeriodMonth, p.DaysWorked, p.ParametersId,
        p.GrossEarnings, p.SgkBase, p.IncomeTaxBaseThisPeriod, p.CumulativeIncomeTaxBaseBefore, p.CumulativeIncomeTaxBaseAfter,
        p.CumulativeMinWageBaseBefore, p.CumulativeMinWageBaseAfter, p.SgkEmployee, p.UnemploymentEmployee,
        p.IncomeTaxGross, p.MinWageIncomeTaxExemptionApplied, p.MinWageStampTaxExemptionApplied, p.DisabilityExemptionApplied,
        p.IncomeTaxNet, p.StampTax, p.OtherDeductionsTotal, p.NetPay, p.SgkEmployer, p.UnemploymentEmployer, p.EmployerCost,
        p.EarningLines.Select(l => new PayslipEarningLineDto(l.Id, l.ComponentType, l.Amount, l.TaxExempt, l.SgkExempt)).ToList(),
        p.DeductionLines.Select(l => new PayslipDeductionLineDto(l.Id, l.DeductionType, l.Amount, l.IsRecurring)).ToList());
}

public class CreatePayrollRunHandler : IRequestHandler<CreatePayrollRunCommand, PayrollRunDetailDto>
{
    private readonly IPayrollRunRepository _runs;
    private readonly IPayrollParametersRepository _parameters;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IUnitOfWork _uow;

    public CreatePayrollRunHandler(
        IPayrollRunRepository runs,
        IPayrollParametersRepository parameters,
        IDocumentSequenceRepository sequences,
        IUnitOfWork uow)
    {
        _runs = runs;
        _parameters = parameters;
        _sequences = sequences;
        _uow = uow;
    }

    public async Task<PayrollRunDetailDto> Handle(CreatePayrollRunCommand c, CancellationToken ct)
    {
        if (await _runs.ExistsForPeriodAsync(c.PeriodYear, c.PeriodMonth, c.RunType, ct))
        {
            throw new DuplicatePayrollRunException(c.PeriodYear, c.PeriodMonth);
        }

        var period = new DateOnly(c.PeriodYear, c.PeriodMonth, 1);
        var parameters = await _parameters.ResolveAsync(c.PeriodYear, period, ct)
            ?? throw new PayrollParametersNotResolvedException(c.PeriodYear, c.PeriodMonth);

        var now = DateTime.UtcNow;
        var seq = await _sequences.GetAsync(DocumentSequenceType.PayrollRunNumber, ct);
        string runNumber;
        if (seq is null)
        {
            seq = new DocumentSequence(DocumentSequenceType.PayrollRunNumber, "BORD", now.Year, 1, 5);
            runNumber = seq.ConsumeNext(now);
            await _sequences.AddAsync(seq, ct);
        }
        else
        {
            runNumber = seq.ConsumeNext(now);
            _sequences.Update(seq);
        }

        var run = new PayrollRun(
            runNumber, c.PeriodYear, c.PeriodMonth, parameters.Id, c.RunType, c.Currency.ToUpperInvariant(), c.Description);

        await _runs.AddAsync(run, ct);
        await _uow.SaveChangesAsync(ct);
        return PayrollRunMapper.ToDetailDto(run);
    }
}

public class CalculatePayrollRunHandler : IRequestHandler<CalculatePayrollRunCommand, PayrollRunDetailDto>
{
    private readonly IPayrollRunRepository _runs;
    private readonly IPayslipRepository _payslips;
    private readonly IEmployeeRepository _employees;
    private readonly IEmployeeYtdTaxBaseRepository _ytd;
    private readonly IPayrollParametersRepository _parameters;
    private readonly IPayrollCalculationService _calculator;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IUnitOfWork _uow;

    public CalculatePayrollRunHandler(
        IPayrollRunRepository runs,
        IPayslipRepository payslips,
        IEmployeeRepository employees,
        IEmployeeYtdTaxBaseRepository ytd,
        IPayrollParametersRepository parameters,
        IPayrollCalculationService calculator,
        IDocumentSequenceRepository sequences,
        IUnitOfWork uow)
    {
        _runs = runs;
        _payslips = payslips;
        _employees = employees;
        _ytd = ytd;
        _parameters = parameters;
        _calculator = calculator;
        _sequences = sequences;
        _uow = uow;
    }

    public async Task<PayrollRunDetailDto> Handle(CalculatePayrollRunCommand c, CancellationToken ct)
    {
        var run = await _runs.GetByIdAsync(c.Id, ct) ?? throw new PayrollRunNotFoundException(c.Id);

        var period = new DateOnly(run.PeriodYear, run.PeriodMonth, 1);
        var parameters = await _parameters.ResolveAsync(run.PeriodYear, period, ct)
            ?? throw new PayrollParametersNotResolvedException(run.PeriodYear, run.PeriodMonth);
        run.PinParameters(parameters.Id);

        await _payslips.RemoveByRunAsync(run.Id, ct);

        var employees = await _employees.GetActiveForPayrollAsync(period, ct);
        var ytdByEmployee = (await _ytd.GetByYearAsync(run.PeriodYear, ct))
            .ToDictionary(y => y.EmployeeId);

        var now = DateTime.UtcNow;
        await EnsurePayslipSequenceAsync(now, ct);

        var totals = new PayrollRunTotals();
        var payslips = new List<Payslip>(employees.Count);

        foreach (var employee in employees)
        {
            var earnings = PayrollPayslipFactory.ResolveEarnings(employee, period);
            ytdByEmployee.TryGetValue(employee.Id, out var prior);
            var priorIncomeTaxBase = prior?.CumulativeIncomeTaxBase ?? 0m;
            var priorMinWageBase = prior?.CumulativeMinWageBase ?? 0m;

            var result = _calculator.Calculate(new PayrollCalcInput(
                GrossSalary: earnings.Gross,
                PriorCumulativeIncomeTaxBase: priorIncomeTaxBase,
                PriorCumulativeMinWageBase: priorMinWageBase,
                IsSgkIncentiveEligible: employee.IsSgkIncentiveEligible,
                OtherDeductions: earnings.OtherDeductionsTotal,
                Parameters: parameters,
                SgkGrossSalary: earnings.SgkGross));

            var payslipNumber = await _sequences.ConsumeAsync(DocumentSequenceType.PayslipNumber, now, ct);
            var payslip = BuildPayslip(run, employee, payslipNumber, earnings, result, priorIncomeTaxBase, priorMinWageBase);
            await _payslips.AddAsync(payslip, ct);
            payslips.Add(payslip);

            totals.Add(earnings.Gross, result);
        }

        run.ApplyTotals(
            totals.TotalGross, totals.TotalSgkEmployee, totals.TotalSgkEmployer,
            totals.TotalUnemploymentEmployee, totals.TotalUnemploymentEmployer, totals.TotalIncomeTax,
            totals.TotalStampTax, totals.TotalDeductions, totals.TotalNet, totals.TotalEmployerCost, payslips.Count);
        run.Calculate();
        _runs.Update(run);

        await _uow.SaveChangesAsync(ct);
        return PayrollRunMapper.ToDetailDto(run);
    }

    private async Task EnsurePayslipSequenceAsync(DateTime now, CancellationToken ct)
    {
        var seq = await _sequences.GetAsync(DocumentSequenceType.PayslipNumber, ct);
        if (seq is null)
        {
            await _sequences.AddAsync(new DocumentSequence(DocumentSequenceType.PayslipNumber, "UCRET", now.Year, 1, 5), ct);
            await _uow.SaveChangesAsync(ct);
        }
    }

    private static Payslip BuildPayslip(
        PayrollRun run,
        Employee employee,
        string payslipNumber,
        EmployeeEarnings earnings,
        PayrollCalcResult result,
        decimal priorIncomeTaxBase,
        decimal priorMinWageBase)
    {
        var payslip = new Payslip(
            payslipNumber, run.Id, employee.Id, employee.EmployeeNumber, employee.FullName, employee.NationalId,
            run.PeriodYear, run.PeriodMonth, run.ParametersId);

        payslip.ApplyComputation(
            grossEarnings: earnings.Gross,
            sgkBase: result.SgkBase,
            incomeTaxBaseThisPeriod: result.IncomeTaxBaseThisPeriod,
            cumulativeIncomeTaxBaseBefore: priorIncomeTaxBase,
            cumulativeIncomeTaxBaseAfter: result.CumulativeIncomeTaxBaseAfter,
            cumulativeMinWageBaseBefore: priorMinWageBase,
            cumulativeMinWageBaseAfter: result.MinWageBaseAfter,
            sgkEmployee: result.SgkEmployee,
            unemploymentEmployee: result.UnemploymentEmployee,
            incomeTaxGross: result.IncomeTaxGross,
            minWageIncomeTaxExemptionApplied: result.MinWageIncomeTaxExemption,
            minWageStampTaxExemptionApplied: result.StampTaxExemption,
            disabilityExemptionApplied: 0m,
            incomeTaxNet: result.IncomeTaxNet,
            stampTax: result.StampTaxNet,
            otherDeductionsTotal: earnings.OtherDeductionsTotal,
            netPay: result.NetPay,
            sgkEmployer: result.SgkEmployer,
            unemploymentEmployer: result.UnemploymentEmployer,
            employerCost: result.EmployerCost);

        foreach (var line in earnings.EarningLines)
        {
            payslip.AddEarningLine(line);
        }
        foreach (var deduction in earnings.Deductions)
        {
            payslip.AddDeductionLine(new PayslipDeductionLine(
                deduction.DeductionType, deduction.Amount, deduction.IsRecurring, deduction.SourceDeductionId));
        }
        return payslip;
    }
}

public class ApprovePayrollRunHandler : IRequestHandler<ApprovePayrollRunCommand, PayrollRunDetailDto>
{
    private readonly IPayrollRunRepository _runs;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;

    public ApprovePayrollRunHandler(IPayrollRunRepository runs, ICurrentUserAccessor currentUser, IUnitOfWork uow)
    {
        _runs = runs;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<PayrollRunDetailDto> Handle(ApprovePayrollRunCommand c, CancellationToken ct)
    {
        var run = await _runs.GetByIdAsync(c.Id, ct) ?? throw new PayrollRunNotFoundException(c.Id);
        run.Approve(_currentUser.UserIdOrThrow());
        _runs.Update(run);
        await _uow.SaveChangesAsync(ct);
        return PayrollRunMapper.ToDetailDto(run);
    }
}

public class PayPayrollRunHandler : IRequestHandler<PayPayrollRunCommand, PayrollRunDetailDto>
{
    private readonly IPayrollRunRepository _runs;
    private readonly IUnitOfWork _uow;

    public PayPayrollRunHandler(IPayrollRunRepository runs, IUnitOfWork uow)
    {
        _runs = runs;
        _uow = uow;
    }

    public async Task<PayrollRunDetailDto> Handle(PayPayrollRunCommand c, CancellationToken ct)
    {
        var run = await _runs.GetByIdAsync(c.Id, ct) ?? throw new PayrollRunNotFoundException(c.Id);
        run.MarkPaid();
        _runs.Update(run);
        await _uow.SaveChangesAsync(ct);
        return PayrollRunMapper.ToDetailDto(run);
    }
}

public class ReopenPayrollRunHandler : IRequestHandler<ReopenPayrollRunCommand, PayrollRunDetailDto>
{
    private readonly IPayrollRunRepository _runs;
    private readonly IPayslipRepository _payslips;
    private readonly IUnitOfWork _uow;

    public ReopenPayrollRunHandler(IPayrollRunRepository runs, IPayslipRepository payslips, IUnitOfWork uow)
    {
        _runs = runs;
        _payslips = payslips;
        _uow = uow;
    }

    public async Task<PayrollRunDetailDto> Handle(ReopenPayrollRunCommand c, CancellationToken ct)
    {
        var run = await _runs.GetByIdAsync(c.Id, ct) ?? throw new PayrollRunNotFoundException(c.Id);
        if (run.Status is PayrollRunStatus.Posted or PayrollRunStatus.Paid)
        {
            throw new PayrollRunReopenNotAllowedException();
        }
        run.Reopen();
        await _payslips.RemoveByRunAsync(run.Id, ct);
        run.ApplyTotals(0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0);
        _runs.Update(run);
        await _uow.SaveChangesAsync(ct);
        return PayrollRunMapper.ToDetailDto(run);
    }
}

public class PostPayrollRunHandler : IRequestHandler<PostPayrollRunCommand, PayrollRunDetailDto>
{
    private readonly IPayrollRunRepository _runs;
    private readonly IPayslipRepository _payslips;
    private readonly IEmployeeYtdTaxBaseRepository _ytd;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;

    public PostPayrollRunHandler(
        IPayrollRunRepository runs,
        IPayslipRepository payslips,
        IEmployeeYtdTaxBaseRepository ytd,
        IEmployeeRepository employees,
        IUnitOfWork uow)
    {
        _runs = runs;
        _payslips = payslips;
        _ytd = ytd;
        _employees = employees;
        _uow = uow;
    }

    public async Task<PayrollRunDetailDto> Handle(PostPayrollRunCommand c, CancellationToken ct)
    {
        var run = await _runs.GetByIdAsync(c.Id, ct) ?? throw new PayrollRunNotFoundException(c.Id);
        run.MarkPosted();

        var payslips = await _payslips.GetByRunAsync(run.Id, ct);
        foreach (var payslip in payslips)
        {
            await _ytd.AcquireEmployeeYearLockAsync(payslip.EmployeeId, run.PeriodYear, ct);
            var ytd = await _ytd.GetAsync(payslip.EmployeeId, run.PeriodYear, ct);
            if (ytd is null)
            {
                ytd = new EmployeeYtdTaxBase(payslip.EmployeeId, run.PeriodYear);
                ytd.Accumulate(payslip.IncomeTaxBaseThisPeriod, AdvanceMinWageDelta(payslip), run.PeriodMonth);
                await _ytd.AddAsync(ytd, ct);
                continue;
            }
            if (ytd.LastPeriodMonth == run.PeriodMonth) continue;
            // Forward-only, gaps allowed. Posting an EARLIER month after a later one would walk
            // the tax brackets in the wrong order, so it is refused; but a gap in one employee's
            // own history is legitimate (rehired mid-year, left out of a month's run) and must
            // not block the whole company's payroll.
            if (run.PeriodMonth < ytd.LastPeriodMonth)
            {
                throw new PayrollOutOfSequencePostException(payslip.EmployeeId, ytd.LastPeriodMonth, run.PeriodMonth);
            }
            ytd.Accumulate(payslip.IncomeTaxBaseThisPeriod, AdvanceMinWageDelta(payslip), run.PeriodMonth);
            _ytd.Update(ytd);
        }

        await AmortiseDeductionBalancesAsync(payslips, ct);

        _runs.Update(run);
        await _uow.SaveChangesAsync(ct);
        return PayrollRunMapper.ToDetailDto(run);
    }

    // WHY this belongs to POST and not to calculate: the instalment is only really withheld once
    // the run is posted, and a Calculated run can still be reopened and recalculated. Without it
    // RemainingBalance never fell, so an advance kept being deducted from the employee's net pay
    // every month forever — the instalment cap in PayrollPayslipFactory never bit because the
    // balance it caps against never moved.
    private async Task AmortiseDeductionBalancesAsync(IReadOnlyList<Payslip> payslips, CancellationToken ct)
    {
        var lines = payslips
            .SelectMany(p => p.DeductionLines)
            .Where(l => l.EmployeeDeductionId is not null && l.Amount > 0m)
            .ToList();
        if (lines.Count == 0) return;

        var deductions = (await _employees.GetDeductionsByIdsAsync(
                lines.Select(l => l.EmployeeDeductionId!.Value), ct))
            .ToDictionary(d => d.Id);

        foreach (var line in lines)
        {
            if (deductions.TryGetValue(line.EmployeeDeductionId!.Value, out var deduction))
            {
                // A percentage deduction (union dues and the like) carries no balance to amortise;
                // ReduceBalance is a no-op at zero, so no special case is needed.
                deduction.ReduceBalance(line.Amount);
            }
        }
    }

    private static decimal AdvanceMinWageDelta(Payslip payslip) =>
        payslip.CumulativeMinWageBaseAfter - payslip.CumulativeMinWageBaseBefore;
}

public class GetPayrollRunByIdHandler : IRequestHandler<GetPayrollRunByIdQuery, PayrollRunDetailDto?>
{
    private readonly IPayrollRunRepository _runs;
    public GetPayrollRunByIdHandler(IPayrollRunRepository runs) => _runs = runs;

    public async Task<PayrollRunDetailDto?> Handle(GetPayrollRunByIdQuery q, CancellationToken ct)
    {
        var run = await _runs.GetByIdAsync(q.Id, ct);
        return run is null ? null : PayrollRunMapper.ToDetailDto(run);
    }
}

public class GetPayrollRunsHandler : IRequestHandler<GetPayrollRunsQuery, PagedResult<PayrollRunListItemDto>>
{
    private readonly IPayrollRunRepository _runs;
    public GetPayrollRunsHandler(IPayrollRunRepository runs) => _runs = runs;

    public async Task<PagedResult<PayrollRunListItemDto>> Handle(GetPayrollRunsQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _runs.GetPagedAsync(q.PeriodYear, q.Status, page, pageSize, ct);
        return new PagedResult<PayrollRunListItemDto>
        {
            Items = items.Select(PayrollRunMapper.ToListItemDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetPayslipsByRunHandler : IRequestHandler<GetPayslipsByRunQuery, IReadOnlyList<PayslipDto>>
{
    private readonly IPayslipRepository _payslips;
    public GetPayslipsByRunHandler(IPayslipRepository payslips) => _payslips = payslips;

    public async Task<IReadOnlyList<PayslipDto>> Handle(GetPayslipsByRunQuery q, CancellationToken ct)
    {
        var payslips = await _payslips.GetByRunAsync(q.RunId, ct);
        return payslips.Select(PayrollRunMapper.ToDto).ToList();
    }
}

public class GetPayslipByIdHandler : IRequestHandler<GetPayslipByIdQuery, PayslipDto?>
{
    private readonly IPayslipRepository _payslips;
    public GetPayslipByIdHandler(IPayslipRepository payslips) => _payslips = payslips;

    public async Task<PayslipDto?> Handle(GetPayslipByIdQuery q, CancellationToken ct)
    {
        var payslip = await _payslips.GetByIdAsync(q.Id, ct);
        return payslip is null ? null : PayrollRunMapper.ToDto(payslip);
    }
}

internal sealed class PayrollRunTotals
{
    public decimal TotalGross { get; private set; }
    public decimal TotalSgkEmployee { get; private set; }
    public decimal TotalSgkEmployer { get; private set; }
    public decimal TotalUnemploymentEmployee { get; private set; }
    public decimal TotalUnemploymentEmployer { get; private set; }
    public decimal TotalIncomeTax { get; private set; }
    public decimal TotalStampTax { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal TotalNet { get; private set; }
    public decimal TotalEmployerCost { get; private set; }

    public void Add(decimal gross, PayrollCalcResult result)
    {
        TotalGross += gross;
        TotalSgkEmployee += result.SgkEmployee;
        TotalSgkEmployer += result.SgkEmployer;
        TotalUnemploymentEmployee += result.UnemploymentEmployee;
        TotalUnemploymentEmployer += result.UnemploymentEmployer;
        TotalIncomeTax += result.IncomeTaxNet;
        TotalStampTax += result.StampTaxNet;
        TotalDeductions += result.TotalDeductions;
        TotalNet += result.NetPay;
        TotalEmployerCost += result.EmployerCost;
    }
}
