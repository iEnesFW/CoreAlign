using CoreAlign.Application.Tax;
using CoreAlign.Application.Tax.Commands;
using CoreAlign.Application.Tax.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Tax;

public class BuildBaBsForPeriodCommandHandlerTests
{
    private readonly ITaxDeclarationRepository _declarationRepository = Substitute.For<ITaxDeclarationRepository>();
    private readonly ITaxAggregationRepository _aggregationRepository = Substitute.For<ITaxAggregationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BuildBaBsForPeriodCommandHandler _sut;

    public BuildBaBsForPeriodCommandHandlerTests()
    {
        var options = Options.Create(new TaxOptions { BaBsThresholdTry = 5000m });
        _sut = new BuildBaBsForPeriodCommandHandler(
            _declarationRepository,
            _aggregationRepository,
            _unitOfWork,
            options);
    }

    [Fact]
    public async Task Happy_path_creates_declaration_with_customer_and_vendor_lines()
    {
        _aggregationRepository
            .GetCustomerInvoiceAggregatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 5000m, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new CustomerInvoiceAggregateRow(Guid.NewGuid(), "Acme", "1111111111", 2, 12000m, 2160m),
            });
        _aggregationRepository
            .GetVendorBillAggregatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 5000m, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new VendorBillAggregateRow(Guid.NewGuid(), "Supplier Co", "2222222222", 3, 9000m, 1620m),
            });
        _declarationRepository
            .GetByPeriodAsync(2026, 5, TaxDeclarationType.BabsBeyani, Arg.Any<CancellationToken>())
            .Returns((TaxDeclaration?)null);

        TaxDeclaration? added = null;
        await _declarationRepository.AddAsync(
            Arg.Do<TaxDeclaration>(d => added = d),
            Arg.Any<CancellationToken>());

        await _sut.Handle(new BuildBaBsForPeriodCommand(2026, 5), default);

        added.Should().NotBeNull();
        added!.TotalAmount.Should().Be(21000m);
        added.TaxAmount.Should().Be(3780m);
        added.Lines.Should().HaveCount(2);
        added.Lines.Should().Contain(l => l.CounterpartyName.StartsWith("[Bs]"));
        added.Lines.Should().Contain(l => l.CounterpartyName.StartsWith("[Ba]"));
    }

    [Fact]
    public async Task Aggregation_repository_filters_below_threshold_so_handler_relies_on_min_threshold_parameter()
    {
        _aggregationRepository
            .GetCustomerInvoiceAggregatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 5000m, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerInvoiceAggregateRow>());
        _aggregationRepository
            .GetVendorBillAggregatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 5000m, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VendorBillAggregateRow>());
        _declarationRepository
            .GetByPeriodAsync(2026, 5, TaxDeclarationType.BabsBeyani, Arg.Any<CancellationToken>())
            .Returns((TaxDeclaration?)null);

        TaxDeclaration? added = null;
        await _declarationRepository.AddAsync(
            Arg.Do<TaxDeclaration>(d => added = d),
            Arg.Any<CancellationToken>());

        await _sut.Handle(new BuildBaBsForPeriodCommand(2026, 5), default);

        added.Should().NotBeNull();
        added!.LineCount.Should().Be(0);
        added.TotalAmount.Should().Be(0m);
        await _aggregationRepository.Received(1).GetCustomerInvoiceAggregatesAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), 5000m, Arg.Any<CancellationToken>());
        await _aggregationRepository.Received(1).GetVendorBillAggregatesAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), 5000m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Idempotent_rerun_replaces_lines_on_existing_declaration()
    {
        var existing = new TaxDeclaration(2026, 5, TaxDeclarationType.BabsBeyani);
        existing.Generate("<old/>", 0m, 0m, 0m, 0);
        existing.ReplaceLines(new[]
        {
            new TaxDeclarationLine(existing.Id, "1", "Stale", 1, 100m, 18m),
        });
        _declarationRepository
            .GetByPeriodAsync(2026, 5, TaxDeclarationType.BabsBeyani, Arg.Any<CancellationToken>())
            .Returns(existing);
        _aggregationRepository
            .GetCustomerInvoiceAggregatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 5000m, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new CustomerInvoiceAggregateRow(Guid.NewGuid(), "Fresh", "9", 1, 6000m, 1080m),
            });
        _aggregationRepository
            .GetVendorBillAggregatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 5000m, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VendorBillAggregateRow>());

        await _sut.Handle(new BuildBaBsForPeriodCommand(2026, 5), default);

        await _declarationRepository.DidNotReceive().AddAsync(Arg.Any<TaxDeclaration>(), Arg.Any<CancellationToken>());
        existing.Lines.Should().HaveCount(1);
        existing.Lines.First().CounterpartyName.Should().Contain("Fresh");
    }
}
