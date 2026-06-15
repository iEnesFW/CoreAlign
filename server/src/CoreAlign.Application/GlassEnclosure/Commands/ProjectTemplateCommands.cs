using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Commands;

public record CreateProjectFromTemplateCommand(CreateProjectFromTemplateDto Data)
    : IRequest<GlassProjectDto>, ITransactionalRequest;
