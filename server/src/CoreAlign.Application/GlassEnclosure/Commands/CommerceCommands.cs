using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Commands;

public record GenerateShareTokenCommand(
    Guid ProjectId,
    GenerateShareTokenDto Data,
    bool ForceWithShortage = false,
    bool ForceWithStaleBom = false) : IRequest<ShareTokenInfoDto>, ITransactionalRequest;

public record GetShareViewerProjectQuery(string Token, string IpHash) : IRequest<ShareViewerProjectDto?>;

public record RecordShareViewerActionCommand(string Token, ShareViewerActionDto Data) : IRequest<ShareViewerActionResultDto>, ITransactionalRequest;

public record GetShareTokensQuery(Guid ProjectId) : IRequest<IReadOnlyList<ShareTokenInfoDto>>;

public record ConvertProjectToOrderCommand(
    Guid ProjectId,
    bool ForceConvertWithShortage = false,
    bool ForceWithStaleBom = false,
    IReadOnlyDictionary<Guid, Guid>? SubstituteSelections = null) : IRequest<ConvertProjectToOrderResultDto>, ITransactionalRequest;

public record ReleaseToProductionCommand(Guid ProjectId, ReleaseToProductionDto Data) : IRequest<GlassWorkOrderDto>, ITransactionalRequest;

public record GetWorkOrdersByProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<GlassWorkOrderDto>>;

public record UpdateWorkOrderStatusCommand(Guid WorkOrderId, string Status) : IRequest<GlassWorkOrderDto>, ITransactionalRequest;

public record RecordWorkOrderDefectCommand(Guid WorkOrderId, string DefectNotes) : IRequest<GlassWorkOrderDto>, ITransactionalRequest;

public record GetNotificationHistoryQuery(Guid ProjectId) : IRequest<IReadOnlyList<NotificationLogDto>>;
