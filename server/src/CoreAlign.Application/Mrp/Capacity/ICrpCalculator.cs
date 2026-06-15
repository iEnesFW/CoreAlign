namespace CoreAlign.Application.Mrp.Capacity;

public interface ICrpCalculator
{
    CrpResult Compute(CrpInput input);
}
