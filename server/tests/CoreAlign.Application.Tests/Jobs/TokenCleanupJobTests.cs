using CoreAlign.Application.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Jobs;

public class TokenCleanupJobTests
{
    private readonly IMaintenanceDataAccess _maintenance = Substitute.For<IMaintenanceDataAccess>();

    [Fact]
    public async Task Deletes_tokens_with_expected_cutoffs()
    {
        var sut = new TokenCleanupJob(_maintenance, NullLogger<TokenCleanupJob>.Instance);
        _maintenance.DeleteRefreshTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(5);
        _maintenance.DeleteEmailVerificationTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(3);
        _maintenance.DeletePasswordResetTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(2);
        _maintenance.DeleteTwoFactorChallengesOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(7);

        var beforeRun = DateTime.UtcNow;
        await sut.RunAsync(CancellationToken.None);
        var afterRun = DateTime.UtcNow;

        await _maintenance.Received(1).DeleteRefreshTokensOlderThanAsync(
            Arg.Is<DateTime>(d => d >= beforeRun.AddDays(-14).AddSeconds(-1) && d <= afterRun.AddDays(-14).AddSeconds(1)),
            Arg.Any<CancellationToken>());
        await _maintenance.Received(1).DeleteEmailVerificationTokensOlderThanAsync(
            Arg.Is<DateTime>(d => d >= beforeRun.AddDays(-30).AddSeconds(-1) && d <= afterRun.AddDays(-30).AddSeconds(1)),
            Arg.Any<CancellationToken>());
        await _maintenance.Received(1).DeletePasswordResetTokensOlderThanAsync(
            Arg.Is<DateTime>(d => d >= beforeRun.AddDays(-7).AddSeconds(-1) && d <= afterRun.AddDays(-7).AddSeconds(1)),
            Arg.Any<CancellationToken>());
        await _maintenance.Received(1).DeleteTwoFactorChallengesOlderThanAsync(
            Arg.Is<DateTime>(d => d >= beforeRun.AddDays(-1).AddSeconds(-1) && d <= afterRun.AddDays(-1).AddSeconds(1)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_op_when_no_rows_match_cutoffs()
    {
        var sut = new TokenCleanupJob(_maintenance, NullLogger<TokenCleanupJob>.Instance);
        _maintenance.DeleteRefreshTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        _maintenance.DeleteEmailVerificationTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        _maintenance.DeletePasswordResetTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        _maintenance.DeleteTwoFactorChallengesOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);

        await sut.RunAsync(CancellationToken.None);

        await _maintenance.Received(1).DeleteRefreshTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Idempotent_running_twice_calls_maintenance_again_with_fresh_cutoffs()
    {
        var sut = new TokenCleanupJob(_maintenance, NullLogger<TokenCleanupJob>.Instance);

        await sut.RunAsync(CancellationToken.None);
        await sut.RunAsync(CancellationToken.None);

        await _maintenance.Received(2).DeleteRefreshTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _maintenance.Received(2).DeleteEmailVerificationTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _maintenance.Received(2).DeletePasswordResetTokensOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _maintenance.Received(2).DeleteTwoFactorChallengesOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
