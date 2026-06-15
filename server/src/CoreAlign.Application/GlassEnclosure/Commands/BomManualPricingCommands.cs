using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Commands;

public record OverrideBomLinePriceCommand(Guid ProjectId, Guid LineId, decimal? UnitPriceOverride)
    : IRequest<BOMSummaryDto>, ITransactionalRequest;

public record AddManualBomLineCommand(Guid ProjectId, AddManualBomLineDto Data)
    : IRequest<BOMSummaryDto>, ITransactionalRequest;

public record DeleteManualBomLineCommand(Guid ProjectId, Guid LineId)
    : IRequest<BOMSummaryDto>, ITransactionalRequest;

public record PushBomLinePriceToCatalogCommand(Guid ProjectId, Guid LineId)
    : IRequest<PushBomLinePriceResultDto>, ITransactionalRequest;
