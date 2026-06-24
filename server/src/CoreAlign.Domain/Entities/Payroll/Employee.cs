using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Payroll;

public class Employee : TenantEntity, IXminConcurrency, ISoftDeletable
{
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string NationalId { get; private set; } = string.Empty;
    public string? SgkRegistrationNo { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public DateOnly HireDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }
    public EmploymentStatus Status { get; private set; } = EmploymentStatus.Active;
    public string? Department { get; private set; }
    public string? Title { get; private set; }
    public EmploymentType EmploymentType { get; private set; } = EmploymentType.FullTime;
    public SalaryBasis SalaryBasis { get; private set; } = SalaryBasis.Gross;
    public decimal BaseSalaryGross { get; private set; }
    public string SalaryCurrency { get; private set; } = "TRY";
    public string? Iban { get; private set; }
    public string? BankName { get; private set; }
    public bool IsSgkIncentiveEligible { get; private set; }
    public DisabilityDegree DisabilityDegree { get; private set; } = DisabilityDegree.None;
    public bool IsRetiredWorking { get; private set; }
    public bool SgkExempt { get; private set; }
    public int DependentCount { get; private set; }
    public bool SpouseEmployed { get; private set; }
    public Guid? UserId { get; private set; }
    public string? TerminationReason { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public ICollection<SalaryComponent> SalaryComponents { get; private set; } = new List<SalaryComponent>();
    public ICollection<EmployeeDeduction> Deductions { get; private set; } = new List<EmployeeDeduction>();

    public string FullName => $"{FirstName} {LastName}".Trim();

    protected Employee() { }

    public Employee(
        string employeeNumber,
        string firstName,
        string lastName,
        string nationalId,
        DateOnly hireDate,
        decimal baseSalaryGross,
        EmploymentType employmentType = EmploymentType.FullTime,
        SalaryBasis salaryBasis = SalaryBasis.Gross,
        string salaryCurrency = "TRY",
        string? sgkRegistrationNo = null,
        string? email = null,
        string? phone = null,
        string? department = null,
        string? title = null,
        string? iban = null,
        string? bankName = null,
        bool isSgkIncentiveEligible = false,
        DisabilityDegree disabilityDegree = DisabilityDegree.None,
        bool isRetiredWorking = false,
        bool sgkExempt = false,
        int dependentCount = 0,
        bool spouseEmployed = false,
        Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            throw new ArgumentException("Employee number is required.", nameof(employeeNumber));
        }
        if (baseSalaryGross < 0m)
        {
            throw new ArgumentException("Base salary cannot be negative.", nameof(baseSalaryGross));
        }
        EmployeeNumber = employeeNumber.Trim();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        NationalId = nationalId.Trim();
        HireDate = hireDate;
        BaseSalaryGross = Math.Round(baseSalaryGross, 4);
        EmploymentType = employmentType;
        SalaryBasis = salaryBasis;
        SalaryCurrency = salaryCurrency;
        SgkRegistrationNo = sgkRegistrationNo;
        Email = email;
        Phone = phone;
        Department = department;
        Title = title;
        Iban = iban;
        BankName = bankName;
        IsSgkIncentiveEligible = isSgkIncentiveEligible;
        DisabilityDegree = disabilityDegree;
        IsRetiredWorking = isRetiredWorking;
        SgkExempt = sgkExempt;
        DependentCount = dependentCount < 0 ? 0 : dependentCount;
        SpouseEmployed = spouseEmployed;
        UserId = userId;
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        string? email,
        string? phone,
        string? department,
        string? title,
        string? iban,
        string? bankName,
        int dependentCount,
        bool spouseEmployed)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email;
        Phone = phone;
        Department = department;
        Title = title;
        Iban = iban;
        BankName = bankName;
        DependentCount = dependentCount < 0 ? 0 : dependentCount;
        SpouseEmployed = spouseEmployed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeBaseSalary(decimal baseSalaryGross, DateOnly effectiveDate)
    {
        if (baseSalaryGross < 0m)
        {
            throw new ArgumentException("Base salary cannot be negative.", nameof(baseSalaryGross));
        }
        var previous = BaseSalaryGross;
        BaseSalaryGross = Math.Round(baseSalaryGross, 4);
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new EmployeeSalaryChangedEvent(
            TenantId, Id, EmployeeNumber, previous, BaseSalaryGross, effectiveDate, UpdatedAtUtc));
    }

    public void PlaceOnLeave()
    {
        if (Status != EmploymentStatus.Active)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), EmploymentStatus.OnLeave.ToString());
        }
        Status = EmploymentStatus.OnLeave;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReturnFromLeave()
    {
        if (Status != EmploymentStatus.OnLeave)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), EmploymentStatus.Active.ToString());
        }
        Status = EmploymentStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Terminate(DateOnly terminationDate, string? reason)
    {
        if (Status == EmploymentStatus.Terminated)
        {
            throw new InvalidOrderStatusTransitionException(Status.ToString(), EmploymentStatus.Terminated.ToString());
        }
        Status = EmploymentStatus.Terminated;
        TerminationDate = terminationDate;
        TerminationReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedByUserId = userId;
        DeletedReason = reason;
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedByUserId = null;
        DeletedReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddSalaryComponent(SalaryComponent component)
    {
        component.AttachToEmployee(Id);
        SalaryComponents.Add(component);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddDeduction(EmployeeDeduction deduction)
    {
        deduction.AttachToEmployee(Id);
        Deductions.Add(deduction);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
