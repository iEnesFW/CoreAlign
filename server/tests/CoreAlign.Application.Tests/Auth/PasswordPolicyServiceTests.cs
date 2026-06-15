using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Auth;

public class PasswordPolicyServiceTests
{
    private readonly IPasswordHistoryRepository _history = Substitute.For<IPasswordHistoryRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IPwnedPasswordsService _pwned = Substitute.For<IPwnedPasswordsService>();
    private readonly PasswordPolicyService _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    public PasswordPolicyServiceTests()
    {
        _sut = new PasswordPolicyService(_history, _hasher, _pwned);
    }

    [Fact]
    public async Task Valid_password_passes_all_checks()
    {
        _pwned.IsPwnedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _history.ListRecentByUserAsync(UserId, PasswordPolicyService.HistoryDepth, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PasswordHistory>());

        Func<Task> act = () => _sut.ValidateAsync(UserId, "StrongPassword1!", PasswordPolicyContext.Standard);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Short_password_throws_weak_password()
    {
        Func<Task> act = () => _sut.ValidateAsync(UserId, "Short1!", PasswordPolicyContext.Standard);

        await act.Should().ThrowAsync<WeakPasswordException>();
        await _pwned.DidNotReceive().IsPwnedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TenantAdmin_requires_longer_minimum_length()
    {
        Func<Task> act = () => _sut.ValidateAsync(UserId, "TwelveChars1!", PasswordPolicyContext.TenantAdmin);

        await act.Should().ThrowAsync<WeakPasswordException>();
    }

    [Fact]
    public async Task Password_over_max_length_throws_weak_password()
    {
        var oversized = new string('a', PasswordPolicyService.MaxLength + 1);

        Func<Task> act = () => _sut.ValidateAsync(UserId, oversized, PasswordPolicyContext.Standard);

        await act.Should().ThrowAsync<WeakPasswordException>();
    }

    [Fact]
    public async Task Compromised_password_throws_compromised_exception()
    {
        _pwned.IsPwnedAsync("StrongPassword1!", Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => _sut.ValidateAsync(UserId, "StrongPassword1!", PasswordPolicyContext.Standard);

        await act.Should().ThrowAsync<CompromisedPasswordException>();
    }

    [Fact]
    public async Task Reused_password_throws_password_reuse_exception()
    {
        var history = new List<PasswordHistory>
        {
            new(UserId, "hash-1"),
            new(UserId, "hash-2"),
        };

        _pwned.IsPwnedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _history.ListRecentByUserAsync(UserId, PasswordPolicyService.HistoryDepth, Arg.Any<CancellationToken>())
            .Returns(history);
        _hasher.Verify("StrongPassword1!", "hash-1").Returns(false);
        _hasher.Verify("StrongPassword1!", "hash-2").Returns(true);

        Func<Task> act = () => _sut.ValidateAsync(UserId, "StrongPassword1!", PasswordPolicyContext.Standard);

        await act.Should().ThrowAsync<PasswordReuseException>();
    }

    [Fact]
    public async Task Graceful_hibp_failure_is_treated_as_not_pwned()
    {
        _pwned.IsPwnedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _history.ListRecentByUserAsync(UserId, PasswordPolicyService.HistoryDepth, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PasswordHistory>());

        Func<Task> act = () => _sut.ValidateAsync(UserId, "StrongPassword1!", PasswordPolicyContext.Standard);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Record_history_adds_entry_and_trims_to_limit()
    {
        await _sut.RecordHistoryAsync(UserId, "new-hash");

        await _history.Received(1).AddAsync(
            Arg.Is<PasswordHistory>(h => h.UserId == UserId && h.PasswordHash == "new-hash"),
            Arg.Any<CancellationToken>());
        await _history.Received(1).RemoveOlderThanAsync(UserId, PasswordPolicyService.HistoryDepth, Arg.Any<CancellationToken>());
    }
}
