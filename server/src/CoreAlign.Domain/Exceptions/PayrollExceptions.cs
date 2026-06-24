namespace CoreAlign.Domain.Exceptions;

public class EmployeeNotFoundException : NotFoundException
{
    public EmployeeNotFoundException() : base("Employee not found.") { }
    public EmployeeNotFoundException(Guid id) : base($"Employee {id} not found.") { }
}

public class DuplicateEmployeeNumberException : ConflictException
{
    public DuplicateEmployeeNumberException(string employeeNumber)
        : base($"An employee with number '{employeeNumber}' already exists.") { }
}

public class DuplicateEmployeeNationalIdException : ConflictException
{
    public DuplicateEmployeeNationalIdException()
        : base("Another employee already uses this national id.") { }
}

public class SalaryComponentNotFoundException : NotFoundException
{
    public SalaryComponentNotFoundException() : base("Salary component not found on the specified employee.") { }
}

public class EmployeeDeductionNotFoundException : NotFoundException
{
    public EmployeeDeductionNotFoundException() : base("Deduction not found on the specified employee.") { }
}

public class PayrollRunNotFoundException : NotFoundException
{
    public PayrollRunNotFoundException() : base("Payroll run not found.") { }
    public PayrollRunNotFoundException(Guid id) : base($"Payroll run {id} not found.") { }
}

public class DuplicatePayrollRunException : ConflictException
{
    public DuplicatePayrollRunException(int periodYear, int periodMonth)
        : base($"A payroll run already exists for period {periodYear:D4}-{periodMonth:D2}.") { }
}

public class PayrollParametersNotResolvedException : NotFoundException
{
    public PayrollParametersNotResolvedException(int periodYear, int periodMonth)
        : base($"No effective payroll parameters resolve for period {periodYear:D4}-{periodMonth:D2}.") { }
}

public class PayrollParametersNotFoundException : NotFoundException
{
    public PayrollParametersNotFoundException(Guid id) : base($"Payroll parameters {id} not found.") { }
}

public class GlobalPayrollParametersReadOnlyException : ForbiddenException
{
    public GlobalPayrollParametersReadOnlyException()
        : base("Global system payroll parameters are read-only; create a tenant override instead.") { }
}

public class PayrollRunReopenNotAllowedException : ConflictException
{
    public PayrollRunReopenNotAllowedException()
        : base("Only a calculated payroll run can be reopened; posted or paid runs are immutable.") { }
}

public class PayrollOutOfSequencePostException : ConflictException
{
    public PayrollOutOfSequencePostException(Guid employeeId, int lastPostedMonth, int attemptedMonth)
        : base($"Payroll for employee {employeeId} must be posted sequentially: last posted month is {lastPostedMonth:D2}, "
            + $"attempted {attemptedMonth:D2}. Post the missing earlier month(s) first.") { }
}
