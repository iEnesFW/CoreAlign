using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Queries;

public record GetGlassProjectsQuery(
    string? Search,
    GlassProjectStatus? Status,
    Guid? CustomerId,
    Guid? AssignedDesignerUserId,
    Guid? AssignedSalespersonUserId,
    int Page,
    int PageSize) : IRequest<PagedResult<GlassProjectListItemDto>>;

public record GetGlassProjectByIdQuery(Guid Id) : IRequest<GlassProjectDto?>;

public record GetSceneLatestQuery(Guid ProjectId) : IRequest<SceneLatestDto?>;

public record GetSceneVersionsQuery(Guid ProjectId, int Limit = 50) : IRequest<IReadOnlyList<SceneVersionDto>>;

public record GetSceneByVersionQuery(Guid ProjectId, int Version) : IRequest<SceneLatestDto?>;
