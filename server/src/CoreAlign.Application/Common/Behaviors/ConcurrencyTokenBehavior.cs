using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Common.Behaviors;

public class ConcurrencyTokenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ConcurrencyTokenBehavior<TRequest, TResponse>> _logger;

    public ConcurrencyTokenBehavior(ILogger<ConcurrencyTokenBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (request is IForceConcurrencyOverride { ForceOverwrite: true })
            {
                // Force-overwrite is NOT safely implementable here: recovering by re-running
                // next() re-runs the whole handler and DOUBLE-APPLIES its mutation (INVARIANTS
                // §88). Fail loudly if a command ever opts in, rather than silently corrupting
                // data — implement save-level retry before enabling this.
                _logger.LogError(
                    "Force concurrency override attempted on {Request} but is unsupported (would double-apply the mutation).",
                    typeof(TRequest).Name);
                throw new NotSupportedException(
                    $"Force concurrency override is not supported for {typeof(TRequest).Name}; " +
                    "re-running the handler would double-apply its mutation. Implement save-level retry instead.");
            }

            var conflicting = ex.Entries
                .Select(e => e.Entity.GetType().Name)
                .Distinct()
                .ToArray();

            long currentVersion = 0;
            long attemptedVersion = 0;
            var entry = ex.Entries.FirstOrDefault();
            if (entry?.Entity is IHasConcurrencyToken aware)
            {
                attemptedVersion = aware.ConcurrencyToken;
                var dbValuesEntry = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (dbValuesEntry != null)
                {
                    currentVersion = dbValuesEntry["ConcurrencyToken"] is long v ? v : 0;
                }
            }

            throw new DomainConcurrencyException(currentVersion, attemptedVersion, conflicting);
        }
    }
}
