using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Auth;

public class ResetPasswordCommandRevokesSessionsTests
{
    private readonly IPasswordResetTokenRepository _resetTokens = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserSessionRepository _sessions = Substitute.For<IUserSessionRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordPolicyService _policy = Substitute.For<IPasswordPolicyService>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly ResetPasswordCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    public ResetPasswordCommandRevokesSessionsTests()
    {
        _sut = new ResetPasswordCommandHandler(
            _resetTokens,
            _users,
            _refreshTokens,
            _sessions,
            _hasher,
            _policy,
            _jwt,
            _roles,
            _uow);
    }

    [Fact]
    public async Task Successful_reset_revokes_all_refresh_tokens_and_sessions()
    {
        var user = BuildUser();
        var token = new PasswordResetToken(user.Id, "hash", DateTime.UtcNow.AddHours(1));
        _jwt.HashToken("raw").Returns("hash");
        _resetTokens.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash("NewStrongPass1!").Returns("hashed-new");

        var result = await _sut.Handle(new ResetPasswordCommand("raw", "NewStrongPass1!"), default);

        result.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed-new");
        await _refreshTokens.Received(1).RevokeAllByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
        await _sessions.Received(1).RevokeAllByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Successful_reset_rotates_security_stamp()
    {
        var user = BuildUser();
        var originalStamp = user.SecurityStamp;
        var token = new PasswordResetToken(user.Id, "hash", DateTime.UtcNow.AddHours(1));
        _jwt.HashToken("raw").Returns("hash");
        _resetTokens.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed-new");

        await _sut.Handle(new ResetPasswordCommand("raw", "NewStrongPass1!"), default);

        user.SecurityStamp.Should().NotBe(originalStamp);
    }

    [Fact]
    public async Task Successful_reset_records_previous_hash_in_history()
    {
        var user = BuildUser();
        var token = new PasswordResetToken(user.Id, "hash", DateTime.UtcNow.AddHours(1));
        _jwt.HashToken("raw").Returns("hash");
        _resetTokens.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(token);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed-new");

        await _sut.Handle(new ResetPasswordCommand("raw", "NewStrongPass1!"), default);

        await _policy.Received(1).RecordHistoryAsync(user.Id, "hashed-old", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_or_expired_token_throws_and_does_not_revoke()
    {
        _jwt.HashToken("raw").Returns("hash");
        _resetTokens.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns((PasswordResetToken?)null);

        Func<Task> act = () => _sut.Handle(new ResetPasswordCommand("raw", "NewStrongPass1!"), default);

        await act.Should().ThrowAsync<TokenExpiredException>();
        await _refreshTokens.DidNotReceive().RevokeAllByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _sessions.DidNotReceive().RevokeAllByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static User BuildUser() => new(TenantId, "alice", "alice@example.com", "hashed-old")
    {
        Id = UserId,
    };
}
