using System.Text.Json;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Custom;

internal static class CustomReportJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };
}

public sealed record PreviewCustomReportQuery(CustomReportDefinitionDto Definition) : IRequest<CustomReportPreviewDto>;

public sealed class PreviewCustomReportQueryHandler : IRequestHandler<PreviewCustomReportQuery, CustomReportPreviewDto>
{
    private readonly ICustomReportExecutor _executor;

    public PreviewCustomReportQueryHandler(ICustomReportExecutor executor)
    {
        _executor = executor;
    }

    public Task<CustomReportPreviewDto> Handle(PreviewCustomReportQuery request, CancellationToken cancellationToken) =>
        _executor.ExecuteAsync(request.Definition, cancellationToken);
}

public sealed record SaveCustomReportCommand(SaveCustomReportRequestDto Payload) : IRequest<CustomReportSummaryDto>;

public sealed class SaveCustomReportCommandHandler : IRequestHandler<SaveCustomReportCommand, CustomReportSummaryDto>
{
    private readonly IReportDefinitionRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserAccessor _currentUser;

    public SaveCustomReportCommandHandler(
        IReportDefinitionRepository repository,
        IUnitOfWork uow,
        ICurrentUserAccessor currentUser)
    {
        _repository = repository;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<CustomReportSummaryDto> Handle(SaveCustomReportCommand request, CancellationToken cancellationToken)
    {
        CustomReportValidator.Validate(request.Payload.Definition);

        var dimsJson = JsonSerializer.Serialize(request.Payload.Definition.Dimensions, CustomReportJson.Options);
        var measuresJson = JsonSerializer.Serialize(request.Payload.Definition.Measures, CustomReportJson.Options);
        var filtersJson = JsonSerializer.Serialize(request.Payload.Definition.Filters ?? Array.Empty<CustomReportFilterDto>(), CustomReportJson.Options);
        var sortJson = request.Payload.Definition.SortBy is null
            ? null
            : JsonSerializer.Serialize(request.Payload.Definition.SortBy, CustomReportJson.Options);

        var entity = new ReportDefinition(
            name: request.Payload.Name,
            entityType: request.Payload.Definition.EntityType,
            dimensionsJson: dimsJson,
            measuresJson: measuresJson,
            filtersJson: filtersJson,
            sortByJson: sortJson,
            limit: request.Payload.Definition.Limit,
            description: request.Payload.Description,
            createdByUserId: _currentUser.UserId);

        await _repository.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new CustomReportSummaryDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.EntityType.ToString(),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }
}

public sealed record ListCustomReportsQuery : IRequest<IReadOnlyList<CustomReportSummaryDto>>;

public sealed class ListCustomReportsQueryHandler : IRequestHandler<ListCustomReportsQuery, IReadOnlyList<CustomReportSummaryDto>>
{
    private readonly IReportDefinitionRepository _repository;

    public ListCustomReportsQueryHandler(IReportDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CustomReportSummaryDto>> Handle(ListCustomReportsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.ListAsync(cancellationToken);
        return rows.Select(r => new CustomReportSummaryDto(
            r.Id,
            r.Name,
            r.Description,
            r.EntityType.ToString(),
            r.CreatedAtUtc,
            r.UpdatedAtUtc)).ToList();
    }
}

public sealed record DeleteCustomReportCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteCustomReportCommandHandler : IRequestHandler<DeleteCustomReportCommand, bool>
{
    private readonly IReportDefinitionRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteCustomReportCommandHandler(IReportDefinitionRepository repository, IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteCustomReportCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null) return false;
        _repository.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record RunCustomReportQuery(Guid Id) : IRequest<ReportDocument?>;

public sealed class RunCustomReportQueryHandler : IRequestHandler<RunCustomReportQuery, ReportDocument?>
{
    private readonly IReportDefinitionRepository _repository;
    private readonly ICustomReportExecutor _executor;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public RunCustomReportQueryHandler(
        IReportDefinitionRepository repository,
        ICustomReportExecutor executor,
        ITenantRepository tenants,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _executor = executor;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument?> Handle(RunCustomReportQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null) return null;

        var def = new CustomReportDefinitionDto(
            entity.EntityType,
            JsonSerializer.Deserialize<List<string>>(entity.DimensionsJson, CustomReportJson.Options) ?? new List<string>(),
            JsonSerializer.Deserialize<List<CustomReportMeasureDto>>(entity.MeasuresJson, CustomReportJson.Options) ?? new List<CustomReportMeasureDto>(),
            JsonSerializer.Deserialize<List<CustomReportFilterDto>>(entity.FiltersJson, CustomReportJson.Options),
            entity.SortByJson is null ? null : JsonSerializer.Deserialize<CustomReportSortDto>(entity.SortByJson, CustomReportJson.Options),
            entity.Limit);

        var preview = await _executor.ExecuteAsync(def, cancellationToken);

        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        return CustomReportDocumentBuilder.Build(
            title: entity.Name,
            tenantName: tenant?.Name ?? string.Empty,
            tenantLegalName: tenant?.LegalName,
            currency: tenant?.DefaultCurrency ?? "TRY",
            locale: tenant?.LocaleCode ?? "tr-TR",
            preview: preview);
    }
}
