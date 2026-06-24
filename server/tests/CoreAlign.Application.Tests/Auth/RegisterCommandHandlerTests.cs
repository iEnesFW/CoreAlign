using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CoreAlign.Application.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository = Substitute.For<ISubscriptionPlanRepository>();
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordPolicyService _passwordPolicyService = Substitute.For<IPasswordPolicyService>();
    private readonly ICaptchaVerifier _captchaVerifier = Substitute.For<ICaptchaVerifier>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:AutoConfirmEmail"] = "false" })
        .Build();
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        _jwtTokenService.GenerateRefreshToken().Returns("raw-token");
        _jwtTokenService.HashToken("raw-token").Returns("token-hash");
        _captchaVerifier.VerifyAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        _sut = new RegisterCommandHandler(
            _tenantRepository,
            _userRepository,
            _roleRepository,
            _subscriptionPlanRepository,
            _subscriptionRepository,
            _emailVerificationTokenRepository,
            _passwordHasher,
            _passwordPolicyService,
            _captchaVerifier,
            _jwtTokenService,
            _emailService,
            _unitOfWork,
            _configuration);
    }

    [Fact]
    public async Task Duplicate_email_returns_silent_success_and_emits_out_of_band_notice()
    {
        _userRepository.ExistsByEmailAsync("dup@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var command = new RegisterCommand("Acme", "user1", "dup@example.com", "Pwd123456!");
        var result = await _sut.Handle(command, default);

        result.Should().NotBeNull();
        result.AccessToken.Should().BeEmpty();
        await _emailService.Received(1).SendDuplicateRegistrationNoticeAsync("dup@example.com", Arg.Any<CancellationToken>());
        await _tenantRepository.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_username_throws_distinct_exception()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByUsernameAsync("taken", Arg.Any<CancellationToken>()).Returns(true);

        var command = new RegisterCommand("Acme", "taken", "new@example.com", "Pwd123456!");

        Func<Task> act = () => _sut.Handle(command, default);
        await act.Should().ThrowAsync<DuplicateUsernameException>();
    }
}
