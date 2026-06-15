using CoreAlign.Application.B2B;
using CoreAlign.Application.Platform.Tenants;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Platform;

public class PlatformTenantHandlerTests
{
    private readonly IPlatformTenantRepository _repo = Substitute.For<IPlatformTenantRepository>();
    private readonly ITenantRepository _baseRepo = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserAccessor _user = Substitute.For<ICurrentUserAccessor>();

    [Fact]
    public async Task List_returns_paged_result_with_total_and_mapping()
    {
        var t1 = new Tenant("Acme", "acme");
        var t2 = new Tenant("Globex", "globex");
        _repo.SearchAsync(null, 1, 20, false, Arg.Any<CancellationToken>())
            .Returns((new[] { t1, t2 }, 5));

        var sut = new ListPlatformTenantsHandler(_repo);
        var result = await sut.Handle(new ListPlatformTenantsQuery(null), CancellationToken.None);

        result.Total.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Name == "Acme" && i.Slug == "acme");
    }

    [Fact]
    public async Task Update_rejects_slug_already_in_use_by_another_tenant()
    {
        var tenant = new Tenant("Acme", "acme");
        _repo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _repo.SlugExistsAsync("taken-slug", tenant.Id, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new UpdatePlatformTenantHandler(_repo, _baseRepo, _uow);
        var act = () => sut.Handle(new UpdatePlatformTenantCommand(tenant.Id, "Acme", "taken-slug", null, "dpo@acme.io"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_persists_changes_and_lowercases_slug()
    {
        var tenant = new Tenant("Acme", "acme");
        _repo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _repo.SlugExistsAsync(Arg.Any<string>(), tenant.Id, Arg.Any<CancellationToken>()).Returns(false);

        var sut = new UpdatePlatformTenantHandler(_repo, _baseRepo, _uow);
        var dto = await sut.Handle(new UpdatePlatformTenantCommand(tenant.Id, " New Name ", "NEW-SLUG", "DPO Name", "dpo@new.io"), CancellationToken.None);

        dto.Name.Should().Be("New Name");
        dto.Slug.Should().Be("new-slug");
        dto.DpoContactEmail.Should().Be("dpo@new.io");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_sets_flag_and_deactivates_and_returns_true()
    {
        var tenant = new Tenant("Acme", "acme");
        _repo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _user.UserId.Returns(Guid.NewGuid());

        var sut = new ArchivePlatformTenantHandler(_repo, _baseRepo, _uow, _user);
        var ok = await sut.Handle(new ArchivePlatformTenantCommand(tenant.Id), CancellationToken.None);

        ok.Should().BeTrue();
        tenant.IsArchived.Should().BeTrue();
        tenant.IsActive.Should().BeFalse();
        tenant.ArchivedAtUtc.Should().NotBeNull();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restore_clears_archive_flag_and_reactivates()
    {
        var tenant = new Tenant("Acme", "acme");
        tenant.Archive(Guid.NewGuid(), DateTime.UtcNow);
        _repo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var sut = new RestorePlatformTenantHandler(_repo, _baseRepo, _uow);
        var ok = await sut.Handle(new RestorePlatformTenantCommand(tenant.Id), CancellationToken.None);

        ok.Should().BeTrue();
        tenant.IsArchived.Should().BeFalse();
        tenant.IsActive.Should().BeTrue();
        tenant.ArchivedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Archive_missing_tenant_returns_false_without_saving()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var sut = new ArchivePlatformTenantHandler(_repo, _baseRepo, _uow, _user);
        var ok = await sut.Handle(new ArchivePlatformTenantCommand(Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
