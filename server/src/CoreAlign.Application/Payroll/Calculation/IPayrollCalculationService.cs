namespace CoreAlign.Application.Payroll.Calculation;

public interface IPayrollCalculationService
{
    PayrollCalcResult Calculate(PayrollCalcInput input);
}
