using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Accounting.Handlers;

public class ListGLPostingMappingsHandler : IRequestHandler<ListGLPostingMappingsQuery, IReadOnlyList<GLPostingMappingDto>>
{
    private readonly IGLPostingMappingRepository _mappings;
    private readonly IGLAccountRepository _accounts;

    public ListGLPostingMappingsHandler(IGLPostingMappingRepository mappings, IGLAccountRepository accounts)
    {
        _mappings = mappings;
        _accounts = accounts;
    }

    public async Task<IReadOnlyList<GLPostingMappingDto>> Handle(ListGLPostingMappingsQuery q, CancellationToken ct)
    {
        var overrides = (await _mappings.ListAsync(ct)).ToDictionary(m => m.PostingKey, m => m.AccountCode);
        var accounts = (await _accounts.GetAllAsync(ct)).ToDictionary(a => a.Code, a => a);

        var result = new List<GLPostingMappingDto>();
        foreach (var key in Enum.GetValues<GLPostingKey>())
        {
            var def = GLPostingDefaults.CodeFor(key);
            overrides.TryGetValue(key, out var ov);
            var effective = ov ?? def ?? string.Empty;
            accounts.TryGetValue(effective, out var account);
            result.Add(new GLPostingMappingDto(
                key,
                key.ToString(),
                effective,
                ov,
                def,
                account?.Name,
                account is not null && account.IsPostable && account.IsActive));
        }
        return result;
    }
}

public class ConfigureGLPostingMappingHandler : IRequestHandler<ConfigureGLPostingMappingCommand, GLPostingMappingDto>
{
    private readonly IGLPostingMappingRepository _mappings;
    private readonly IGLAccountRepository _accounts;
    private readonly IUnitOfWork _uow;

    public ConfigureGLPostingMappingHandler(IGLPostingMappingRepository mappings, IGLAccountRepository accounts, IUnitOfWork uow)
    {
        _mappings = mappings;
        _accounts = accounts;
        _uow = uow;
    }

    public async Task<GLPostingMappingDto> Handle(ConfigureGLPostingMappingCommand c, CancellationToken ct)
    {
        var code = c.AccountCode.Trim();
        var account = await _accounts.GetByCodeAsync(code, ct)
            ?? throw new KeyNotFoundException($"GL account '{code}' not found for current tenant.");

        var existing = await _mappings.GetByKeyAsync(c.Key, ct);
        if (existing is null)
        {
            existing = new GLPostingMapping(c.Key, code);
            await _mappings.AddAsync(existing, ct);
        }
        else
        {
            existing.SetAccountCode(code);
            _mappings.Update(existing);
        }
        await _uow.SaveChangesAsync(ct);

        return new GLPostingMappingDto(
            c.Key,
            c.Key.ToString(),
            code,
            code,
            GLPostingDefaults.CodeFor(c.Key),
            account.Name,
            account.IsPostable && account.IsActive);
    }
}
