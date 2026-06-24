using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Common.Caching;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Common.Behaviors;

public class IssueCreditNoteIdempotencyBehavior : IPipelineBehavior<IssueCreditNoteCommand, InvoiceDto>
{
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromMinutes(10);

    private readonly ITenantContext _tenantContext;
    private readonly IDistributedCacheService? _cache;

    public IssueCreditNoteIdempotencyBehavior(ITenantContext tenantContext, IDistributedCacheService? cache = null)
    {
        _tenantContext = tenantContext;
        _cache = cache;
    }

    public async Task<InvoiceDto> Handle(
        IssueCreditNoteCommand request,
        RequestHandlerDelegate<InvoiceDto> next,
        CancellationToken cancellationToken)
    {
        if (_cache is null || !_tenantContext.HasTenant)
        {
            return await next();
        }

        var cacheKey = _cache.BuildKey(
            nameof(CacheRegion.Generic),
            _tenantContext.RequireTenantId(),
            BuildFingerprint(request));

        var cached = await _cache.GetAsync<InvoiceDto>(nameof(CacheRegion.Generic), cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var result = await next();

        await _cache.SetAsync(nameof(CacheRegion.Generic), cacheKey, result, IdempotencyWindow, cancellationToken);
        return result;
    }

    private static string BuildFingerprint(IssueCreditNoteCommand request)
    {
        if (request.OperationId is { } operationId && operationId != Guid.Empty)
        {
            return $"credit-note:op:{operationId:N}";
        }

        var lines = request.Lines
            .GroupBy(l => l.InvoiceLineId)
            .Select(g => (LineId: g.Key, Quantity: g.Sum(l => l.Quantity)))
            .OrderBy(t => t.LineId)
            .Select(t => $"{t.LineId:N}={t.Quantity.ToString(CultureInfo.InvariantCulture)}");

        var raw = $"{request.InvoiceId:N}|{string.Join(",", lines)}|{request.ReturnRequestId:N}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"credit-note:hash:{Convert.ToHexString(hash)}";
    }
}
