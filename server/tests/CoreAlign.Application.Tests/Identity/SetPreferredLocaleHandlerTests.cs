using CoreAlign.Application.B2B;
using CoreAlign.Application.Identity.Locale;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Identity;

public class SetPreferredLocaleHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public SetPreferredLocaleHandlerTests()
    {
        _currentUser.UserIdOrThrow().Returns(UserId);
    }

    [Theory]
    [InlineData("ar", "ar")]
    [InlineData("AR", "ar")]
    [InlineData("ar-SA", "ar")]
    [InlineData("tr-TR", "tr")]
    [InlineData("de", "de")]
    [InlineData("ru", "ru")]
    [InlineData("EN", "en")]
    public async Task Normalises_and_persists_supported_locale(string input, string expected)
    {
        var user = BuildUser();
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var sut = new SetPreferredLocaleHandler(_users, _currentUser, _uow);
        var result = await sut.Handle(new SetPreferredLocaleCommand(input), default);

        result.PreferredLocale.Should().Be(expected);
        user.PreferredLocale.Should().Be(expected);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("zh")]
    [InlineData("xx-YY")]
    [InlineData("")]
    public async Task Falls_back_to_english_for_unsupported_locale(string input)
    {
        var user = BuildUser();
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var sut = new SetPreferredLocaleHandler(_users, _currentUser, _uow);
        var result = await sut.Handle(new SetPreferredLocaleCommand(input), default);

        result.PreferredLocale.Should().Be("en");
        user.PreferredLocale.Should().Be("en");
    }

    [Fact]
    public async Task Throws_when_user_missing()
    {
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((User?)null);
        var sut = new SetPreferredLocaleHandler(_users, _currentUser, _uow);
        var act = async () => await sut.Handle(new SetPreferredLocaleCommand("tr"), default);
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    private static User BuildUser()
    {
        var user = new User(TenantId, "alice", "alice@example.com", "hash");
        user.Id = UserId;
        return user;
    }
}
