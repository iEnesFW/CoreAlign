using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Commands;

public record CreateFieldSurveyCommand(CreateFieldSurveyDto Data) : IRequest<FieldSurveyDto>, ITransactionalRequest;

public record UpdateFieldSurveyCommand(Guid Id, UpdateFieldSurveyDto Data) : IRequest<FieldSurveyDto>, ITransactionalRequest;

public record SubmitFieldSurveyCommand(Guid Id) : IRequest<FieldSurveyDto>, ITransactionalRequest;

public record ApproveFieldSurveyCommand(Guid Id, ApproveFieldSurveyDto Data) : IRequest<FieldSurveyApplyResultDto?>, ITransactionalRequest;

public record RejectFieldSurveyCommand(Guid Id, RejectFieldSurveyDto Data) : IRequest<FieldSurveyDto>, ITransactionalRequest;

public record ApplyFieldSurveyCommand(Guid Id) : IRequest<FieldSurveyApplyResultDto>, ITransactionalRequest;

public record DeleteFieldSurveyCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record GetFieldSurveysByProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<FieldSurveyDto>>;

public record GetFieldSurveyByIdQuery(Guid Id) : IRequest<FieldSurveyDto?>;
