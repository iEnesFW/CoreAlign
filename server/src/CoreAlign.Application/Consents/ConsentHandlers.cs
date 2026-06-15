using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Consents;

public class CaptureConsentHandler : IRequestHandler<CaptureConsentCommand, ConsentDto>
{
    private const int FingerprintMaxLength = 64;
    private const int IpMaxLength = 45;
    private const int UserAgentMaxLength = 256;

    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserConsentRepository _consents;

    public CaptureConsentHandler(ICurrentUserAccessor currentUser, IUserConsentRepository consents)
    {
        _currentUser = currentUser;
        _consents = consents;
    }

    public async Task<ConsentDto> Handle(CaptureConsentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var fingerprint = Truncate(request.Fingerprint, FingerprintMaxLength);
        var capturedAt = DateTime.UtcNow;

        var consent = new UserConsent(
            userId,
            fingerprint,
            request.Purpose,
            request.Version,
            capturedAt,
            MaskIpAddress(request.IpAddress),
            Truncate(request.UserAgent, UserAgentMaxLength));

        if (!request.Given)
        {
            consent.Withdraw(capturedAt);
        }

        await _consents.AddAsync(consent, cancellationToken);

        return Map(consent);
    }

    internal static ConsentDto Map(UserConsent c) =>
        new(c.Id, c.UserId, c.Purpose, c.Version, c.CapturedAtUtc, c.WithdrawnAtUtc);

    internal static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    internal static string? MaskIpAddress(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        var trimmed = ip.Trim();
        if (trimmed.Length > IpMaxLength) trimmed = trimmed[..IpMaxLength];
        if (trimmed.Contains(':'))
        {
            var idx = trimmed.IndexOf(':');
            var head = trimmed[..idx];
            return $"{head}::/64";
        }
        var parts = trimmed.Split('.');
        if (parts.Length != 4) return trimmed;
        return $"{parts[0]}.{parts[1]}.{parts[2]}.0/24";
    }
}

public class ListMyConsentsHandler : IRequestHandler<ListMyConsentsQuery, IReadOnlyList<ConsentDto>>
{
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserConsentRepository _consents;

    public ListMyConsentsHandler(ICurrentUserAccessor currentUser, IUserConsentRepository consents)
    {
        _currentUser = currentUser;
        _consents = consents;
    }

    public async Task<IReadOnlyList<ConsentDto>> Handle(ListMyConsentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrThrow();
        var rows = await _consents.ListByUserAsync(userId, cancellationToken);
        return rows.Select(CaptureConsentHandler.Map).ToList();
    }
}

public class WithdrawConsentHandler : IRequestHandler<WithdrawConsentCommand, ConsentDto>
{
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserConsentRepository _consents;

    public WithdrawConsentHandler(ICurrentUserAccessor currentUser, IUserConsentRepository consents)
    {
        _currentUser = currentUser;
        _consents = consents;
    }

    public async Task<ConsentDto> Handle(WithdrawConsentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrThrow();
        var consent = await _consents.GetByIdAsync(request.ConsentId, cancellationToken)
            ?? throw new ConsentNotFoundException();

        if (consent.UserId != userId)
        {
            throw new ConsentNotFoundException();
        }

        consent.Withdraw(DateTime.UtcNow);
        return CaptureConsentHandler.Map(consent);
    }
}

public class ConsentNotFoundException : Exception
{
    public ConsentNotFoundException() : base("Consent record not found.") { }
}
