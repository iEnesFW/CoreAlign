using CoreAlign.Application.Common;
using CoreAlign.Application.Tags.DTOs;
using MediatR;

namespace CoreAlign.Application.Tags.Commands;

public record CreateTagCommand(string Name, string? ColorHex = null)
    : IRequest<TagDto>, ITransactionalRequest;

public record UpdateTagCommand(Guid Id, string Name, string? ColorHex, bool IsActive)
    : IRequest<TagDto>, ITransactionalRequest;

public record DeleteTagCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
