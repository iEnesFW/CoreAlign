using CoreAlign.Application.Payroll.Employees;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAlign.Application.Tests.Payroll;

public sealed class PayrollSequenceIntegrationTests : IDisposable
{
    private readonly CoreAlignDbContext _db;
    private readonly CreateEmployeeHandler _sut;

    public PayrollSequenceIntegrationTests()
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(Guid.NewGuid());
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(Guid.NewGuid());

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase($"payroll-seq-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new CoreAlignDbContext(options, tenant, Substitute.For<IPublisher>());
        _db.Database.EnsureCreated();

        _sut = new CreateEmployeeHandler(
            new EmployeeRepository(_db),
            new DocumentSequenceRepository(_db),
            new UnitOfWork(_db));
    }

    private static CreateEmployeeCommand Command(string nationalId) => new(
        FirstName: "Ada",
        LastName: "Yilmaz",
        NationalId: nationalId,
        HireDate: new DateOnly(2026, 1, 1),
        BaseSalaryGross: 60000m);

    [Fact]
    public async Task Create_lazily_creates_the_PER_sequence_without_concurrency_conflict()
    {
        var result = await _sut.Handle(Command("12345678901"), default);

        result.EmployeeNumber.Should().StartWith("PER-");
        result.EmployeeNumber.Should().EndWith("00001");
    }

    [Fact]
    public async Task Second_create_increments_the_persisted_sequence()
    {
        var first = await _sut.Handle(Command("12345678901"), default);
        var second = await _sut.Handle(Command("98765432109"), default);

        first.EmployeeNumber.Should().EndWith("00001");
        second.EmployeeNumber.Should().EndWith("00002");
    }

    public void Dispose() => _db.Dispose();
}
