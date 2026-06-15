using CoreAlign.Application.BI;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI;

public sealed class SavedReportService : ISavedReportService
{
    private readonly CoreAlignDbContext _db;
    private readonly ITenantContext _tenant;

    public SavedReportService(CoreAlignDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<SavedReportDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var rows = await _db.SavedReports.AsNoTracking()
            .Where(r => r.TenantId == tenantId && (r.OwnerUserId == userId || r.IsPublic))
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<SavedReportDto> CreateAsync(Guid userId, SavedReportUpsertDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _tenant.RequireTenantId();
        var report = new SavedReport(userId, dto.Name, dto.DataSource, dto.QueryConfigJson, dto.IsPublic, dto.Description);
        _db.SavedReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(report);
    }

    public async Task<SavedReportDto> UpdateAsync(Guid userId, Guid id, SavedReportUpsertDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var tenantId = _tenant.RequireTenantId();
        var report = await _db.SavedReports.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken)
            ?? throw new SavedReportNotFoundException(id);
        if (report.OwnerUserId != userId)
        {
            throw new CrossTenantAccessException();
        }
        report.Update(dto.Name, dto.Description, dto.DataSource, dto.QueryConfigJson, dto.IsPublic);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(report);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var report = await _db.SavedReports.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken)
            ?? throw new SavedReportNotFoundException(id);
        if (report.OwnerUserId != userId)
        {
            throw new CrossTenantAccessException();
        }
        _db.SavedReports.Remove(report);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SavedReportDto Map(SavedReport r) => new(
        r.Id,
        r.OwnerUserId,
        r.Name,
        r.Description,
        r.DataSource,
        r.QueryConfigJson,
        r.IsPublic,
        r.LastRunAtUtc,
        r.LastRunRowCount);
}
