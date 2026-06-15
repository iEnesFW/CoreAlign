using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Auth;

public class TwoFactorEnrollmentHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITwoFactorService _twoFactorService = Substitute.For<ITwoFactorService>();
    private readonly ITwoFactorBackupCodeRepository _backupCodeRepository = Substitute.For<ITwoFactorBackupCodeRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task Enroll_assigns_secret_and_returns_otpauth_uri()
    {
        var user = BuildUser();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tenantRepository.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>())
            .Returns(new Tenant("Acme", "acme") { Id = TenantId });
        _twoFactorService.GenerateSecret().Returns("BASE32SECRETXYZ");
        _twoFactorService.BuildOtpAuthUri("BASE32SECRETXYZ", user.Email, "Acme")
            .Returns("otpauth://totp/Acme:tester@example.com?secret=BASE32SECRETXYZ");

        var sut = new EnrollTwoFactorCommandHandler(_userRepository, _tenantRepository, _twoFactorService, _unitOfWork);

        var result = await sut.Handle(new EnrollTwoFactorCommand(user.Id), CancellationToken.None);

        result.ManualKey.Should().Be("BASE32SECRETXYZ");
        result.QrCodeUri.Should().StartWith("otpauth://totp/");
        user.TwoFactorSecretKey.Should().Be("BASE32SECRETXYZ");
        user.IsTwoFactorEnabled.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Enroll_when_already_enabled_throws()
    {
        var user = BuildUser();
        user.IsTwoFactorEnabled = true;
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sut = new EnrollTwoFactorCommandHandler(_userRepository, _tenantRepository, _twoFactorService, _unitOfWork);
        var act = async () => await sut.Handle(new EnrollTwoFactorCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<TwoFactorAlreadyEnabledException>();
    }

    [Fact]
    public async Task Verify_with_valid_code_enables_2fa_and_returns_backup_codes()
    {
        var user = BuildUser();
        user.TwoFactorSecretKey = "BASE32SECRET";
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _twoFactorService.Verify("BASE32SECRET", "123456", 1).Returns(true);
        _twoFactorService.GenerateBackupCodes(10).Returns(new[]
        {
            "AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "EEEEEEEE",
            "FFFFFFFF", "GGGGGGGG", "HHHHHHHH", "JJJJJJJJ", "KKKKKKKK",
        });
        _twoFactorService.HashBackupCode(Arg.Any<string>()).Returns(c => "HASH-" + c.Arg<string>());

        var sut = new VerifyTwoFactorEnrollmentCommandHandler(
            _userRepository, _twoFactorService, _backupCodeRepository, _unitOfWork);

        var result = await sut.Handle(new VerifyTwoFactorEnrollmentCommand(user.Id, "123456"), CancellationToken.None);

        result.BackupCodes.Should().HaveCount(10);
        user.IsTwoFactorEnabled.Should().BeTrue();
        await _backupCodeRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<TwoFactorBackupCode>>(codes => codes.Count() == 10),
            Arg.Any<CancellationToken>());
        await _backupCodeRepository.Received(1).RemoveAllByUserAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_with_invalid_code_throws()
    {
        var user = BuildUser();
        user.TwoFactorSecretKey = "BASE32SECRET";
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _twoFactorService.Verify("BASE32SECRET", "000000", 1).Returns(false);

        var sut = new VerifyTwoFactorEnrollmentCommandHandler(
            _userRepository, _twoFactorService, _backupCodeRepository, _unitOfWork);
        var act = async () => await sut.Handle(new VerifyTwoFactorEnrollmentCommand(user.Id, "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorCodeException>();
        user.IsTwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Disable_with_correct_password_clears_state()
    {
        var user = BuildUser();
        user.IsTwoFactorEnabled = true;
        user.TwoFactorSecretKey = "SECRET";
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("CorrectPwd1!", user.PasswordHash).Returns(true);

        var sut = new DisableTwoFactorCommandHandler(_userRepository, _passwordHasher, _backupCodeRepository, _unitOfWork);
        var result = await sut.Handle(new DisableTwoFactorCommand(user.Id, "CorrectPwd1!"), CancellationToken.None);

        result.Should().BeTrue();
        user.IsTwoFactorEnabled.Should().BeFalse();
        user.TwoFactorSecretKey.Should().BeNull();
        await _backupCodeRepository.Received(1).RemoveAllByUserAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Disable_with_wrong_password_throws_invalid_credentials()
    {
        var user = BuildUser();
        user.IsTwoFactorEnabled = true;
        user.TwoFactorSecretKey = "SECRET";
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("WrongPwd", user.PasswordHash).Returns(false);

        var sut = new DisableTwoFactorCommandHandler(_userRepository, _passwordHasher, _backupCodeRepository, _unitOfWork);
        var act = async () => await sut.Handle(new DisableTwoFactorCommand(user.Id, "WrongPwd"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        user.IsTwoFactorEnabled.Should().BeTrue();
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
