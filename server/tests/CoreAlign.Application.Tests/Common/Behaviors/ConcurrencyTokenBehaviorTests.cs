using CoreAlign.Application.Common.Behaviors;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Common.Behaviors;

public class ConcurrencyTokenBehaviorTests
{
    [Fact]
    public async Task DbUpdateConcurrencyException_is_translated_to_domain_concurrency_exception()
    {
        var sut = new ConcurrencyTokenBehavior<SampleRequest, SampleResponse>(
            NullLogger<ConcurrencyTokenBehavior<SampleRequest, SampleResponse>>.Instance);
        RequestHandlerDelegate<SampleResponse> next = () =>
            throw new DbUpdateConcurrencyException("conflict");

        var act = async () => await sut.Handle(new SampleRequest(false), next, CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<DomainConcurrencyException>();
        thrown.Which.CurrentVersion.Should().Be(0);
        thrown.Which.AttemptedVersion.Should().Be(0);
        thrown.Which.ConflictingFields.Should().BeEmpty();
    }

    [Fact]
    public async Task Force_overwrite_is_rejected_as_unsupported_and_does_not_retry()
    {
        var sut = new ConcurrencyTokenBehavior<SampleRequest, SampleResponse>(
            NullLogger<ConcurrencyTokenBehavior<SampleRequest, SampleResponse>>.Instance);
        var calls = 0;
        RequestHandlerDelegate<SampleResponse> next = () =>
        {
            calls++;
            throw new DbUpdateConcurrencyException("conflict");
        };

        var act = async () => await sut.Handle(new SampleRequest(true), next, CancellationToken.None);

        // Re-running next() would re-run the whole handler and DOUBLE-APPLY its mutation
        // (INVARIANTS §88), so a force-overwrite request must fail loudly, not silently retry.
        await act.Should().ThrowAsync<NotSupportedException>();
        calls.Should().Be(1);
    }

    [Fact]
    public async Task Non_concurrency_exceptions_propagate_unchanged()
    {
        var sut = new ConcurrencyTokenBehavior<SampleRequest, SampleResponse>(
            NullLogger<ConcurrencyTokenBehavior<SampleRequest, SampleResponse>>.Instance);
        RequestHandlerDelegate<SampleResponse> next = () =>
            throw new InvalidOperationException("unrelated");

        var act = async () => await sut.Handle(new SampleRequest(false), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("unrelated");
    }

    public sealed record SampleRequest(bool ForceOverwrite) : IRequest<SampleResponse>, IForceConcurrencyOverride;

    public sealed record SampleResponse(bool Ok);
}
