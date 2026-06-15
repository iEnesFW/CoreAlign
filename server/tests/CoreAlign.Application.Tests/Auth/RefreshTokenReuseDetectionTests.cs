using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Auth;

public class RefreshTokenReuseDetectionTests
{
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
    private readonly ILoginAuditLogRepository _auditLogs = Substitute.For<ILoginAuditLogRepository>();
    private readonly ISecurityAlertOutbox _securityAlerts = Substitute.For<ISecurityAlertOutbox>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IUserMembershipService _membership = Substitute.For<IUserMembershipService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly RefreshTokenCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    public RefreshTokenReuseDetectionTests()
    {
        _sut = new RefreshTokenCommandHandler(
            _refreshTokens,
            _tenants,
            _sessions,
            _auditLogs,
            _securityAlerts,
            _jwt,
            _membership,
            _uow);
    }

    [Fact]
    public async Task Replay_of_revoked_token_throws_token_expired_and_revokes_active_descendants()
    {
        var (chain, tokenAHash) = BuildChain(4);
        var tokenA = chain[0];

        _jwt.HashToken("raw-A").Returns(tokenAHash);
        _refreshTokens.GetByTokenHashAsync(tokenAHash, Arg.Any<CancellationToken>()).Returns(tokenA);
        _refreshTokens.ListChainFromAsync(tokenAHash, Arg.Any<CancellationToken>()).Returns(chain);

        var command = new RefreshTokenCommand("raw-A", IpAddress: "1.2.3.4", DeviceInfo: "ua/1");

        Func<Task> act = () => _sut.Handle(command, default);
        await act.Should().ThrowAsync<TokenExpiredException>();

        await _refreshTokens.Received(1).RevokeManyAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Count() == 1 && ids.Contains(chain[3].Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replay_of_revoked_token_revokes_user_sessions_and_writes_audit_log()
    {
        var (chain, tokenAHash) = BuildChain(2);
        var tokenA = chain[0];

        _jwt.HashToken("raw-A").Returns(tokenAHash);
        _refreshTokens.GetByTokenHashAsync(tokenAHash, Arg.Any<CancellationToken>()).Returns(tokenA);
        _refreshTokens.ListChainFromAsync(tokenAHash, Arg.Any<CancellationToken>()).Returns(chain);

        var command = new RefreshTokenCommand("raw-A", IpAddress: "9.9.9.9", DeviceInfo: "agent");

        Func<Task> act = () => _sut.Handle(command, default);
        await act.Should().ThrowAsync<TokenExpiredException>();

        await _sessions.Received(1).RevokeAllByUserIdAsync(UserId, Arg.Any<CancellationToken>());
        await _auditLogs.Received(1).AddAsync(
            Arg.Is<LoginAuditLog>(l => l.FailureReason == "RefreshTokenReuse" && l.UserId == UserId && l.IpAddress == "9.9.9.9"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replay_of_revoked_token_enqueues_security_alert_outbox_message()
    {
        var (chain, tokenAHash) = BuildChain(2);
        var tokenA = chain[0];

        _jwt.HashToken("raw-A").Returns(tokenAHash);
        _refreshTokens.GetByTokenHashAsync(tokenAHash, Arg.Any<CancellationToken>()).Returns(tokenA);
        _refreshTokens.ListChainFromAsync(tokenAHash, Arg.Any<CancellationToken>()).Returns(chain);

        var command = new RefreshTokenCommand("raw-A", IpAddress: "5.5.5.5", DeviceInfo: "agent-x");

        Func<Task> act = () => _sut.Handle(command, default);
        await act.Should().ThrowAsync<TokenExpiredException>();

        await _securityAlerts.Received(1).EnqueueRefreshTokenReuseAsync(
            UserId,
            Arg.Any<DateTime>(),
            "5.5.5.5",
            "agent-x",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Active_token_rotation_does_not_trigger_reuse_detection()
    {
        var user = BuildUser();
        var token = new RefreshToken(user.Id, "active-hash", DateTime.UtcNow.AddDays(7))
        {
            Id = Guid.NewGuid(),
            User = user,
        };
        var tenant = new Tenant("Acme", "acme") { Id = TenantId };

        _jwt.HashToken("raw").Returns("active-hash");
        _refreshTokens.GetByTokenHashAsync("active-hash", Arg.Any<CancellationToken>()).Returns(token);
        _tenants.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _jwt.GenerateRefreshToken().Returns("new-raw");
        _jwt.HashToken("new-raw").Returns("new-hash");
        _jwt.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>()).Returns("access-token");
        _membership.ResolvePersonaAsync(user.Id, TenantId, Arg.Any<CancellationToken>()).Returns(Domain.Enums.UserPersona.Tenant);

        var result = await _sut.Handle(new RefreshTokenCommand("raw"), default);

        result.AccessToken.Should().Be("access-token");
        await _refreshTokens.DidNotReceive().RevokeManyAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await _sessions.DidNotReceive().RevokeAllByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _securityAlerts.DidNotReceive().EnqueueRefreshTokenReuseAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    private static (List<RefreshToken> Chain, string TokenAHash) BuildChain(int length)
    {
        var user = BuildUser();
        var tokens = new List<RefreshToken>();
        string? previousHash = null;

        for (var i = 0; i < length; i++)
        {
            var hash = $"hash-{(char)('A' + i)}";
            var token = new RefreshToken(user.Id, hash, DateTime.UtcNow.AddDays(7))
            {
                Id = Guid.NewGuid(),
                User = user,
            };
            tokens.Add(token);
            if (previousHash is not null)
            {
                tokens[i - 1].Revoke(hash);
            }
            previousHash = hash;
        }

        return (tokens, tokens[0].TokenHash);
    }

    private static User BuildUser() => new(TenantId, "alice", "alice@example.com", "hashed")
    {
        Id = UserId,
    };
}
