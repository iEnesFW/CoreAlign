using CoreAlign.Application.BI;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI;

public sealed class DashboardService : IDashboardService
{
    private readonly CoreAlignDbContext _db;
    private readonly ITenantContext _tenant;

    public DashboardService(CoreAlignDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<DashboardWidgetDto>> GetUserDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var widgets = await _db.DashboardWidgets.AsNoTracking()
            .Where(w => w.TenantId == tenantId && (w.UserId == userId || w.UserId == null) && w.IsActive)
            .OrderBy(w => w.DisplayOrder)
            .ToListAsync(cancellationToken);
        return widgets.Select(Map).ToList();
    }

    public async Task SaveWidgetLayoutAsync(Guid userId, IReadOnlyList<DashboardWidgetUpsertDto> widgets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(widgets);
        var tenantId = _tenant.RequireTenantId();
        foreach (var dto in widgets)
        {
            if (dto.Id is null)
            {
                continue;
            }
            var existing = await _db.DashboardWidgets.FirstOrDefaultAsync(w => w.Id == dto.Id && w.TenantId == tenantId && w.UserId == userId, cancellationToken);
            if (existing is null)
            {
                continue;
            }
            existing.UpdateLayout(dto.GridX, dto.GridY, dto.Width, dto.Height, dto.DisplayOrder);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardWidgetDto> AddWidgetAsync(Guid userId, DashboardWidgetUpsertDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _tenant.RequireTenantId();
        var widget = new DashboardWidget(
            userId,
            dto.Title,
            dto.Type,
            dto.DataSource,
            dto.QueryConfigJson,
            dto.GridX,
            dto.GridY,
            dto.Width,
            dto.Height,
            dto.DisplayOrder);
        _db.DashboardWidgets.Add(widget);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(widget);
    }

    public async Task RemoveWidgetAsync(Guid userId, Guid widgetId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var widget = await _db.DashboardWidgets.FirstOrDefaultAsync(w => w.Id == widgetId && w.TenantId == tenantId && w.UserId == userId, cancellationToken)
            ?? throw new DashboardWidgetNotFoundException(widgetId);
        _db.DashboardWidgets.Remove(widget);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static DashboardWidgetDto Map(DashboardWidget w) => new(
        w.Id,
        w.UserId,
        w.Title,
        w.Type,
        w.DataSource,
        w.QueryConfigJson,
        w.GridX,
        w.GridY,
        w.Width,
        w.Height,
        w.DisplayOrder,
        w.IsActive);
}
