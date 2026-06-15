using CoreAlign.Domain.Exceptions;
using DynamicExpresso;

namespace CoreAlign.Application.GlassEnclosure.Services;

public interface IExpressionEvaluator
{
    decimal EvaluateNumeric(string expression, IReadOnlyDictionary<string, object> variables);
    bool EvaluateBoolean(string expression, IReadOnlyDictionary<string, object> variables);
}

public class DynamicExpressoEvaluator : IExpressionEvaluator
{
    private readonly Interpreter _interpreter;

    public DynamicExpressoEvaluator()
    {
        _interpreter = new Interpreter(InterpreterOptions.Default)
            .SetFunction("ceil", (Func<decimal, decimal>)(x => Math.Ceiling(x)))
            .SetFunction("floor", (Func<decimal, decimal>)(x => Math.Floor(x)))
            .SetFunction("round", (Func<decimal, decimal>)(x => Math.Round(x)))
            .SetFunction("max", (Func<decimal, decimal, decimal>)Math.Max)
            .SetFunction("min", (Func<decimal, decimal, decimal>)Math.Min)
            .SetFunction("abs", (Func<decimal, decimal>)Math.Abs);
    }

    public decimal EvaluateNumeric(string expression, IReadOnlyDictionary<string, object> variables)
    {
        try
        {
            var parameters = variables.Select(kv => new Parameter(kv.Key, kv.Value)).ToArray();
            var result = _interpreter.Eval(expression, parameters);
            return result switch
            {
                decimal d => d,
                double dd => (decimal)dd,
                int i => i,
                long l => l,
                _ => Convert.ToDecimal(result),
            };
        }
        catch (Exception ex)
        {
            throw new GlassFormulaEvaluationException(expression, ex.Message);
        }
    }

    public bool EvaluateBoolean(string expression, IReadOnlyDictionary<string, object> variables)
    {
        try
        {
            var parameters = variables.Select(kv => new Parameter(kv.Key, kv.Value)).ToArray();
            var result = _interpreter.Eval<bool>(expression, parameters);
            return result;
        }
        catch (Exception ex)
        {
            throw new GlassFormulaEvaluationException(expression, ex.Message);
        }
    }
}
