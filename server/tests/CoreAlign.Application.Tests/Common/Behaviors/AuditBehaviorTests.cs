using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Common.Behaviors;
using CoreAlign.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Common.Behaviors;

public class AuditBehaviorTests
{
    private readonly IAuditContext _auditContext = Substitute.For<IAuditContext>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IAuditFieldRedactor _redactor = Substitute.For<IAuditFieldRedactor>();

    [Fact]
    public async Task Handler_success_exposes_pending_entries_to_audit_context()
    {
        var request = new SampleAuditableRequest(Guid.NewGuid(), "GlassProject");
        var captured = new[]
        {
            new AuditEntry(request.AggregateId, request.AggregateType, "FieldUpdate", "Name", "old", "new", DateTime.UtcNow),
        };
        _auditContext.PendingEntries.Returns(captured);
        _currentUser.UserId.Returns(Guid.NewGuid());
        var sut = new AuditBehavior<SampleAuditableRequest, SampleResponse>(
            _auditContext, _currentUser, _redactor, NullLogger<AuditBehavior<SampleAuditableRequest, SampleResponse>>.Instance);
        RequestHandlerDelegate<SampleResponse> next = () => Task.FromResult(new SampleResponse(true));

        var response = await sut.Handle(request, next, CancellationToken.None);

        response.Ok.Should().BeTrue();
        _auditContext.PendingEntries.Should().HaveCount(1);
        _auditContext.PendingEntries[0].Field.Should().Be("Name");
        _auditContext.DidNotReceive().Clear();
    }

    [Fact]
    public async Task Handler_throws_clears_audit_context()
    {
        var request = new SampleAuditableRequest(Guid.NewGuid(), "GlassProject");
        var sut = new AuditBehavior<SampleAuditableRequest, SampleResponse>(
            _auditContext, _currentUser, _redactor, NullLogger<AuditBehavior<SampleAuditableRequest, SampleResponse>>.Instance);
        RequestHandlerDelegate<SampleResponse> next = () => throw new InvalidOperationException("boom");

        var act = async () => await sut.Handle(request, next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _auditContext.Received(1).Clear();
    }

    [Fact]
    public async Task Non_auditable_request_does_not_compile_against_behavior_so_unmarked_requests_are_skipped_by_pipeline()
    {
        typeof(AuditBehavior<,>)
            .GetGenericArguments()[0]
            .GetGenericParameterConstraints()
            .Should()
            .Contain(t => t == typeof(IAuditableMutation));
    }

    public sealed record SampleAuditableRequest(Guid AggregateId, string AggregateType)
        : IRequest<SampleResponse>, IAuditableMutation;

    public sealed record SampleResponse(bool Ok);
}
