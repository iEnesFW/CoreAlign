using System.Net.Mail;
using System.Text.Json;
using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Schedules;

internal static class ScheduleJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}

public sealed class ScheduleValidationException : CoreAlign.Domain.Exceptions.DomainException
{
    public ScheduleValidationException(string message) : base(message) { }
}

internal static class ScheduleMapper
{
    public static ReportScheduleDto Map(ReportSchedule s)
    {
        var recipients = JsonSerializer.Deserialize<List<string>>(s.RecipientsJson, ScheduleJson.Options) ?? new List<string>();
        return new ReportScheduleDto(
            s.Id,
            s.Name,
            s.ReportKey,
            s.CustomReportDefinitionId,
            s.Frequency.ToString(),
            s.CronExpression,
            recipients,
            s.Format.ToString(),
            s.FiltersJson,
            s.IsActive,
            s.NextRunAtUtc,
            s.LastRunAtUtc,
            s.LastRunStatus,
            s.LastRunError);
    }

    public static void ValidateRecipients(IReadOnlyList<string> recipients)
    {
        if (recipients is null || recipients.Count == 0)
        {
            throw new ScheduleValidationException("At least one recipient email is required.");
        }
        foreach (var r in recipients)
        {
            if (!MailAddress.TryCreate(r, out _))
            {
                throw new ScheduleValidationException($"Invalid email address '{r}'.");
            }
        }
    }

    public static DateTime ResolveStart(DateTime? requested, ReportFrequency frequency)
    {
        var now = DateTime.UtcNow;
        if (requested.HasValue)
        {
            var resolved = DateTime.SpecifyKind(requested.Value, DateTimeKind.Utc);
            return resolved >= now ? resolved : ReportSchedule.ComputeNextRunAtUtc(frequency, now);
        }
        return ReportSchedule.ComputeNextRunAtUtc(frequency, now);
    }
}

public sealed record CreateReportScheduleCommand(CreateReportScheduleRequestDto Payload) : IRequest<ReportScheduleDto>;

public sealed class CreateReportScheduleCommandHandler : IRequestHandler<CreateReportScheduleCommand, ReportScheduleDto>
{
    private readonly IReportScheduleRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateReportScheduleCommandHandler(
        IReportScheduleRepository repository,
        IUnitOfWork uow,
        ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ReportScheduleDto> Handle(CreateReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        ScheduleMapper.ValidateRecipients(payload.Recipients);
        if (string.IsNullOrWhiteSpace(payload.ReportKey) && payload.CustomReportDefinitionId is null)
        {
            throw new ScheduleValidationException("Either ReportKey or CustomReportDefinitionId must be provided.");
        }

        var recipientsJson = JsonSerializer.Serialize(payload.Recipients, ScheduleJson.Options);
        var entity = new ReportSchedule(
            name: payload.Name,
            reportKey: payload.ReportKey ?? string.Empty,
            customReportDefinitionId: payload.CustomReportDefinitionId,
            frequency: payload.Frequency,
            cronExpression: payload.CronExpression,
            recipientsJson: recipientsJson,
            format: payload.Format,
            filtersJson: string.IsNullOrWhiteSpace(payload.FiltersJson) ? "{}" : payload.FiltersJson!,
            nextRunAtUtc: ScheduleMapper.ResolveStart(payload.StartAtUtc, payload.Frequency),
            createdByUserId: _currentUser.UserId);
        await _repository.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ScheduleMapper.Map(entity);
    }
}

public sealed record UpdateReportScheduleCommand(Guid Id, UpdateReportScheduleRequestDto Payload) : IRequest<ReportScheduleDto?>;

public sealed class UpdateReportScheduleCommandHandler : IRequestHandler<UpdateReportScheduleCommand, ReportScheduleDto?>
{
    private readonly IReportScheduleRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateReportScheduleCommandHandler(IReportScheduleRepository repository, IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<ReportScheduleDto?> Handle(UpdateReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null) return null;
        var payload = request.Payload;
        ScheduleMapper.ValidateRecipients(payload.Recipients);
        if (string.IsNullOrWhiteSpace(payload.ReportKey) && payload.CustomReportDefinitionId is null)
        {
            throw new ScheduleValidationException("Either ReportKey or CustomReportDefinitionId must be provided.");
        }
        var recipientsJson = JsonSerializer.Serialize(payload.Recipients, ScheduleJson.Options);
        entity.Update(
            payload.Name,
            payload.ReportKey ?? string.Empty,
            payload.CustomReportDefinitionId,
            payload.Frequency,
            payload.CronExpression,
            recipientsJson,
            payload.Format,
            string.IsNullOrWhiteSpace(payload.FiltersJson) ? "{}" : payload.FiltersJson!);
        if (payload.IsActive.HasValue)
        {
            if (payload.IsActive.Value) entity.Activate();
            else entity.Deactivate();
        }
        _repository.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return ScheduleMapper.Map(entity);
    }
}

public sealed record DeleteReportScheduleCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteReportScheduleCommandHandler : IRequestHandler<DeleteReportScheduleCommand, bool>
{
    private readonly IReportScheduleRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteReportScheduleCommandHandler(IReportScheduleRepository repository, IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null) return false;
        _repository.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record ListReportSchedulesQuery : IRequest<IReadOnlyList<ReportScheduleDto>>;

public sealed class ListReportSchedulesQueryHandler : IRequestHandler<ListReportSchedulesQuery, IReadOnlyList<ReportScheduleDto>>
{
    private readonly IReportScheduleRepository _repository;

    public ListReportSchedulesQueryHandler(IReportScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ReportScheduleDto>> Handle(ListReportSchedulesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.ListAsync(cancellationToken);
        return rows.Select(ScheduleMapper.Map).ToList();
    }
}
