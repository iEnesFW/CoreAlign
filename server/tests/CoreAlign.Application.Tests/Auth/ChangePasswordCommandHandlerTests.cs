using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ChangePasswordCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    public ChangePasswordCommandHandlerTests()
    {
        _sut = new ChangePasswordCommandHandler(
            _userRepository,
            _refreshTokenRepository,
            _userSessionRepository,
            _passwordHasher,
            _unitOfWork);
    }

    [Fact]
    public async Task Changes_password_and_revokes_all_sessions_when_current_is_valid()
    {
        var user = BuildUser();
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("OldPass1!", user.PasswordHash).Returns(true);
        _passwordHasher.Hash("NewPass1!").Returns("hashed-new");

        var originalStamp = user.SecurityStamp;

        var result = await _sut.Handle(new ChangePasswordCommand(UserId, "OldPass1!", "NewPass1!"), default);

        result.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed-new");
        user.SecurityStamp.Should().NotBe(originalStamp);
        await _refreshTokenRepository.Received(1).RevokeAllByUserIdAsync(UserId, Arg.Any<CancellationToken>());
        await _userSessionRepository.Received(1).RevokeAllByUserIdAsync(UserId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_invalid_credentials_when_current_password_wrong()
    {
        var user = BuildUser();
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Wrong", user.PasswordHash).Returns(false);

        Func<Task> act = () => _sut.Handle(new ChangePasswordCommand(UserId, "Wrong", "NewPass1!"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_user_not_found_when_user_missing()
    {
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _sut.Handle(new ChangePasswordCommand(UserId, "x", "Y1!aaaaa"), default);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    private static User BuildUser() => new(TenantId, "alice", "alice@example.com", "hashed-old")
    {
        Id = UserId
    };
}
