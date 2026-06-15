using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Warranty;

public class ListWarrantyContractsForCustomerHandler
    : IRequestHandler<ListWarrantyContractsForCustomerQuery, IReadOnlyList<WarrantyContractDto>>
{
    private readonly IWarrantyContractRepository _repo;
    public ListWarrantyContractsForCustomerHandler(IWarrantyContractRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<WarrantyContractDto>> Handle(ListWarrantyContractsForCustomerQuery q, CancellationToken ct)
        => (await _repo.ListByCustomerAsync(q.CustomerId, ct)).Select(WarrantyMapper.ToDto).ToList();
}

public class ListWarrantyContractsHandler
    : IRequestHandler<ListWarrantyContractsQuery, IReadOnlyList<WarrantyContractDto>>
{
    private readonly IWarrantyContractRepository _repo;
    public ListWarrantyContractsHandler(IWarrantyContractRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<WarrantyContractDto>> Handle(ListWarrantyContractsQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.Status, q.CustomerId, ct)).Select(WarrantyMapper.ToDto).ToList();
}

public class GetWarrantyContractByIdHandler
    : IRequestHandler<GetWarrantyContractByIdQuery, WarrantyContractDto?>
{
    private readonly IWarrantyContractRepository _repo;
    public GetWarrantyContractByIdHandler(IWarrantyContractRepository repo) => _repo = repo;

    public async Task<WarrantyContractDto?> Handle(GetWarrantyContractByIdQuery q, CancellationToken ct)
    {
        var contract = await _repo.GetByIdAsync(q.Id, ct);
        return contract is null ? null : WarrantyMapper.ToDto(contract);
    }
}

public class GetWarrantyContractByOrderIdHandler
    : IRequestHandler<GetWarrantyContractByOrderIdQuery, WarrantyContractDto?>
{
    private readonly IWarrantyContractRepository _repo;
    public GetWarrantyContractByOrderIdHandler(IWarrantyContractRepository repo) => _repo = repo;

    public async Task<WarrantyContractDto?> Handle(GetWarrantyContractByOrderIdQuery q, CancellationToken ct)
    {
        var contract = await _repo.GetByOrderIdAsync(q.OrderId, ct);
        return contract is null ? null : WarrantyMapper.ToDto(contract);
    }
}

public class ListExpiringWarrantyAlertsHandler
    : IRequestHandler<ListExpiringWarrantyAlertsQuery, IReadOnlyList<WarrantyExpiryAlertDto>>
{
    private readonly IWarrantyContractRepository _repo;
    public ListExpiringWarrantyAlertsHandler(IWarrantyContractRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<WarrantyExpiryAlertDto>> Handle(ListExpiringWarrantyAlertsQuery q, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var rows = await _repo.ListExpiringWithinDaysAsync(q.WithinDays, ct);
        return rows.Select(r => WarrantyMapper.ToExpiryAlert(r, now)).ToList();
    }
}

public class ListServiceTicketsHandler
    : IRequestHandler<ListServiceTicketsQuery, IReadOnlyList<ServiceTicketDto>>
{
    private readonly IServiceTicketRepository _repo;
    public ListServiceTicketsHandler(IServiceTicketRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ServiceTicketDto>> Handle(ListServiceTicketsQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.Status, q.Type, q.Priority, q.CustomerId, ct)).Select(WarrantyMapper.ToDto).ToList();
}

public class ListMyServiceTicketsHandler
    : IRequestHandler<ListMyServiceTicketsQuery, IReadOnlyList<ServiceTicketDto>>
{
    private readonly IServiceTicketRepository _repo;
    public ListMyServiceTicketsHandler(IServiceTicketRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ServiceTicketDto>> Handle(ListMyServiceTicketsQuery q, CancellationToken ct)
        => (await _repo.ListByCustomerAsync(q.CustomerId, ct)).Select(WarrantyMapper.ToDto).ToList();
}

public class ListMaintenanceSchedulesDueHandler
    : IRequestHandler<ListMaintenanceSchedulesDueQuery, IReadOnlyList<MaintenanceScheduleDto>>
{
    private readonly IMaintenanceScheduleRepository _repo;
    public ListMaintenanceSchedulesDueHandler(IMaintenanceScheduleRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<MaintenanceScheduleDto>> Handle(ListMaintenanceSchedulesDueQuery q, CancellationToken ct)
        => (await _repo.ListDueAsync(q.AsOfDate, ct)).Select(WarrantyMapper.ToDto).ToList();
}
