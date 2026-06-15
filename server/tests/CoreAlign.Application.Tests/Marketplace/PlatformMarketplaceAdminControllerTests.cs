using CoreAlign.API.Controllers;
using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.Marketplace.Commands;
using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Application.GlassEnclosure.Marketplace.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.Application.Tests.Marketplace;

public class PlatformMarketplaceAdminControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly PlatformMarketplaceAdminController _sut;

    public PlatformMarketplaceAdminControllerTests()
    {
        _sut = new PlatformMarketplaceAdminController(_mediator);
    }

    [Fact]
    public async Task ListPending_returns_pending_submissions_via_mediator()
    {
        var pending = new List<MarketplaceSubmissionDto>
        {
            new(Guid.NewGuid(), "CODE-1", "Name.Key.1", ProjectTemplateVisibility.MarketplaceSubmitted,
                DateTime.UtcNow, null, null, 0),
            new(Guid.NewGuid(), "CODE-2", "Name.Key.2", ProjectTemplateVisibility.MarketplaceSubmitted,
                DateTime.UtcNow, null, null, 0),
        };
        _mediator.Send(Arg.Any<ListPendingMarketplaceSubmissionsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<MarketplaceSubmissionDto>)pending);

        var result = await _sut.ListPending(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value.Should().BeOfType<ApiResponse<IReadOnlyList<MarketplaceSubmissionDto>>>().Subject;
        envelope.IsSuccess.Should().BeTrue();
        envelope.Data.Should().HaveCount(2);
        await _mediator.Received(1).Send(Arg.Any<ListPendingMarketplaceSubmissionsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_dispatches_publish_command_with_id_from_body()
    {
        var templateId = Guid.NewGuid();
        var publishedDto = new MarketplaceSubmissionDto(
            templateId, "CODE", "Key", ProjectTemplateVisibility.MarketplacePublished,
            DateTime.UtcNow, DateTime.UtcNow, null, 0);
        _mediator.Send(Arg.Is<PublishMarketplaceCommand>(c => c.TemplateId == templateId), Arg.Any<CancellationToken>())
            .Returns(publishedDto);

        var result = await _sut.Publish(new PublishMarketplaceDto(templateId), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value.Should().BeOfType<ApiResponse<MarketplaceSubmissionDto>>().Subject;
        envelope.Data!.Visibility.Should().Be(ProjectTemplateVisibility.MarketplacePublished);
        await _mediator.Received(1).Send(
            Arg.Is<PublishMarketplaceCommand>(c => c.TemplateId == templateId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_dispatches_reject_command_with_reason()
    {
        var templateId = Guid.NewGuid();
        var rejectedDto = new MarketplaceSubmissionDto(
            templateId, "CODE", "Key", ProjectTemplateVisibility.MarketplaceRejected,
            DateTime.UtcNow, null, "Bad metadata", 0);
        _mediator.Send(Arg.Is<RejectMarketplaceCommand>(c => c.TemplateId == templateId && c.Reason == "Bad metadata"),
                Arg.Any<CancellationToken>())
            .Returns(rejectedDto);

        var result = await _sut.Reject(new RejectMarketplaceDto(templateId, "Bad metadata"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value.Should().BeOfType<ApiResponse<MarketplaceSubmissionDto>>().Subject;
        envelope.Data!.RejectionReason.Should().Be("Bad metadata");
        await _mediator.Received(1).Send(
            Arg.Is<RejectMarketplaceCommand>(c => c.TemplateId == templateId && c.Reason == "Bad metadata"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_workflow_end_to_end_via_mediator_dispatch()
    {
        var templateId = Guid.NewGuid();
        var submission = new MarketplaceSubmissionDto(
            templateId, "CODE", "Key", ProjectTemplateVisibility.MarketplaceSubmitted,
            DateTime.UtcNow, null, null, 0);
        var published = submission with
        {
            Visibility = ProjectTemplateVisibility.MarketplacePublished,
            PublishedAtUtc = DateTime.UtcNow,
        };
        _mediator.Send(Arg.Any<ListPendingMarketplaceSubmissionsQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<MarketplaceSubmissionDto>)new List<MarketplaceSubmissionDto> { submission });
        _mediator.Send(Arg.Any<PublishMarketplaceCommand>(), Arg.Any<CancellationToken>()).Returns(published);

        var listResult = await _sut.ListPending(CancellationToken.None);
        var pendingEnvelope = ((OkObjectResult)listResult).Value
            .Should().BeOfType<ApiResponse<IReadOnlyList<MarketplaceSubmissionDto>>>().Subject;
        pendingEnvelope.Data.Should().ContainSingle(s => s.Id == templateId);

        var publishResult = await _sut.Publish(new PublishMarketplaceDto(templateId), CancellationToken.None);
        var publishEnvelope = ((OkObjectResult)publishResult).Value
            .Should().BeOfType<ApiResponse<MarketplaceSubmissionDto>>().Subject;
        publishEnvelope.Data!.Visibility.Should().Be(ProjectTemplateVisibility.MarketplacePublished);
    }
}
