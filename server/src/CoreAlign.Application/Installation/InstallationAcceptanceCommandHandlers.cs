using CoreAlign.Application.Installation.Validation;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Installation;

public class StartInstallationAcceptanceHandler : IRequestHandler<StartInstallationAcceptanceCommand, InstallationAcceptanceDto>
{
    private readonly IInstallationAcceptanceService _service;
    public StartInstallationAcceptanceHandler(IInstallationAcceptanceService service) => _service = service;

    public async Task<InstallationAcceptanceDto> Handle(StartInstallationAcceptanceCommand c, CancellationToken ct)
    {
        var acceptance = await _service.StartAsync(c.WorkOrderId, c.InspectorUserId, ct);
        return InstallationAcceptanceMapper.ToDto(acceptance);
    }
}

public class UpdateChecklistItemHandler : IRequestHandler<UpdateChecklistItemCommand, InstallationAcceptanceDto>
{
    private readonly IInstallationAcceptanceService _service;
    private readonly IInstallationAcceptanceRepository _repo;
    public UpdateChecklistItemHandler(IInstallationAcceptanceService service, IInstallationAcceptanceRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<InstallationAcceptanceDto> Handle(UpdateChecklistItemCommand c, CancellationToken ct)
    {
        await _service.UpdateChecklistAsync(c.AcceptanceId, c.Category, c.ItemKey, c.Result, c.Notes, ct);
        var acceptance = await _repo.GetByIdAsync(c.AcceptanceId, ct)
            ?? throw new KeyNotFoundException($"Acceptance {c.AcceptanceId} not found.");
        return InstallationAcceptanceMapper.ToDto(acceptance);
    }
}

public class UploadAcceptancePhotoHandler : IRequestHandler<UploadAcceptancePhotoCommand, InstallationAcceptanceDto>
{
    private readonly IInstallationAcceptanceService _service;
    private readonly IInstallationAcceptanceRepository _repo;
    private readonly IFileOwnershipValidator _fileOwnership;

    public UploadAcceptancePhotoHandler(
        IInstallationAcceptanceService service,
        IInstallationAcceptanceRepository repo,
        IFileOwnershipValidator fileOwnership)
    {
        _service = service;
        _repo = repo;
        _fileOwnership = fileOwnership;
    }

    public async Task<InstallationAcceptanceDto> Handle(UploadAcceptancePhotoCommand c, CancellationToken ct)
    {
        if (!await _fileOwnership.ValidateAcceptanceFileAsync(c.FileId, c.AcceptanceId, ct))
        {
            throw new FileOwnershipViolationException();
        }
        await _service.AddPhotoAsync(c.AcceptanceId, c.FileId, ct);
        var acceptance = await _repo.GetByIdAsync(c.AcceptanceId, ct)
            ?? throw new KeyNotFoundException($"Acceptance {c.AcceptanceId} not found.");
        return InstallationAcceptanceMapper.ToDto(acceptance);
    }
}

public class CaptureCustomerSignatureHandler : IRequestHandler<CaptureCustomerSignatureCommand, InstallationAcceptanceDto>
{
    private readonly IInstallationAcceptanceService _service;
    private readonly IInstallationAcceptanceRepository _repo;
    private readonly IFileOwnershipValidator _fileOwnership;

    public CaptureCustomerSignatureHandler(
        IInstallationAcceptanceService service,
        IInstallationAcceptanceRepository repo,
        IFileOwnershipValidator fileOwnership)
    {
        _service = service;
        _repo = repo;
        _fileOwnership = fileOwnership;
    }

    public async Task<InstallationAcceptanceDto> Handle(CaptureCustomerSignatureCommand c, CancellationToken ct)
    {
        if (!await _fileOwnership.ValidateAcceptanceFileAsync(c.FileId, c.AcceptanceId, ct))
        {
            throw new FileOwnershipViolationException();
        }
        await _service.CaptureSignatureAsync(c.AcceptanceId, c.FileId, c.CustomerName, ct);
        var acceptance = await _repo.GetByIdAsync(c.AcceptanceId, ct)
            ?? throw new KeyNotFoundException($"Acceptance {c.AcceptanceId} not found.");
        return InstallationAcceptanceMapper.ToDto(acceptance);
    }
}

public class AcceptInstallationHandler : IRequestHandler<AcceptInstallationCommand, InstallationAcceptanceDto>
{
    private readonly IInstallationAcceptanceService _service;
    private readonly IInstallationAcceptanceRepository _repo;
    public AcceptInstallationHandler(IInstallationAcceptanceService service, IInstallationAcceptanceRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<InstallationAcceptanceDto> Handle(AcceptInstallationCommand c, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(c.IdempotencyKey))
        {
            var existing = await _repo.GetByAcceptIdempotencyKeyAsync(c.IdempotencyKey, ct);
            if (existing is not null && existing.Status == InstallationAcceptanceStatus.Accepted)
            {
                return InstallationAcceptanceMapper.ToDto(existing);
            }
        }

        await _service.AcceptAsync(c.AcceptanceId, c.IdempotencyKey, ct);
        var acceptance = await _repo.GetByIdAsync(c.AcceptanceId, ct)
            ?? throw new KeyNotFoundException($"Acceptance {c.AcceptanceId} not found.");
        return InstallationAcceptanceMapper.ToDto(acceptance);
    }
}

public class RejectInstallationHandler : IRequestHandler<RejectInstallationCommand, InstallationAcceptanceDto>
{
    private readonly IInstallationAcceptanceService _service;
    private readonly IInstallationAcceptanceRepository _repo;
    public RejectInstallationHandler(IInstallationAcceptanceService service, IInstallationAcceptanceRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<InstallationAcceptanceDto> Handle(RejectInstallationCommand c, CancellationToken ct)
    {
        await _service.RejectAsync(c.AcceptanceId, c.Reason, ct);
        var acceptance = await _repo.GetByIdAsync(c.AcceptanceId, ct)
            ?? throw new KeyNotFoundException($"Acceptance {c.AcceptanceId} not found.");
        return InstallationAcceptanceMapper.ToDto(acceptance);
    }
}

public class AddPunchListItemHandler : IRequestHandler<AddPunchListItemCommand, PunchListItemDto>
{
    private readonly IInstallationAcceptanceService _service;
    public AddPunchListItemHandler(IInstallationAcceptanceService service) => _service = service;

    public async Task<PunchListItemDto> Handle(AddPunchListItemCommand c, CancellationToken ct)
    {
        var item = await _service.AddPunchListAsync(c.AcceptanceId, c.Description, c.Severity, ct);
        return InstallationAcceptanceMapper.ToDto(item);
    }
}

public class ResolvePunchListItemHandler : IRequestHandler<ResolvePunchListItemCommand, PunchListItemDto>
{
    private readonly IInstallationAcceptanceService _service;
    private readonly IPunchListRepository _repo;
    public ResolvePunchListItemHandler(IInstallationAcceptanceService service, IPunchListRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    public async Task<PunchListItemDto> Handle(ResolvePunchListItemCommand c, CancellationToken ct)
    {
        await _service.ResolvePunchListAsync(c.PunchItemId, c.ResolutionNotes, ct);
        var item = await _repo.GetByIdAsync(c.PunchItemId, ct)
            ?? throw new KeyNotFoundException($"PunchListItem {c.PunchItemId} not found.");
        return InstallationAcceptanceMapper.ToDto(item);
    }
}
