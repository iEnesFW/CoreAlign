using CoreAlign.Application.Tags.DTOs;
using MediatR;

namespace CoreAlign.Application.Tags.Queries;

public record ListTagsQuery(bool? IsActive = null) : IRequest<IReadOnlyList<TagDto>>;
