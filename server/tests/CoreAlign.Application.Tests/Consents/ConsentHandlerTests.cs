using CoreAlign.Application.B2B;
using CoreAlign.Application.Consents;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Consents;

public class CaptureConsentHandlerTests
{
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUserConsentRepository _repository = Substitute.For<IUserConsentRepository>();

    [Fact]
    public async Task Anonymous_capture_persists_record_with_fingerprint_and_no_user_id()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var sut = new CaptureConsentHandler(_currentUser, _repository);

        var command = new CaptureConsentCommand(
            Purpose: "analytics",
            Version: "v2026-06-01",
            Given: true,
            Fingerprint: "fp-abc123",
            IpAddress: "203.0.113.42",
            UserAgent: "Mozilla/5.0");

        var result = await sut.Handle(command, default);

        result.UserId.Should().BeNull();
        result.Purpose.Should().Be("analytics");
        result.WithdrawnAtUtc.Should().BeNull();
        await _repository.Received(1).AddAsync(
            Arg.Is<UserConsent>(c =>
                c.UserId == null &&
                c.AnonymousFingerprint == "fp-abc123" &&
                c.Purpose == "analytics" &&
                c.IpAddress == "203.0.113.0/24"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Authenticated_capture_attributes_record_to_current_user()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        var sut = new CaptureConsentHandler(_currentUser, _repository);

        var command = new CaptureConsentCommand(
            Purpose: "marketing",
            Version: "v1",
            Given: true,
            Fingerprint: null);

        var result = await sut.Handle(command, default);

        result.UserId.Should().Be(userId);
        await _repository.Received(1).AddAsync(
            Arg.Is<UserConsent>(c => c.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_false_marks_consent_as_withdrawn_immediately()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        var sut = new CaptureConsentHandler(_currentUser, _repository);

        var command = new CaptureConsentCommand(
            Purpose: "analytics",
            Version: "v1",
            Given: false,
            Fingerprint: null);

        var result = await sut.Handle(command, default);

        result.WithdrawnAtUtc.Should().NotBeNull();
    }
}

public class WithdrawConsentHandlerTests
{
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUserConsentRepository _repository = Substitute.For<IUserConsentRepository>();

    [Fact]
    public async Task Withdraw_marks_existing_consent_as_withdrawn()
    {
        var userId = Guid.NewGuid();
        var consent = new UserConsent(userId, null, "marketing", "v1", DateTime.UtcNow.AddDays(-5), null, null);
        _currentUser.UserIdOrThrow().Returns(userId);
        _repository.GetByIdAsync(consent.Id, Arg.Any<CancellationToken>()).Returns(consent);

        var sut = new WithdrawConsentHandler(_currentUser, _repository);
        var result = await sut.Handle(new WithdrawConsentCommand(consent.Id), default);

        result.WithdrawnAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Withdraw_throws_when_consent_belongs_to_different_user()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var consent = new UserConsent(ownerId, null, "marketing", "v1", DateTime.UtcNow.AddDays(-5), null, null);
        _currentUser.UserIdOrThrow().Returns(attackerId);
        _repository.GetByIdAsync(consent.Id, Arg.Any<CancellationToken>()).Returns(consent);

        var sut = new WithdrawConsentHandler(_currentUser, _repository);

        Func<Task> act = () => sut.Handle(new WithdrawConsentCommand(consent.Id), default);
        await act.Should().ThrowAsync<ConsentNotFoundException>();
    }
}
