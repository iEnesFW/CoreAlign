using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Marketplace.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Marketplace;

public class ProjectMarketplaceServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private readonly IProjectTemplateRepository _templates = Substitute.For<IProjectTemplateRepository>();
    private readonly IProjectTemplateReviewRepository _reviews = Substitute.For<IProjectTemplateReviewRepository>();
    private readonly IProjectTemplateInstallRepository _installs = Substitute.For<IProjectTemplateInstallRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly ProjectMarketplaceService _sut;

    public ProjectMarketplaceServiceTests()
    {
        _tenantContext.RequireTenantId().Returns(TenantId);
        _tenantContext.CurrentTenantId.Returns(TenantId);
        _currentUser.UserIdOrThrow().Returns(UserId);

        _sut = new ProjectMarketplaceService(
            _templates, _reviews, _installs, _tenantContext, _currentUser);
    }

    [Fact]
    public async Task Submit_transitions_tenant_template_to_submitted()
    {
        var template = BuildTenantTemplate();
        _templates.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var dto = await _sut.SubmitToMarketplaceAsync(template.Id, UserId);

        dto.Visibility.Should().Be(ProjectTemplateVisibility.MarketplaceSubmitted);
        template.SubmittedByTenantId.Should().Be(TenantId);
        template.SubmittedAtUtc.Should().NotBeNull();
        _templates.Received(1).Update(template);
    }

    [Fact]
    public async Task Submit_rejects_global_system_template()
    {
        var systemTemplate = BuildSystemTemplate();
        _templates.GetByIdAsync(systemTemplate.Id, Arg.Any<CancellationToken>()).Returns(systemTemplate);

        Func<Task> act = () => _sut.SubmitToMarketplaceAsync(systemTemplate.Id, UserId);

        await act.Should().ThrowAsync<MarketplaceCannotSubmitGlobalTemplateException>();
        _templates.DidNotReceive().Update(Arg.Any<ProjectTemplate>());
    }

    [Fact]
    public async Task Submit_throws_when_template_missing()
    {
        _templates.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProjectTemplate?)null);

        Func<Task> act = () => _sut.SubmitToMarketplaceAsync(Guid.NewGuid(), UserId);

        await act.Should().ThrowAsync<ProjectTemplateNotFoundException>();
    }

    [Fact]
    public async Task Publish_promotes_submitted_template_to_published()
    {
        var template = BuildTenantTemplate();
        template.SubmitToMarketplace(TenantId);
        _templates.GetByIdIgnoringTenantAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var dto = await _sut.PublishAsync(template.Id, AdminUserId);

        dto.Visibility.Should().Be(ProjectTemplateVisibility.MarketplacePublished);
        template.PublishedAtUtc.Should().NotBeNull();
        _templates.Received(1).Update(template);
    }

    [Fact]
    public async Task Publish_rejects_non_submitted_state()
    {
        var template = BuildTenantTemplate();
        _templates.GetByIdIgnoringTenantAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        Func<Task> act = () => _sut.PublishAsync(template.Id, AdminUserId);

        await act.Should().ThrowAsync<MarketplaceTemplateInvalidStateException>();
    }

    [Fact]
    public async Task Reject_sets_rejection_reason_and_state()
    {
        var template = BuildTenantTemplate();
        template.SubmitToMarketplace(TenantId);
        _templates.GetByIdIgnoringTenantAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var dto = await _sut.RejectAsync(template.Id, "Not enough detail");

        dto.Visibility.Should().Be(ProjectTemplateVisibility.MarketplaceRejected);
        dto.RejectionReason.Should().Be("Not enough detail");
        _templates.Received(1).Update(template);
    }

    [Fact]
    public async Task Install_clones_published_template_for_other_tenant()
    {
        var source = BuildTenantTemplate();
        source.SubmitToMarketplace(OtherTenantId);
        source.Publish(AdminUserId);
        _templates.GetByIdWithPresetsIgnoringTenantAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);

        var initialDownloadCount = source.DownloadCount;

        var result = await _sut.InstallToTenantAsync(source.Id);

        result.InstalledTemplateId.Should().NotBe(source.Id);
        await _templates.Received(1).AddAsync(Arg.Is<ProjectTemplate>(t =>
            t.TenantId == TenantId &&
            t.Visibility == ProjectTemplateVisibility.TenantOnly &&
            !t.IsSystemTemplate), Arg.Any<CancellationToken>());
        await _installs.Received(1).AddAsync(Arg.Any<ProjectTemplateInstall>(), Arg.Any<CancellationToken>());
        source.DownloadCount.Should().Be(initialDownloadCount + 1);
    }

    [Fact]
    public async Task Install_rejects_when_template_not_published()
    {
        var source = BuildTenantTemplate();
        source.SubmitToMarketplace(OtherTenantId);
        _templates.GetByIdWithPresetsIgnoringTenantAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);

        Func<Task> act = () => _sut.InstallToTenantAsync(source.Id);

        await act.Should().ThrowAsync<MarketplaceTemplateNotPublishedException>();
        await _templates.DidNotReceive().AddAsync(Arg.Any<ProjectTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Install_rejects_when_tenant_attempts_to_install_own_submission()
    {
        var source = BuildTenantTemplate();
        source.SubmitToMarketplace(TenantId);
        source.Publish(AdminUserId);
        _templates.GetByIdWithPresetsIgnoringTenantAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);

        Func<Task> act = () => _sut.InstallToTenantAsync(source.Id);

        await act.Should().ThrowAsync<MarketplaceCannotInstallOwnSubmissionException>();
    }

    [Fact]
    public async Task Rate_creates_review_and_updates_aggregate_when_first_rating()
    {
        var template = BuildTenantTemplate();
        template.SubmitToMarketplace(OtherTenantId);
        template.Publish(AdminUserId);
        _templates.GetByIdIgnoringTenantAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        _reviews.GetByTemplateAndReviewerAsync(template.Id, UserId, Arg.Any<CancellationToken>())
            .Returns((ProjectTemplateReview?)null);
        _reviews.GetAggregateAsync(template.Id, Arg.Any<CancellationToken>())
            .Returns((0, (decimal?)null));

        var dto = await _sut.RateAsync(template.Id, 5, "Great template");

        dto.RatingStars.Should().Be(5);
        dto.CommentMd.Should().Be("Great template");
        await _reviews.Received(1).AddAsync(Arg.Any<ProjectTemplateReview>(), Arg.Any<CancellationToken>());
        template.AverageRating.Should().Be(5m);
        template.ReviewCount.Should().Be(1);
    }

    [Fact]
    public async Task Rate_updates_existing_review_instead_of_inserting()
    {
        var template = BuildTenantTemplate();
        template.SubmitToMarketplace(OtherTenantId);
        template.Publish(AdminUserId);
        _templates.GetByIdIgnoringTenantAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var existing = new ProjectTemplateReview(template.Id, UserId, 3, "Old") { TenantId = TenantId };
        _reviews.GetByTemplateAndReviewerAsync(template.Id, UserId, Arg.Any<CancellationToken>()).Returns(existing);
        _reviews.GetAggregateAsync(template.Id, Arg.Any<CancellationToken>())
            .Returns((1, (decimal?)4m));

        var dto = await _sut.RateAsync(template.Id, 4, "Updated");

        dto.RatingStars.Should().Be(4);
        await _reviews.DidNotReceive().AddAsync(Arg.Any<ProjectTemplateReview>(), Arg.Any<CancellationToken>());
        _reviews.Received(1).Update(existing);
    }

    [Fact]
    public async Task Rate_rejects_when_template_not_published()
    {
        var template = BuildTenantTemplate();
        _templates.GetByIdIgnoringTenantAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        Func<Task> act = () => _sut.RateAsync(template.Id, 4, null);

        await act.Should().ThrowAsync<MarketplaceTemplateNotPublishedException>();
    }

    private static ProjectTemplate BuildTenantTemplate(bool isSystem = false)
    {
        var template = new ProjectTemplate(
            code: "CUSTOM-BALCONY-01",
            displayNameKey: "Marketplace.Balcony.Custom",
            isSystemTemplate: isSystem,
            category: EnclosureCategory.Vertical,
            subtype: EnclosureSubtype.Balcony,
            geometryMode: GeometryMode.Planar,
            mountingTopology: MountingTopology.ProfileFramed,
            defaultConnectorKind: ConnectorKind.Profile)
        {
            TenantId = TenantId,
        };
        return template;
    }

    private static ProjectTemplate BuildSystemTemplate() => new ProjectTemplate(
        code: "SYS-BALCONY-DEFAULT",
        displayNameKey: "Marketplace.Balcony.System",
        isSystemTemplate: true,
        category: EnclosureCategory.Vertical,
        subtype: EnclosureSubtype.Balcony,
        geometryMode: GeometryMode.Planar,
        mountingTopology: MountingTopology.ProfileFramed,
        defaultConnectorKind: ConnectorKind.Profile)
    {
        TenantId = Guid.Empty,
    };
}
