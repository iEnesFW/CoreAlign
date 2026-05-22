using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CoreAlign.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IValidator<TRequest>[] _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators as IValidator<TRequest>[] ?? validators.ToArray();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Most requests have zero or one validator — skip Task.WhenAll allocation
        // and the failure-list materialization for those common cases.
        if (_validators.Length == 0)
            return await next();

        var context = new ValidationContext<TRequest>(request);

        if (_validators.Length == 1)
        {
            var result = await _validators[0].ValidateAsync(context, cancellationToken);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);
            return await next();
        }

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<ValidationFailure>? failures = null;
        foreach (var r in validationResults)
        {
            if (r.Errors.Count == 0) continue;
            failures ??= new List<ValidationFailure>(r.Errors.Count);
            failures.AddRange(r.Errors);
        }

        if (failures is { Count: > 0 })
            throw new ValidationException(failures);

        return await next();
    }
}
