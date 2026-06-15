using System.Text.Json;
using CoreAlign.Application.Compliance.Audit;
using CoreAlign.Domain.Entities;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Compliance;

public sealed class ScheduledAuditExportConfigRepository : IScheduledAuditExportConfigRepository
{
    public const string SettingCategory = "Compliance";
    public const string SettingKey = "AuditExportSchedule";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private readonly CoreAlignDbContext _context;

    public ScheduledAuditExportConfigRepository(CoreAlignDbContext context) => _context = context;

    public async Task<ScheduledAuditExportConfig?> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var setting = await _context.TenantSettingsStore
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Category == SettingCategory && s.Key == SettingKey, cancellationToken);
        return Deserialize(setting?.Value);
    }

    public async Task UpsertForTenantAsync(Guid tenantId, ScheduledAuditExportConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var existing = await _context.TenantSettingsStore
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Category == SettingCategory && s.Key == SettingKey, cancellationToken);
        if (existing is null)
        {
            var entity = new TenantSetting(SettingCategory, SettingKey, json, "json", "Scheduled audit log export configuration")
            {
                TenantId = tenantId,
            };
            await _context.TenantSettingsStore.AddAsync(entity, cancellationToken);
        }
        else
        {
            existing.SetValue(json);
            _context.TenantSettingsStore.Update(existing);
        }
    }

    public async Task<IReadOnlyList<(Guid TenantId, ScheduledAuditExportConfig Config)>> ListEnabledAcrossTenantsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.TenantSettingsStore
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.Category == SettingCategory && s.Key == SettingKey)
            .ToListAsync(cancellationToken);

        var results = new List<(Guid, ScheduledAuditExportConfig)>(settings.Count);
        foreach (var setting in settings)
        {
            var cfg = Deserialize(setting.Value);
            if (cfg is { Enabled: true })
            {
                results.Add((setting.TenantId, cfg));
            }
        }
        return results;
    }

    private static ScheduledAuditExportConfig? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<ScheduledAuditExportConfig>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
