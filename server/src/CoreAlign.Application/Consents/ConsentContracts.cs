using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Consents;

public record ConsentDto(
    Guid Id,
    Guid? UserId,
    string Purpose,
    string Version,
    DateTime CapturedAtUtc,
    DateTime? WithdrawnAtUtc);

public record CaptureConsentCommand(
    string Purpose,
    string Version,
    bool Given,
    string? Fingerprint,
    string? IpAddress = null,
    string? UserAgent = null)
    : IRequest<ConsentDto>, ITransactionalRequest;

public record ListMyConsentsQuery() : IRequest<IReadOnlyList<ConsentDto>>;

public record WithdrawConsentCommand(Guid ConsentId)
    : IRequest<ConsentDto>, ITransactionalRequest;
