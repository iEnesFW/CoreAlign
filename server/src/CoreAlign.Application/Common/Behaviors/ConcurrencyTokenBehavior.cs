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
            var force = request is IForceConcurrencyOverride o && o.ForceOverwrite;
            if (force)
            {
                _logger.LogWarning("Concurrency conflict force-overwritten on {Request}", typeof(TRequest).Name);
                return await ResolveForceOverwriteAsync(ex, next, cancellationToken);
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

    private static async Task<TResponse> ResolveForceOverwriteAsync(
        DbUpdateConcurrencyException ex,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        foreach (var entry in ex.Entries)
        {
            var dbValues = await entry.GetDatabaseValuesAsync(cancellationToken);
            if (dbValues != null)
            {
                entry.OriginalValues.SetValues(dbValues);
            }
        }
        return await next();
    }
}
