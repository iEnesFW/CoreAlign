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
    public async Task Force_overwrite_retries_next_after_concurrency_failure()
    {
        var sut = new ConcurrencyTokenBehavior<SampleRequest, SampleResponse>(
            NullLogger<ConcurrencyTokenBehavior<SampleRequest, SampleResponse>>.Instance);
        var calls = 0;
        RequestHandlerDelegate<SampleResponse> next = () =>
        {
            calls++;
            if (calls == 1)
            {
                throw new DbUpdateConcurrencyException("conflict");
            }
            return Task.FromResult(new SampleResponse(true));
        };

        var response = await sut.Handle(new SampleRequest(true), next, CancellationToken.None);

        calls.Should().Be(2);
        response.Ok.Should().BeTrue();
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
