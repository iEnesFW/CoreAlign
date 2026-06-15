using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoreAlign.Application.BI;
using CoreAlign.Application.BI.DataSources;
using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CoreAlign.Infrastructure.BI;

public sealed class BIReportService : IBIReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly CoreAlignDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IReadOnlyDictionary<BIDataSource, IBIDataSourceAggregator> _sources;
    private readonly IReadOnlyDictionary<BIExportFormat, IExportProvider> _exporters;
    private readonly IMemoryCache _cache;

    public BIReportService(
        CoreAlignDbContext db,
        ITenantContext tenant,
        ICurrentUserAccessor currentUser,
        IEnumerable<IBIDataSourceAggregator> sources,
        IEnumerable<IExportProvider> exporters,
        IMemoryCache cache)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _sources = sources.ToDictionary(s => s.Source);
        _exporters = exporters.ToDictionary(e => e.Format);
        _cache = cache;
    }

    public async Task<BIResultDto> ExecuteAsync(BIDataSource dataSource, BIQueryConfigDto config, CancellationToken cancellationToken = default)
    {
        if (!_sources.TryGetValue(dataSource, out var src))
        {
            throw new InvalidOperationException($"No data source registered for {dataSource}.");
        }

        var resolvedConfig = config ?? new BIQueryConfigDto(null, null, null, null, null, null, null);
        var cacheKey = BuildExecuteCacheKey(dataSource, resolvedConfig);
        if (_cache.TryGetValue<BIResultDto>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var result = await src.ExecuteAsync(resolvedConfig, cancellationToken);
        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    private string BuildExecuteCacheKey(BIDataSource dataSource, BIQueryConfigDto config)
    {
        var tenantId = _tenant.RequireTenantId();
        var payload = JsonSerializer.Serialize(config, JsonOptions);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return $"bi:exec:{tenantId:N}:{dataSource}:{Convert.ToHexString(digest)}";
    }

    public async Task<BIResultDto> RunSavedReportAsync(Guid savedReportId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var report = await _db.SavedReports.FirstOrDefaultAsync(r => r.Id == savedReportId && r.TenantId == tenantId, cancellationToken)
            ?? throw new SavedReportNotFoundException(savedReportId);
        var config = JsonSerializer.Deserialize<BIQueryConfigDto>(report.QueryConfigJson, JsonOptions)
            ?? new BIQueryConfigDto(null, null, null, null, null, null, null);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await ExecuteAsync(report.DataSource, config, cancellationToken);
            stopwatch.Stop();
            report.RecordRun(DateTime.UtcNow, result.TotalRowCount);
            _db.ReportRuns.Add(new ReportRun(
                savedReportId,
                _currentUser.UserIdOrThrow(),
                DateTime.UtcNow,
                result.TotalRowCount,
                exportFormat: null,
                durationMs: stopwatch.ElapsedMilliseconds));
            await _db.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _db.ReportRuns.Add(new ReportRun(
                savedReportId,
                _currentUser.UserIdOrThrow(),
                DateTime.UtcNow,
                0,
                exportFormat: null,
                durationMs: stopwatch.ElapsedMilliseconds,
                errorMessage: ex.Message));
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<byte[]> ExportAsync(Guid savedReportId, BIExportFormat format, CancellationToken cancellationToken = default)
    {
        if (!_exporters.TryGetValue(format, out var exporter))
        {
            throw new InvalidOperationException($"No export provider registered for {format}.");
        }
        var tenantId = _tenant.RequireTenantId();
        var report = await _db.SavedReports.FirstOrDefaultAsync(r => r.Id == savedReportId && r.TenantId == tenantId, cancellationToken)
            ?? throw new SavedReportNotFoundException(savedReportId);
        var config = JsonSerializer.Deserialize<BIQueryConfigDto>(report.QueryConfigJson, JsonOptions)
            ?? new BIQueryConfigDto(null, null, null, null, null, null, null);
        var result = await ExecuteAsync(report.DataSource, config, cancellationToken);
        var bytes = await exporter.ExportAsync(report.Name, result, cancellationToken);

        report.RecordRun(DateTime.UtcNow, result.TotalRowCount);
        var run = new ReportRun(savedReportId, _currentUser.UserIdOrThrow(), DateTime.UtcNow, result.TotalRowCount, format, durationMs: null);
        _db.ReportRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
        return bytes;
    }
}
