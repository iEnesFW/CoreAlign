using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Commands;

public record SaveGlassProjectTemplateCommand(SaveGlassProjectTemplateDto Data)
    : IRequest<GlassProjectTemplateDto>, ITransactionalRequest;

public record DeleteGlassProjectTemplateCommand(Guid Id)
    : IRequest<Unit>, ITransactionalRequest;
