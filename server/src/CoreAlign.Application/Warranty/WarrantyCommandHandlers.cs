using CoreAlign.Application.B2B;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Warranty;

public class CreateWarrantyContractHandler : IRequestHandler<CreateWarrantyContractCommand, WarrantyContractDto>
{
    private readonly IWarrantyContractService _service;
    private readonly IWarrantyContractRepository _repo;

    public CreateWarrantyContractHandler(IWarrantyContractService service, IWarrantyContractRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<WarrantyContractDto> Handle(CreateWarrantyContractCommand c, CancellationToken ct)
    {
        var contract = await _service.CreateAsync(
            c.OrderId,
            c.CustomerId,
            c.CoverageType,
            c.WarrantyMonths,
            string.IsNullOrWhiteSpace(c.TermsJson) ? "{}" : c.TermsJson,
            c.ProductId,
            c.WorkOrderId,
            c.InvoiceId,
            c.Notes,
            ct);
        return WarrantyMapper.ToDto(contract);
    }
}

public class ActivateWarrantyContractHandler : IRequestHandler<ActivateWarrantyContractCommand, WarrantyContractDto>
{
    private readonly IWarrantyContractService _service;
    private readonly IWarrantyContractRepository _repo;

    public ActivateWarrantyContractHandler(IWarrantyContractService service, IWarrantyContractRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<WarrantyContractDto> Handle(ActivateWarrantyContractCommand c, CancellationToken ct)
    {
        await _service.ActivateAsync(c.Id, c.StartDate, ct);
        var contract = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException($"Warranty contract {c.Id} not found.");
        return WarrantyMapper.ToDto(contract);
    }
}

public class ExtendWarrantyContractHandler : IRequestHandler<ExtendWarrantyContractCommand, WarrantyContractDto>
{
    private readonly IWarrantyContractService _service;
    private readonly IWarrantyContractRepository _repo;

    public ExtendWarrantyContractHandler(IWarrantyContractService service, IWarrantyContractRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<WarrantyContractDto> Handle(ExtendWarrantyContractCommand c, CancellationToken ct)
    {
        await _service.ExtendAsync(c.Id, c.MonthsAdded, c.Reason, ct);
        var contract = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException($"Warranty contract {c.Id} not found.");
        return WarrantyMapper.ToDto(contract);
    }
}

public class CancelWarrantyContractHandler : IRequestHandler<CancelWarrantyContractCommand, WarrantyContractDto>
{
    private readonly IWarrantyContractService _service;
    private readonly IWarrantyContractRepository _repo;

    public CancelWarrantyContractHandler(IWarrantyContractService service, IWarrantyContractRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<WarrantyContractDto> Handle(CancelWarrantyContractCommand c, CancellationToken ct)
    {
        await _service.CancelAsync(c.Id, c.Reason, ct);
        var contract = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException($"Warranty contract {c.Id} not found.");
        return WarrantyMapper.ToDto(contract);
    }
}

public class SuspendWarrantyContractHandler : IRequestHandler<SuspendWarrantyContractCommand, WarrantyContractDto>
{
    private readonly IWarrantyContractService _service;
    private readonly IWarrantyContractRepository _repo;

    public SuspendWarrantyContractHandler(IWarrantyContractService service, IWarrantyContractRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<WarrantyContractDto> Handle(SuspendWarrantyContractCommand c, CancellationToken ct)
    {
        await _service.SuspendAsync(c.Id, c.Reason, ct);
        var contract = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException($"Warranty contract {c.Id} not found.");
        return WarrantyMapper.ToDto(contract);
    }
}

public class ResumeWarrantyContractHandler : IRequestHandler<ResumeWarrantyContractCommand, WarrantyContractDto>
{
    private readonly IWarrantyContractService _service;
    private readonly IWarrantyContractRepository _repo;

    public ResumeWarrantyContractHandler(IWarrantyContractService service, IWarrantyContractRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<WarrantyContractDto> Handle(ResumeWarrantyContractCommand c, CancellationToken ct)
    {
        await _service.ResumeAsync(c.Id, ct);
        var contract = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException($"Warranty contract {c.Id} not found.");
        return WarrantyMapper.ToDto(contract);
    }
}

public class CreateServiceTicketHandler : IRequestHandler<CreateServiceTicketCommand, ServiceTicketDto>
{
    private readonly IServiceTicketService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICustomerUserRepository _customerUsers;
    private readonly ITenantContext _tenant;

