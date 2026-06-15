using CoreAlign.Application.Accounting.Handlers;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.BugHuntD;

public class FinancialGetByIdNotFoundContractTests
{
    [Fact]
    public async Task GetGLAccountById_for_missing_or_cross_tenant_id_throws_NotFound_not_returns_null()
    {
        var repo = Substitute.For<IGLAccountRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GLAccount?)null);
        var sut = new GetGLAccountByIdHandler(repo);

        var act = async () => await sut.Handle(new GetGLAccountByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetJournalEntryById_for_missing_or_cross_tenant_id_throws_NotFound_not_returns_null()
    {
        var repo = Substitute.For<IJournalEntryRepository>();
        repo.GetWithLinesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((JournalEntry?)null);
        var sut = new GetJournalEntryByIdHandler(repo);

        var act = async () => await sut.Handle(new GetJournalEntryByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAccountingPeriodById_for_missing_or_cross_tenant_id_throws_NotFound_not_returns_null()
    {
        var repo = Substitute.For<IAccountingPeriodRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AccountingPeriod?)null);
        var sut = new GetAccountingPeriodByIdHandler(repo);

        var act = async () => await sut.Handle(new GetAccountingPeriodByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetVendorBillById_for_missing_or_cross_tenant_id_throws_NotFound_not_returns_null()
    {
        var repo = Substitute.For<IVendorBillRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((VendorBill?)null);
        var sut = new GetVendorBillByIdHandler(repo);

        var act = async () => await sut.Handle(new GetVendorBillByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
