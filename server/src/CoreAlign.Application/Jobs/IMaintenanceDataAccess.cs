namespace CoreAlign.Application.Jobs;

public interface IMaintenanceDataAccess
{
    Task<int> DeleteRefreshTokensOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<int> DeleteEmailVerificationTokensOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<int> DeletePasswordResetTokensOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task<int> DeleteTwoFactorChallengesOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    Task<int> AnonymizeLoginAuditLogsOlderThanAsync(DateTime cutoffUtc, Func<string, string> ipHasher, CancellationToken cancellationToken = default);
    Task<int> AnonymizeActivityLogsOlderThanAsync(DateTime cutoffUtc, Func<string, string> ipHasher, Func<string, string> uaHasher, CancellationToken cancellationToken = default);
}
