using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Bom;

public record BomLineBackfillCommand : IRequest<BomLineBackfillResult>, ITransactionalRequest;

public sealed record BomLineBackfillResult(
    int TotalScanned,
    int AlreadyLinked,
    int Linked,
    int CouldNotLink,
    IReadOnlyList<BomLineBackfillIssue> Issues);

public sealed record BomLineBackfillIssue(
    Guid BomLineId,
    string LineKind,
    Guid? RefId,
    string ReasonKey);
