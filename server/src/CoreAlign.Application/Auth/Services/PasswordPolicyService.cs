using System.Text;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Auth.Services;

public sealed class PasswordPolicyService : IPasswordPolicyService
{
    public const int MinLengthStandard = 12;
    public const int MinLengthTenantAdmin = 15;
    public const int MaxLength = 72;
    public const int HistoryDepth = 5;

    private readonly IPasswordHistoryRepository _history;
    private readonly IPasswordHasher _hasher;
    private readonly IPwnedPasswordsService _pwned;

    public PasswordPolicyService(
        IPasswordHistoryRepository history,
        IPasswordHasher hasher,
        IPwnedPasswordsService pwned)
    {
        _history = history;
        _hasher = hasher;
        _pwned = pwned;
    }

    public async Task ValidateAsync(Guid userId, string newPassword, PasswordPolicyContext context, CancellationToken cancellationToken = default)
    {
        EnsureLength(newPassword, context);

        if (await _pwned.IsPwnedAsync(newPassword, cancellationToken))
        {
            throw new CompromisedPasswordException();
        }

        var recent = await _history.ListRecentByUserAsync(userId, HistoryDepth, cancellationToken);
        foreach (var entry in recent)
        {
            if (_hasher.Verify(newPassword, entry.PasswordHash))
            {
                throw new PasswordReuseException();
            }
        }
    }

    public async Task RecordHistoryAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        await _history.AddAsync(new PasswordHistory(userId, passwordHash), cancellationToken);
        await _history.RemoveOlderThanAsync(userId, HistoryDepth, cancellationToken);
    }

    private static void EnsureLength(string password, PasswordPolicyContext context)
    {
        var min = context.IsTenantAdmin ? MinLengthTenantAdmin : MinLengthStandard;
        if (string.IsNullOrEmpty(password) || password.Length < min)
        {
            throw new WeakPasswordException(context.IsTenantAdmin
                ? "Validation.PasswordTooShortTenantAdmin"
                : "Validation.PasswordTooShort");
        }

        var byteLength = Encoding.UTF8.GetByteCount(password);
        if (byteLength > MaxLength || password.Length > MaxLength)
        {
            throw new WeakPasswordException("Validation.PasswordTooLong");
        }
    }
}
