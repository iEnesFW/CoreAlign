using CoreAlign.Application.B2B;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Identity.Locale;

public sealed class SetPreferredLocaleHandler : IRequestHandler<SetPreferredLocaleCommand, PreferredLocaleDto>
{
    private static readonly HashSet<string> AllowedLocales = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "tr", "ar", "de", "ru",
    };

    private readonly IUserRepository _users;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;

    public SetPreferredLocaleHandler(
        IUserRepository users,
        ICurrentUserAccessor currentUser,
        IUnitOfWork uow)
    {
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<PreferredLocaleDto> Handle(SetPreferredLocaleCommand request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request.Locale);
        var userId = _currentUser.UserIdOrThrow();
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UserNotFoundException();

        user.PreferredLocale = normalized;
        user.UpdatedAtUtc = DateTime.UtcNow;
        _users.Update(user);

        await _uow.SaveChangesAsync(cancellationToken);

        return new PreferredLocaleDto(normalized);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "en";
        }
        var code = value.Trim().Split('-')[0].ToLowerInvariant();
        return AllowedLocales.Contains(code) ? code : "en";
    }
}
