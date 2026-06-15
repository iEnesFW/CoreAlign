using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Commands;

public record CreateGlassProjectCommand(CreateGlassProjectDto Data) : IRequest<GlassProjectDto>, ITransactionalRequest;
public record UpdateGlassProjectHeaderCommand(Guid Id, UpdateGlassProjectHeaderDto Data) : IRequest<GlassProjectDto>, ITransactionalRequest;
public record AssignGlassProjectTeamCommand(Guid Id, AssignProjectTeamDto Data) : IRequest<GlassProjectDto>, ITransactionalRequest;
public record TransitionGlassProjectStatusCommand(Guid Id, TransitionProjectStatusDto Data) : IRequest<GlassProjectDto>, ITransactionalRequest;
public record DeleteGlassProjectCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;
public record CloneGlassProjectCommand(Guid SourceProjectId, string NewProjectName, Guid? CustomerId) : IRequest<GlassProjectDto>, ITransactionalRequest;
public record ConfigureEnclosureCommand(Guid ProjectId, ConfigureEnclosureDto Data) : IRequest<GlassProjectDto>, ITransactionalRequest;

public record AddRunCommand(Guid ProjectId, AddRunDto Data) : IRequest<GlassProjectRunDto>, ITransactionalRequest;
public record UpdateRunCommand(Guid ProjectId, Guid RunId, UpdateRunDto Data) : IRequest<GlassProjectRunDto>, ITransactionalRequest;
public record RemoveRunCommand(Guid ProjectId, Guid RunId) : IRequest<Unit>, ITransactionalRequest;
public record BulkRebalancePanelsCommand(Guid ProjectId, Guid RunId, BulkRebalancePanelsDto Data) : IRequest<GlassProjectRunDto>, ITransactionalRequest;

public record AddRunConnectionCommand(Guid ProjectId, AddRunConnectionDto Data) : IRequest<RunConnectionDto>, ITransactionalRequest;
public record UpdateRunConnectionCommand(Guid ProjectId, Guid ConnectionId, UpdateRunConnectionDto Data) : IRequest<RunConnectionDto>, ITransactionalRequest;
public record RemoveRunConnectionCommand(Guid ProjectId, Guid ConnectionId) : IRequest<Unit>, ITransactionalRequest;

public record AddPanelCommand(Guid ProjectId, Guid RunId, AddPanelDto Data) : IRequest<GlassProjectPanelDto>, ITransactionalRequest;
public record UpdatePanelCommand(Guid ProjectId, Guid RunId, Guid PanelId, UpdatePanelDto Data) : IRequest<GlassProjectPanelDto>, ITransactionalRequest;
public record RemovePanelCommand(Guid ProjectId, Guid RunId, Guid PanelId) : IRequest<Unit>, ITransactionalRequest;

public record SaveSceneCommand(Guid ProjectId, SaveSceneDto Data) : IRequest<SceneVersionDto>, ITransactionalRequest;

public record ValidateProjectCommand(Guid ProjectId) : IRequest<GlassProjectValidationResultDto>;
