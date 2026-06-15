using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Auth;

public class LoginWithTwoFactorRequirementTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly ILoginAuditLogRepository _loginAuditLogRepository = Substitute.For<ILoginAuditLogRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IUserMembershipService _userMembershipService = Substitute.For<IUserMembershipService>();
    private readonly ITwoFactorChallengeRepository _twoFactorChallengeRepository = Substitute.For<ITwoFactorChallengeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid TenantId = Guid.NewGuid();

    private LoginCommandHandler BuildSut() => new(
        _userRepository,
        _tenantRepository,
        _refreshTokenRepository,
        _loginAuditLogRepository,
        _userSessionRepository,
        _passwordHasher,
        _jwtTokenService,
        _userMembershipService,
        _twoFactorChallengeRepository,
        _unitOfWork,
        NullLogger<LoginCommandHandler>.Instance);

    [Fact]
    public async Task User_with_2fa_enabled_receives_challenge_token_and_no_access_token()
    {
        var user = BuildUser(twoFactorEnabled: true);
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Pwd123456!", user.PasswordHash).Returns(true);
        _tenantRepository.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>())
            .Returns(new Tenant("Acme", "acme") { Id = user.TenantId });
        _jwtTokenService.HashToken(Arg.Any<string>()).Returns("HASH");

        var sut = BuildSut();
        var result = await sut.Handle(
            new LoginCommand(user.Email, "Pwd123456!"),
            CancellationToken.None);

        result.RequiresTwoFactor.Should().BeTrue();
        result.TwoFactorChallengeToken.Should().NotBeNullOrEmpty();
        result.AccessToken.Should().BeEmpty();
        result.User.Should().BeNull();
        await _twoFactorChallengeRepository.Received(1).AddAsync(
            Arg.Any<TwoFactorChallenge>(),
            Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tenant_requires_2fa_for_role_and_user_has_no_2fa_throws()
    {
        var user = BuildUser(twoFactorEnabled: false);
        var role = new Role { Id = 1, Name = "TenantAdmin" };
        user.UserRoles.Add(new UserRole(user.Id, role.Id) { Role = role });

        var tenant = new Tenant("Acme", "acme") { Id = user.TenantId, RequireTwoFactorForRoles = "TenantAdmin" };

        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Pwd123456!", user.PasswordHash).Returns(true);
        _tenantRepository.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        var sut = BuildSut();
        var act = async () => await sut.Handle(
            new LoginCommand(user.Email, "Pwd123456!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<TwoFactorRequiredException>();
        await _twoFactorChallengeRepository.DidNotReceive().AddAsync(Arg.Any<TwoFactorChallenge>(), Arg.Any<CancellationToken>());
    }

    private static User BuildUser(bool twoFactorEnabled)
    {
        var user = new User(TenantId, "tester", "tester@example.com", "hashed-pw")
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            IsEmailConfirmed = true,
            IsTwoFactorEnabled = twoFactorEnabled,
            TwoFactorSecretKey = twoFactorEnabled ? "SECRET" : null,
        };
        return user;
    }
}
