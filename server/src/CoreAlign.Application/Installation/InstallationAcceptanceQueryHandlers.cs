using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Installation;

public sealed class GetInstallationAcceptanceByWorkOrderIdHandler
    : IRequestHandler<GetInstallationAcceptanceByWorkOrderIdQuery, InstallationAcceptanceDto?>
{
    private readonly IInstallationAcceptanceRepository _repo;
    public GetInstallationAcceptanceByWorkOrderIdHandler(IInstallationAcceptanceRepository repo) => _repo = repo;

    public async Task<InstallationAcceptanceDto?> Handle(GetInstallationAcceptanceByWorkOrderIdQuery q, CancellationToken ct)
    {
        var entity = await _repo.GetByWorkOrderIdAsync(q.WorkOrderId, ct);
        return entity is null ? null : InstallationAcceptanceMapper.ToDto(entity);
    }
}

public sealed class GetInstallationAcceptanceByIdHandler
    : IRequestHandler<GetInstallationAcceptanceByIdQuery, InstallationAcceptanceDto?>
{
    private readonly IInstallationAcceptanceRepository _repo;
    public GetInstallationAcceptanceByIdHandler(IInstallationAcceptanceRepository repo) => _repo = repo;

    public async Task<InstallationAcceptanceDto?> Handle(GetInstallationAcceptanceByIdQuery q, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(q.Id, ct);
        return entity is null ? null : InstallationAcceptanceMapper.ToDto(entity);
    }
}

public sealed class ListPendingAcceptancesForInspectorHandler
    : IRequestHandler<ListPendingAcceptancesForInspectorQuery, IReadOnlyList<InstallationAcceptanceDto>>
{
    private readonly IInstallationAcceptanceRepository _repo;
    public ListPendingAcceptancesForInspectorHandler(IInstallationAcceptanceRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<InstallationAcceptanceDto>> Handle(ListPendingAcceptancesForInspectorQuery q, CancellationToken ct)
    {
        var entities = await _repo.ListByInspectorAsync(q.InspectorUserId, q.Status, ct);
        return entities.Select(InstallationAcceptanceMapper.ToDto).ToList();
    }
}

public sealed class ListPunchListItemsHandler
    : IRequestHandler<ListPunchListItemsQuery, IReadOnlyList<PunchListItemDto>>
{
    private readonly IPunchListRepository _repo;
    public ListPunchListItemsHandler(IPunchListRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<PunchListItemDto>> Handle(ListPunchListItemsQuery q, CancellationToken ct)
    {
        var entities = await _repo.ListByStatusAsync(q.Status, ct);
        return entities.Select(InstallationAcceptanceMapper.ToDto).ToList();
    }
}

public sealed class GetAcceptanceWithFullDetailsHandler
    : IRequestHandler<GetAcceptanceWithFullDetailsQuery, AcceptanceFullDetailsDto?>
{
    private readonly IInstallationAcceptanceRepository _acceptances;
    private readonly IPunchListRepository _punchList;

    public GetAcceptanceWithFullDetailsHandler(
        IInstallationAcceptanceRepository acceptances,
        IPunchListRepository punchList)
    {
        _acceptances = acceptances;
        _punchList = punchList;
    }

    public async Task<AcceptanceFullDetailsDto?> Handle(GetAcceptanceWithFullDetailsQuery q, CancellationToken ct)
    {
        var acceptance = await _acceptances.GetByIdAsync(q.Id, ct);
        if (acceptance is null) return null;
        var punch = await _punchList.ListByAcceptanceAsync(acceptance.Id, ct);
        return new AcceptanceFullDetailsDto(
            InstallationAcceptanceMapper.ToDto(acceptance),
            punch.Select(InstallationAcceptanceMapper.ToDto).ToList());
    }
}
