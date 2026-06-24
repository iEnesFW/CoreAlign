namespace CoreAlign.Domain.Enums;

public enum EmploymentStatus
{
    Active = 0,
    OnLeave = 1,
    Terminated = 2,
}

public enum EmploymentType
{
    FullTime = 0,
    PartTime = 1,
    Seasonal = 2,
}

public enum SalaryBasis
{
    Gross = 0,
    Net = 1,
}

public enum DisabilityDegree
{
    None = 0,
    Degree1 = 1,
    Degree2 = 2,
    Degree3 = 3,
}

public enum SalaryComponentType
{
    BaseSalary = 0,
    Meal = 1,
    Transport = 2,
    Bonus = 3,
    Premium = 4,
    Overtime = 5,
    Family = 6,
    Child = 7,
}

public enum DeductionType
{
    Advance = 0,
    Garnishment = 1,
    UnionDues = 2,
    PrivatePensionBES = 3,
    Custom = 4,
}

public enum PayrollRunType
{
    Regular = 0,
    OffCycle = 1,
}

public enum PayrollRunStatus
{
    Draft = 0,
    Calculated = 1,
    Approved = 2,
    Posted = 3,
    Paid = 4,
}
