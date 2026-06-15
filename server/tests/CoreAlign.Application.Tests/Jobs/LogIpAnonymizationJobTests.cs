using CoreAlign.Application.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Jobs;

public class LogIpAnonymizationJobTests
{
    private readonly IMaintenanceDataAccess _maintenance = Substitute.For<IMaintenanceDataAccess>();

    [Fact]
    public async Task Calls_maintenance_with_30_day_cutoff_for_both_tables()
    {
        var sut = new LogIpAnonymizationJob(_maintenance, NullLogger<LogIpAnonymizationJob>.Instance);

        var beforeRun = DateTime.UtcNow;
        await sut.RunAsync(CancellationToken.None);
        var afterRun = DateTime.UtcNow;

        await _maintenance.Received(1).AnonymizeLoginAuditLogsOlderThanAsync(
            Arg.Is<DateTime>(d => d >= beforeRun.AddDays(-30).AddSeconds(-1) && d <= afterRun.AddDays(-30).AddSeconds(1)),
            Arg.Any<Func<string, string>>(),
            Arg.Any<CancellationToken>());

        await _maintenance.Received(1).AnonymizeActivityLogsOlderThanAsync(
            Arg.Is<DateTime>(d => d >= beforeRun.AddDays(-30).AddSeconds(-1) && d <= afterRun.AddDays(-30).AddSeconds(1)),
            Arg.Any<Func<string, string>>(),
            Arg.Any<Func<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Hasher_produces_stable_sha256_hex()
    {
        Func<string, string>? capturedHasher = null;
        _maintenance.AnonymizeLoginAuditLogsOlderThanAsync(Arg.Any<DateTime>(), Arg.Do<Func<string, string>>(h => capturedHasher = h), Arg.Any<CancellationToken>())
            .Returns(0);

        var sut = new LogIpAnonymizationJob(_maintenance, NullLogger<LogIpAnonymizationJob>.Instance);
        await sut.RunAsync(CancellationToken.None);

        capturedHasher.Should().NotBeNull();
        var hashA = capturedHasher!("192.168.1.1");
        var hashB = capturedHasher!("192.168.1.1");
        var hashC = capturedHasher!("10.0.0.1");

        hashA.Should().Be(hashB);
        hashA.Should().NotBe(hashC);
        hashA.Length.Should().Be(64);
    }

    [Fact]
    public async Task No_op_when_no_rows_match()
    {
        _maintenance.AnonymizeLoginAuditLogsOlderThanAsync(
            Arg.Any<DateTime>(), Arg.Any<Func<string, string>>(), Arg.Any<CancellationToken>()).Returns(0);
        _maintenance.AnonymizeActivityLogsOlderThanAsync(
            Arg.Any<DateTime>(), Arg.Any<Func<string, string>>(), Arg.Any<Func<string, string>>(), Arg.Any<CancellationToken>()).Returns(0);

        var sut = new LogIpAnonymizationJob(_maintenance, NullLogger<LogIpAnonymizationJob>.Instance);

        await sut.RunAsync(CancellationToken.None);

        await _maintenance.Received(1).AnonymizeLoginAuditLogsOlderThanAsync(
            Arg.Any<DateTime>(), Arg.Any<Func<string, string>>(), Arg.Any<CancellationToken>());
    }
}
