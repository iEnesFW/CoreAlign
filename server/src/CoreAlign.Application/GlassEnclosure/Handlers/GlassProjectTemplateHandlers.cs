using System.Text.Json;
using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Mapping;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class SaveGlassProjectTemplateCommandHandler
    : IRequestHandler<SaveGlassProjectTemplateCommand, GlassProjectTemplateDto>
{
    private readonly IGlassProjectTemplateRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;

    public SaveGlassProjectTemplateCommandHandler(
        IGlassProjectTemplateRepository repo,
        ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<GlassProjectTemplateDto> Handle(
        SaveGlassProjectTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var (wallCount, slabCount, runCount) = CountEntitiesOrThrow(request.Data.PayloadJson);
        var userId = _currentUser.UserId ?? Guid.Empty;

        var template = new GlassProjectTemplate(
            request.Data.Name,
            userId,
            request.Data.PayloadJson,
            wallCount,
            slabCount,
            runCount);

        await _repo.AddAsync(template, cancellationToken);
        return GlassProjectTemplateMappers.ToDto(template);
    }

    private static (int WallCount, int SlabCount, int RunCount) CountEntitiesOrThrow(string payloadJson)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException)
        {
            throw new GlassProjectTemplateInvalidException("Template payload is not valid JSON.");
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new GlassProjectTemplateInvalidException("Template payload must be a JSON object.");

            var walls = ArrayLength(doc.RootElement, "walls");
            var slabs = ArrayLength(doc.RootElement, "slabs");
            var runs = ArrayLength(doc.RootElement, "runs");

            if (walls + slabs + runs == 0)
                throw new GlassProjectTemplateInvalidException("Template must contain at least one wall, slab or run.");

            return (walls, slabs, runs);
        }
    }

    private static int ArrayLength(JsonElement obj, string propertyName) =>
        obj.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Array
            ? element.GetArrayLength()
            : 0;
}

public class DeleteGlassProjectTemplateCommandHandler
    : IRequestHandler<DeleteGlassProjectTemplateCommand, Unit>
{
    private readonly IGlassProjectTemplateRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;

    public DeleteGlassProjectTemplateCommandHandler(
        IGlassProjectTemplateRepository repo,
        ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(
        DeleteGlassProjectTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (template is null || template.CreatedByUserId != (_currentUser.UserId ?? Guid.Empty))
            throw new GlassProjectTemplateNotFoundException();

        _repo.Remove(template);
        return Unit.Value;
    }
}

public class GetMyGlassProjectTemplatesQueryHandler
    : IRequestHandler<GetMyGlassProjectTemplatesQuery, IReadOnlyList<GlassProjectTemplateSummaryDto>>
{
    private readonly IGlassProjectTemplateRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetMyGlassProjectTemplatesQueryHandler(
        IGlassProjectTemplateRepository repo,
        ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<GlassProjectTemplateSummaryDto>> Handle(
        GetMyGlassProjectTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var rows = await _repo.ListByUserAsync(userId, cancellationToken);
        return rows.Select(GlassProjectTemplateMappers.ToSummary).ToList();
    }
}

public class GetGlassProjectTemplateByIdQueryHandler
    : IRequestHandler<GetGlassProjectTemplateByIdQuery, GlassProjectTemplateDto?>
{
    private readonly IGlassProjectTemplateRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetGlassProjectTemplateByIdQueryHandler(
        IGlassProjectTemplateRepository repo,
        ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<GlassProjectTemplateDto?> Handle(
        GetGlassProjectTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (template is null || template.CreatedByUserId != (_currentUser.UserId ?? Guid.Empty))
            return null;

        return GlassProjectTemplateMappers.ToDto(template);
    }
}
