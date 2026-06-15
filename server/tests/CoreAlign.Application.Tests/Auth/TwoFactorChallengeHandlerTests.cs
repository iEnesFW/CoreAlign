using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Auth;

public class TwoFactorChallengeHandlerTests
{
    private readonly ITwoFactorChallengeRepository _challengeRepository = Substitute.For<ITwoFactorChallengeRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITwoFactorService _twoFactorService = Substitute.For<ITwoFactorService>();
    private readonly ITwoFactorBackupCodeRepository _backupCodeRepository = Substitute.For<ITwoFactorBackupCodeRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly ILoginAuditLogRepository _loginAuditLogRepository = Substitute.For<ILoginAuditLogRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IUserMembershipService _userMembershipService = Substitute.For<IUserMembershipService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid TenantId = Guid.NewGuid();

    private CompleteTwoFactorChallengeCommandHandler BuildSut() => new(
        _challengeRepository,
        _userRepository,
        _tenantRepository,
        _twoFactorService,
        _backupCodeRepository,
        _refreshTokenRepository,
        _loginAuditLogRepository,
        _userSessionRepository,
        _jwtTokenService,
        _userMembershipService,
        _unitOfWork);

    [Fact]
    public async Task Success_with_totp_issues_tokens_and_marks_challenge_consumed()
    {
        var (user, challenge) = SetupValidChallenge();
        _twoFactorService.Verify(user.TwoFactorSecretKey!, "123456", 1).Returns(true);
        _jwtTokenService.GenerateAccessToken(user.Id, user.TenantId, user.Email, Arg.Any<IEnumerable<string>>(), "tenant", Arg.Any<DateTime?>())
            .Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("refresh-raw");
        _jwtTokenService.HashToken("refresh-raw").Returns("refresh-hash");

        var sut = BuildSut();
        var result = await sut.Handle(
            new CompleteTwoFactorChallengeCommand("raw-token", "123456", null, "1.1.1.1", "ua"),
            CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-raw");
        result.User.Should().NotBeNull();
        challenge.IsConsumed.Should().BeTrue();
        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _userSessionRepository.Received(1).AddAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>());
        await _loginAuditLogRepository.Received().AddAsync(
            Arg.Is<LoginAuditLog>(l => l.LoginResult == LoginResultType.TwoFactorSuccess),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Success_with_backup_code_marks_code_used_and_issues_tokens()
    {
        var (user, _) = SetupValidChallenge();
        var backupCode = new TwoFactorBackupCode(user.TenantId, user.Id, "HASH-ABCDEFGH");
        _twoFactorService.HashBackupCode("ABCDEFGH").Returns("HASH-ABCDEFGH");
        _backupCodeRepository.FindActiveByHashAsync(user.Id, "HASH-ABCDEFGH", Arg.Any<CancellationToken>())
            .Returns(backupCode);
        _jwtTokenService.GenerateAccessToken(user.Id, user.TenantId, user.Email, Arg.Any<IEnumerable<string>>(), "tenant", Arg.Any<DateTime?>())
            .Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("refresh-raw");
        _jwtTokenService.HashToken("refresh-raw").Returns("refresh-hash");

        var sut = BuildSut();
        var result = await sut.Handle(
            new CompleteTwoFactorChallengeCommand("raw-token", null, "ABCDEFGH"),
            CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        backupCode.IsUsed.Should().BeTrue();
        _backupCodeRepository.Received(1).Update(backupCode);
    }

    [Fact]
    public async Task Wrong_code_throws_invalid_and_writes_failure_audit()
    {
        var (user, _) = SetupValidChallenge();
        _twoFactorService.Verify(user.TwoFactorSecretKey!, "999999", 1).Returns(false);

        var sut = BuildSut();
        var act = async () => await sut.Handle(
            new CompleteTwoFactorChallengeCommand("raw-token", "999999", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorCodeException>();
        await _loginAuditLogRepository.Received().AddAsync(
            Arg.Is<LoginAuditLog>(l => l.LoginResult == LoginResultType.TwoFactorFailed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Expired_challenge_throws()
    {
        var user = BuildUser();
        user.IsTwoFactorEnabled = true;
        user.TwoFactorSecretKey = "SECRET";
        var expired = new TwoFactorChallenge(user.TenantId, user.Id, "challenge-hash",
            DateTime.UtcNow.AddMinutes(-1)) { User = user };
        _jwtTokenService.HashToken("raw-token").Returns("challenge-hash");
        _challengeRepository.FindByTokenHashAsync("challenge-hash", Arg.Any<CancellationToken>()).Returns(expired);

        var sut = BuildSut();
        var act = async () => await sut.Handle(
            new CompleteTwoFactorChallengeCommand("raw-token", "123456", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorChallengeException>();
    }

    [Fact]
    public async Task Missing_code_and_backup_throws()
    {
        var sut = BuildSut();
        var act = async () => await sut.Handle(
            new CompleteTwoFactorChallengeCommand("raw-token", null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorCodeException>();
    }

    private (User, TwoFactorChallenge) SetupValidChallenge()
    {
        var user = BuildUser();
        user.IsTwoFactorEnabled = true;
        user.TwoFactorSecretKey = "SECRET";
        var challenge = new TwoFactorChallenge(user.TenantId, user.Id, "challenge-hash",
            DateTime.UtcNow.AddMinutes(5)) { User = user };
        _jwtTokenService.HashToken("raw-token").Returns("challenge-hash");
        _challengeRepository.FindByTokenHashAsync("challenge-hash", Arg.Any<CancellationToken>()).Returns(challenge);
        _tenantRepository.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>())
            .Returns(new Tenant("Acme", "acme") { Id = user.TenantId });
        _userMembershipService.ResolvePersonaAsync(user.Id, user.TenantId, Arg.Any<CancellationToken>())
            .Returns(UserPersona.Tenant);
        return (user, challenge);
    }

    private static User BuildUser()
    {
        return new User(TenantId, "tester", "tester@example.com", "hashed-pw")
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            IsEmailConfirmed = true,
        };
    }
}
