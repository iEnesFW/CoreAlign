using System.Text.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Dunning;

public class ListDunningSettingsHandler : IRequestHandler<ListDunningSettingsQuery, IReadOnlyList<DunningSettingDto>>
{
    private readonly IDunningSettingRepository _repo;
    public ListDunningSettingsHandler(IDunningSettingRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<DunningSettingDto>> Handle(ListDunningSettingsQuery q, CancellationToken ct)
    {
        var stored = (await _repo.ListAsync(ct)).ToDictionary(s => s.Type);
        return Enum.GetValues<DunningType>()
            .Select(type => stored.TryGetValue(type, out var s)
                ? DunningSettingMapper.ToDto(s)
                : DunningSettingMapper.Default(type))
            .ToList();
    }
}

public class UpsertDunningSettingHandler : IRequestHandler<UpsertDunningSettingCommand, DunningSettingDto>
{
    private readonly IDunningSettingRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpsertDunningSettingHandler(IDunningSettingRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DunningSettingDto> Handle(UpsertDunningSettingCommand c, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(c.RecipientUserIds ?? new List<Guid>());
        var existing = await _repo.GetByTypeAsync(c.Type, ct);
        if (existing is null)
        {
            existing = new DunningSetting(c.Type, c.IsEnabled, c.SendInApp, c.SendEmail, json);
            await _repo.AddAsync(existing, ct);
        }
        else
        {
            existing.Update(c.IsEnabled, c.SendInApp, c.SendEmail, json);
            _repo.Update(existing);
        }
        await _uow.SaveChangesAsync(ct);
        return DunningSettingMapper.ToDto(existing);
    }
}

internal static class DunningSettingMapper
{
    public static DunningSettingDto ToDto(DunningSetting s) => new()
    {
        Type = s.Type,
        IsEnabled = s.IsEnabled,
        SendInApp = s.SendInApp,
        SendEmail = s.SendEmail,
        RecipientUserIds = Deserialize(s.RecipientUserIdsJson),
    };

    public static DunningSettingDto Default(DunningType type) => new()
    {
        Type = type,
        IsEnabled = false,
        SendInApp = true,
        SendEmail = false,
        RecipientUserIds = new List<Guid>(),
    };

    private static List<Guid> Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return new List<Guid>();
        }
    }
}
