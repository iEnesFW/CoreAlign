using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Common.Behaviors;

/// <summary>
/// Collects field-level audit entries for any request marked with
/// <see cref="IAuditableMutation"/>. The behavior itself does not persist —
/// entries stay in <see cref="IAuditContext"/> until the outbox publisher
/// flushes them inside the same transaction as the handler's domain writes.
/// On handler failure the context is cleared so partially captured diffs
/// never leak into the next request that shares the scope.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IAuditableMutation
{
    private readonly IAuditContext _auditContext;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IAuditFieldRedactor _redactor;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(
        IAuditContext auditContext,
        ICurrentUserAccessor currentUser,
        IAuditFieldRedactor redactor,
        ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _auditContext = auditContext;
        _currentUser = currentUser;
        _redactor = redactor;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await next();

            var pending = _auditContext.PendingEntries;
            if (pending.Count > 0 && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Audit: {RequestType} captured {EntryCount} entries for aggregate {AggregateType}:{AggregateId} by user {UserId}",
                    typeof(TRequest).Name,
                    pending.Count,
                    request.AggregateType,
                    request.AggregateId,
                    _currentUser.UserId);
            }

            _ = _redactor;

            return response;
        }
        catch
        {
            _auditContext.Clear();
            throw;
        }
    }
}
