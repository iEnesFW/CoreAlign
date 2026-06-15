namespace CoreAlign.Application.Auth.Services;

public interface IPasswordPolicyService
{
    Task ValidateAsync(Guid userId, string newPassword, PasswordPolicyContext context, CancellationToken cancellationToken = default);
    Task RecordHistoryAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
}

public sealed record PasswordPolicyContext(bool IsTenantAdmin)
{
    public static PasswordPolicyContext Standard => new(false);
    public static PasswordPolicyContext TenantAdmin => new(true);
}
