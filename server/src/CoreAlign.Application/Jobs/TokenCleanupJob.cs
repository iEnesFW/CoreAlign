using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Jobs;

public sealed class TokenCleanupJob
{
    private readonly IMaintenanceDataAccess _maintenance;
    private readonly ILogger<TokenCleanupJob> _logger;

    public TokenCleanupJob(IMaintenanceDataAccess maintenance, ILogger<TokenCleanupJob> logger)
    {
        _maintenance = maintenance;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var refreshCutoff = now.AddDays(-14);
        var emailCutoff = now.AddDays(-30);
        var resetCutoff = now.AddDays(-7);
        var twoFactorCutoff = now.AddDays(-1);

        var refresh = await _maintenance.DeleteRefreshTokensOlderThanAsync(refreshCutoff, cancellationToken);
        var email = await _maintenance.DeleteEmailVerificationTokensOlderThanAsync(emailCutoff, cancellationToken);
        var reset = await _maintenance.DeletePasswordResetTokensOlderThanAsync(resetCutoff, cancellationToken);
        var twoFactor = await _maintenance.DeleteTwoFactorChallengesOlderThanAsync(twoFactorCutoff, cancellationToken);

        _logger.LogInformation(
            "Token cleanup completed. RefreshDeleted={Refresh}, EmailVerifyDeleted={Email}, PasswordResetDeleted={Reset}, TwoFactorDeleted={TwoFactor}.",
            refresh, email, reset, twoFactor);
    }
}
