using CoreAlign.Application.Privacy;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.Privacy;

public sealed class PrivacyErasureAndGuardTests
{
    private readonly IDataSubjectRequestRepository _repo = Substitute.For<IDataSubjectRequestRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPiiAnonymizer _anonymizer = Substitute.For<IPiiAnonymizer>();
    private readonly IPrivacyDataReader _reader = Substitute.For<IPrivacyDataReader>();
    private readonly IPrivacyHasher _hasher = Substitute.For<IPrivacyHasher>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    private DataSubjectRequestService BuildService() =>
        new(_repo, _users, _anonymizer, _reader, _hasher, _tenant);

    private static DataSubjectRequest BuildRequest(Guid tenantId, DataSubjectRequestType type, Guid subjectUserId) =>
        DataSubjectRequest.Submit(tenantId, type, DateTime.UtcNow, subjectUserId, null, null, null, null);

    private static User BuildUser(Guid tenantId, Guid userId) =>
        new(tenantId, "subject", "subject@example.com", "hash") { Id = userId };

    [Fact]
    public async Task Erasure_rejects_subject_user_from_another_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();
        var request = BuildRequest(tenantA, DataSubjectRequestType.Erasure, subjectUserId);
        _repo.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _users.GetByIdAsync(subjectUserId, Arg.Any<CancellationToken>()).Returns(BuildUser(tenantB, subjectUserId));

        var act = () => BuildService().ProcessErasureRequestAsync(request.Id, keepFinancialTrail: false);

        await act.Should().ThrowAsync<PrivacyUserNotFoundException>();
        await _anonymizer.DidNotReceive().AnonymizeUserAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Erasure_allows_subject_user_in_same_tenant()
    {
        var tenant = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();
        var request = BuildRequest(tenant, DataSubjectRequestType.Erasure, subjectUserId);
        _repo.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _users.GetByIdAsync(subjectUserId, Arg.Any<CancellationToken>()).Returns(BuildUser(tenant, subjectUserId));

        await BuildService().ProcessErasureRequestAsync(request.Id, keepFinancialTrail: false);

        await _anonymizer.Received(1).AnonymizeUserAsync(subjectUserId, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_rejects_subject_user_from_another_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();
        _tenant.RequireTenantId().Returns(tenantA);
        _users.GetByIdAsync(subjectUserId, Arg.Any<CancellationToken>()).Returns(BuildUser(tenantB, subjectUserId));

        var act = () => BuildService().SubmitAsync(
            new SubmitDataSubjectRequestInput(DataSubjectRequestType.Erasure, subjectUserId, null, null, null));

        await act.Should().ThrowAsync<PrivacyUserNotFoundException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<DataSubjectRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildExport_returns_real_package_for_same_tenant_subject()
    {
        var tenant = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();
        var request = BuildRequest(tenant, DataSubjectRequestType.Access, subjectUserId);
        _repo.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _users.GetByIdAsync(subjectUserId, Arg.Any<CancellationToken>()).Returns(BuildUser(tenant, subjectUserId));
        _reader.GetUserOrdersAsync(subjectUserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<PersonalOrderDto>());
        _reader.GetUserActivityAsync(subjectUserId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<PersonalActivityDto>());
        _reader.GetCustomerMembershipsAsync(subjectUserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<PersonalMembershipDto>());
        _reader.GetDealerMembershipsAsync(subjectUserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<PersonalMembershipDto>());

        var package = await BuildService().BuildExportAsync(request.Id);

        package.Profile.Id.Should().Be(subjectUserId);
        package.Profile.Email.Should().Be("subject@example.com");
        await _reader.Received(1).GetUserOrdersAsync(subjectUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildExport_rejects_cross_tenant_subject()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();
        var request = BuildRequest(tenantA, DataSubjectRequestType.Access, subjectUserId);
        _repo.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _users.GetByIdAsync(subjectUserId, Arg.Any<CancellationToken>()).Returns(BuildUser(tenantB, subjectUserId));

        var act = () => BuildService().BuildExportAsync(request.Id);

        await act.Should().ThrowAsync<PrivacyUserNotFoundException>();
    }

    [Fact]
    public void Employee_anonymize_clears_pii_and_soft_deletes()
    {
        var employee = new Employee(
            employeeNumber: "E-1",
            firstName: "Ada",
            lastName: "Lovelace",
            nationalId: "12345678901",
            hireDate: new DateOnly(2020, 1, 1),
            baseSalaryGross: 50000m,
            sgkRegistrationNo: "SGK-42",
            email: "ada@example.com",
            phone: "+90 555 111 22 33",
            iban: "TR000000000000000000000001",
            bankName: "Demo Bank");

        employee.Anonymize(DateTime.UtcNow);

        employee.Iban.Should().BeNull();
        employee.SgkRegistrationNo.Should().BeNull();
        employee.Email.Should().BeNull();
        employee.Phone.Should().BeNull();
        employee.BankName.Should().BeNull();
        employee.NationalId.Should().Be("00000000000");
        employee.FirstName.Should().NotBe("Ada");
        employee.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task EraseUserCascade_anonymizes_linked_employee()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using (var seed = NewDb(conn, tenantId))
        {
            seed.Database.EnsureCreated();
            seed.Set<Tenant>().Add(new Tenant("Demo", "demo") { Id = tenantId });
            seed.Set<User>().Add(new User(tenantId, "grace", "grace@example.com", "hash") { Id = userId });
            var employee = new Employee(
                employeeNumber: "E-9",
                firstName: "Grace",
                lastName: "Hopper",
                nationalId: "99988877766",
                hireDate: new DateOnly(2019, 5, 1),
                baseSalaryGross: 42000m,
                sgkRegistrationNo: "SGK-9",
                email: "grace@example.com",
                iban: "TR000000000000000000000009",
                userId: userId);
            seed.Employees.Add(employee);
            await seed.SaveChangesAsync();
        }

        using (var act = NewDb(conn, tenantId))
        {
            var service = new PrivacyEraseService(act);
            await service.EraseUserCascadeAsync(userId, "grace@example.com", DateTime.UtcNow);
            await act.SaveChangesAsync();
        }

        using var verify = NewDb(conn, tenantId);
        var erased = await verify.Employees.IgnoreQueryFilters()
            .SingleAsync(e => e.UserId == userId);
        erased.Iban.Should().BeNull();
        erased.SgkRegistrationNo.Should().BeNull();
        erased.Email.Should().BeNull();
        erased.NationalId.Should().Be("00000000000");
        erased.IsDeleted.Should().BeTrue();
    }

    private static CoreAlignDbContext NewDb(SqliteConnection conn, Guid tenantId)
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(tenantId);
        return new CoreAlignDbContext(
            new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options,
            tenant,
            Substitute.For<IPublisher>());
    }
}
