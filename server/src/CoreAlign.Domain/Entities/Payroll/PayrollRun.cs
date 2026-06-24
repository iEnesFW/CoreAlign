using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Payroll;

public class PayrollRun : TenantEntity, IXminConcurrency
{
    public string RunNumber { get; private set; } = string.Empty;
    public int PeriodYear { get; private set; }
    public int PeriodMonth { get; private set; }
    public PayrollRunType RunType { get; private set; } = PayrollRunType.Regular;
    public PayrollRunStatus Status { get; private set; } = PayrollRunStatus.Draft;
    public string? Description { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public Guid ParametersId { get; private set; }

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
    public int PayslipCount { get; private set; }

    public DateTime? CalculatedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    public PayrollParameters Parameters { get; private set; } = null!;

    protected PayrollRun() { }

    public PayrollRun(
        string runNumber,
        int periodYear,
        int periodMonth,
        Guid parametersId,
        PayrollRunType runType = PayrollRunType.Regular,
        string currency = "TRY",
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(runNumber))
        {
            throw new ArgumentException("Run number is required.", nameof(runNumber));
        }
        if (periodMonth is < 1 or > 12)
        {
            throw new ArgumentException("Period month must be between 1 and 12.", nameof(periodMonth));
        }
        RunNumber = runNumber.Trim();
        PeriodYear = periodYear;
        PeriodMonth = periodMonth;
        ParametersId = parametersId;
        RunType = runType;
        Currency = currency;
        Description = description;
    }

    public void PinParameters(Guid parametersId)
    {
        if (Status != PayrollRunStatus.Draft)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PayrollRunStatus.Calculated.ToString());
        }
        ParametersId = parametersId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyTotals(
        decimal totalGross,
        decimal totalSgkEmployee,
        decimal totalSgkEmployer,
        decimal totalUnemploymentEmployee,
        decimal totalUnemploymentEmployer,
        decimal totalIncomeTax,
        decimal totalStampTax,
        decimal totalDeductions,
        decimal totalNet,
        decimal totalEmployerCost,
        int payslipCount)
    {
        TotalGross = Math.Round(totalGross, 4);
        TotalSgkEmployee = Math.Round(totalSgkEmployee, 4);
        TotalSgkEmployer = Math.Round(totalSgkEmployer, 4);
        TotalUnemploymentEmployee = Math.Round(totalUnemploymentEmployee, 4);
        TotalUnemploymentEmployer = Math.Round(totalUnemploymentEmployer, 4);
        TotalIncomeTax = Math.Round(totalIncomeTax, 4);
        TotalStampTax = Math.Round(totalStampTax, 4);
        TotalDeductions = Math.Round(totalDeductions, 4);
        TotalNet = Math.Round(totalNet, 4);
        TotalEmployerCost = Math.Round(totalEmployerCost, 4);
        PayslipCount = payslipCount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Calculate()
    {
        if (Status != PayrollRunStatus.Draft)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PayrollRunStatus.Calculated.ToString());
        }
        Status = PayrollRunStatus.Calculated;
        CalculatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CalculatedAtUtc.Value;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != PayrollRunStatus.Calculated)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PayrollRunStatus.Approved.ToString());
        }
        Status = PayrollRunStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ApprovedAtUtc.Value;
    }

    public void MarkPosted()
    {
        if (Status != PayrollRunStatus.Approved)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PayrollRunStatus.Posted.ToString());
        }
        Status = PayrollRunStatus.Posted;
        PostedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = PostedAtUtc.Value;
        AddDomainEvent(new PayrollRunPostedEvent(
            TenantId, Id, RunNumber, PeriodYear, PeriodMonth, TotalNet, TotalEmployerCost, UpdatedAtUtc));
    }

    public void MarkPaid()
    {
        if (Status != PayrollRunStatus.Posted)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PayrollRunStatus.Paid.ToString());
        }
        Status = PayrollRunStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = PaidAtUtc.Value;
        AddDomainEvent(new PayrollRunPaidEvent(
            TenantId, Id, RunNumber, PeriodYear, PeriodMonth, TotalNet, UpdatedAtUtc));
    }

    public void Reopen()
    {
        if (Status != PayrollRunStatus.Calculated)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), PayrollRunStatus.Draft.ToString());
        }
        Status = PayrollRunStatus.Draft;
        CalculatedAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
