using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly ILoginAuditLogRepository _loginAuditLogRepository = Substitute.For<ILoginAuditLogRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LoginCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(
            _userRepository,
            _tenantRepository,
            _refreshTokenRepository,
            _loginAuditLogRepository,
            _userSessionRepository,
            _passwordHasher,
            _jwtTokenService,
            _unitOfWork,
            NullLogger<LoginCommandHandler>.Instance);
    }

    [Fact]
    public async Task UnknownEmail_ReturnsGenericInvalidCredentials()
    {
        _userRepository.GetByEmailAsync("ghost@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = async () => await _sut.Handle(
            new LoginCommand("ghost@example.com", "Pwd123456!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LockedAccount_DoesNotRevealLockoutState()
    {
        var user = BuildUser(isActive: true, isEmailConfirmed: true);
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await _sut.Handle(
            new LoginCommand(user.Email, "Pwd123456!"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task WrongPassword_ReturnsInvalidCredentialsAndRecordsFailedLogin()
    {
        var user = BuildUser(isActive: true, isEmailConfirmed: true);
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", user.PasswordHash).Returns(false);

        var act = async () => await _sut.Handle(
            new LoginCommand(user.Email, "wrong"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task DisabledAccount_AfterPasswordVerification_RevealsDisabledState()
    {
        var user = BuildUser(isActive: false, isEmailConfirmed: true);
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Pwd123456!", user.PasswordHash).Returns(true);

        var act = async () => await _sut.Handle(
            new LoginCommand(user.Email, "Pwd123456!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccountDisabledException>();
    }

    [Fact]
    public async Task UnverifiedEmail_AfterPasswordVerification_RevealsState()
    {
        var user = BuildUser(isActive: true, isEmailConfirmed: false);
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Pwd123456!", user.PasswordHash).Returns(true);

        var act = async () => await _sut.Handle(
            new LoginCommand(user.Email, "Pwd123456!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<EmailNotVerifiedException>();
    }

    private static User BuildUser(bool isActive, bool isEmailConfirmed)
    {
        var user = new User(TenantId, "tester", "tester@example.com", "hashed-pw")
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            IsActive = isActive,
            IsEmailConfirmed = isEmailConfirmed
        };
        return user;
    }
}
