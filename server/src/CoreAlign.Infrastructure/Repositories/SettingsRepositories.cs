using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class TenantSettingRepository : ITenantSettingRepository
{
    private readonly CoreAlignDbContext _context;
    public TenantSettingRepository(CoreAlignDbContext context) => _context = context;

    public Task<TenantSetting?> GetAsync(string category, string key, CancellationToken ct = default) =>
        _context.TenantSettingsStore.FirstOrDefaultAsync(s => s.Category == category && s.Key == key, ct);

    public async Task<IReadOnlyList<TenantSetting>> ListAsync(string? category = null, CancellationToken ct = default)
    {
        var query = _context.TenantSettingsStore.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(s => s.Category == category);
        return await query.OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync(ct);
    }

    public async Task UpsertAsync(
        string category,
        string key,
        string? value,
        string dataType = "string",
        string? description = null,
        bool isSensitive = false,
        CancellationToken ct = default)
    {
        var existing = await GetAsync(category, key, ct);
        if (existing is null)
        {
            var setting = new TenantSetting(category, key, value, dataType, description, isSensitive);
            await _context.TenantSettingsStore.AddAsync(setting, ct);
        }
        else
        {
            existing.SetValue(value);
            existing.Describe(description, dataType, isSensitive);
            _context.TenantSettingsStore.Update(existing);
        }
    }

    public async Task DeleteAsync(string category, string key, CancellationToken ct = default)
    {
        var existing = await GetAsync(category, key, ct);
        if (existing is null) return;
        _context.TenantSettingsStore.Remove(existing);
    }
}

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly CoreAlignDbContext _context;
    public EmailTemplateRepository(CoreAlignDbContext context) => _context = context;

    public Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<EmailTemplate?> GetByCodeAsync(string code, string locale, CancellationToken ct = default) =>
        _context.EmailTemplates.FirstOrDefaultAsync(t => t.Code == code && t.Locale == locale, ct);

    public async Task<IReadOnlyList<EmailTemplate>> ListAsync(CancellationToken ct = default) =>
        await _context.EmailTemplates
            .AsNoTracking()
            .OrderBy(t => t.Code)
            .ThenBy(t => t.Locale)
            .ToListAsync(ct);

    public async Task AddAsync(EmailTemplate template, CancellationToken ct = default) =>
        await _context.EmailTemplates.AddAsync(template, ct);

    public void Update(EmailTemplate template) => _context.EmailTemplates.Update(template);
    public void Remove(EmailTemplate template) => _context.EmailTemplates.Remove(template);
}