    public CreateServiceTicketHandler(
        IServiceTicketService service,
        ICurrentUserAccessor currentUser,
        ICustomerUserRepository customerUsers,
        ITenantContext tenant)
    {
        _service = service;
        _currentUser = currentUser;
        _customerUsers = customerUsers;
        _tenant = tenant;
    }

    public async Task<ServiceTicketDto> Handle(CreateServiceTicketCommand c, CancellationToken ct)
    {
        var userId = _currentUser.UserIdOrThrow();
        var tenantId = _tenant.RequireTenantId();
        var isPortalUser = await _customerUsers.AnyActiveForUserAsync(userId, tenantId, ct);
        if (isPortalUser)
        {
            var hasOwnership = await _customerUsers.HasActiveOwnershipAsync(userId, c.CustomerId, ct);
            if (!hasOwnership) throw new ServiceTicketCustomerOwnershipException();
        }

        var ticket = await _service.OpenAsync(
            c.CustomerId,
            c.Type,
            c.Priority,
            c.Title,
            c.DescriptionMd,
            c.WarrantyContractId,
            ct);
        return WarrantyMapper.ToDto(ticket);
    }
}

public class AssignServiceTicketHandler : IRequestHandler<AssignServiceTicketCommand, ServiceTicketDto>
{
    private readonly IServiceTicketService _service;
    private readonly IServiceTicketRepository _repo;

    public AssignServiceTicketHandler(IServiceTicketService service, IServiceTicketRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<ServiceTicketDto> Handle(AssignServiceTicketCommand c, CancellationToken ct)
    {
        await _service.AssignAsync(c.Id, c.UserId, ct);
        var ticket = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException($"Service ticket {c.Id} not found.");
        return WarrantyMapper.ToDto(ticket);
    }
}

public class ResolveServiceTicketHandler : IRequestHandler<ResolveServiceTicketCommand, ServiceTicketDto>
{
    private readonly IServiceTicketService _service;
    private readonly IServiceTicketRepository _repo;

    public ResolveServiceTicketHandler(IServiceTicketService service, IServiceTicketRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<ServiceTicketDto> Handle(ResolveServiceTicketCommand c, CancellationToken ct)
    {
        await _service.ResolveAsync(c.Id, c.ResolutionNotesMd, c.WorkOrderId, c.ChargeableAmount, ct);
        var ticket = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException($"Service ticket {c.Id} not found.");
        return WarrantyMapper.ToDto(ticket);
    }
}

public class CreateMaintenanceScheduleHandler : IRequestHandler<CreateMaintenanceScheduleCommand, MaintenanceScheduleDto>
{
    private readonly IMaintenanceScheduleService _service;

    public CreateMaintenanceScheduleHandler(IMaintenanceScheduleService service) => _service = service;

    public async Task<MaintenanceScheduleDto> Handle(CreateMaintenanceScheduleCommand c, CancellationToken ct)
    {
        var schedule = await _service.CreateAsync(
            c.WarrantyContractId,
            c.Type,
            c.NextDueDate,
            c.RecurrencePattern,
            c.Notes,
            ct);
        return WarrantyMapper.ToDto(schedule);
    }
}

public class CompleteScheduledMaintenanceHandler : IRequestHandler<CompleteScheduledMaintenanceCommand, MaintenanceScheduleDto>
{
    private readonly IMaintenanceScheduleService _service;
    private readonly IMaintenanceScheduleRepository _repo;

    public CompleteScheduledMaintenanceHandler(IMaintenanceScheduleService service, IMaintenanceScheduleRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<MaintenanceScheduleDto> Handle(CompleteScheduledMaintenanceCommand c, CancellationToken ct)
    {
        await _service.CompleteAsync(c.Id, c.CompletedAtUtc, ct);
        var schedule = await _repo.GetByIdAsync(c.Id, ct)
            ?? throw new KeyNotFoundException($"Maintenance schedule {c.Id} not found.");
        return WarrantyMapper.ToDto(schedule);
    }
}
