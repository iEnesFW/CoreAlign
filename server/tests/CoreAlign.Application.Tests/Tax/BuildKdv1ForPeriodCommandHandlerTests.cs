using System.Text.Json;
using CoreAlign.Application.Tax.Commands;
using CoreAlign.Application.Tax.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Tax;

public class BuildKdv1ForPeriodCommandHandlerTests
{
    private readonly ITaxDeclarationRepository _declarationRepository = Substitute.For<ITaxDeclarationRepository>();
    private readonly ITaxAggregationRepository _aggregationRepository = Substitute.For<ITaxAggregationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BuildKdv1ForPeriodCommandHandler _sut;

    public BuildKdv1ForPeriodCommandHandlerTests()
    {
        _sut = new BuildKdv1ForPeriodCommandHandler(
            _declarationRepository,
            _aggregationRepository,
            _unitOfWork,
            NullLogger<BuildKdv1ForPeriodCommandHandler>.Instance);
    }

    [Fact]
    public async Task Happy_path_creates_new_declaration_with_xml()
    {
        var rows = new[]
        {
            BuildRow(taxable: 1000m, tax: 200m, withholding: 0m, rate: 20m),
            BuildRow(taxable: 500m, tax: 50m, withholding: 0m, rate: 10m),
        };
        _aggregationRepository
            .GetInvoiceTaxRowsForPeriodAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(rows);
        _declarationRepository
            .GetByPeriodAsync(2026, 5, TaxDeclarationType.Kdv1, Arg.Any<CancellationToken>())
            .Returns((TaxDeclaration?)null);

        TaxDeclaration? added = null;
        await _declarationRepository.AddAsync(
            Arg.Do<TaxDeclaration>(d => added = d),
            Arg.Any<CancellationToken>());

        var id = await _sut.Handle(new BuildKdv1ForPeriodCommand(2026, 5), default);

        added.Should().NotBeNull();
        added!.Status.Should().Be(TaxDeclarationStatus.Generated);
        added.TotalAmount.Should().Be(1500m);
        added.TaxAmount.Should().Be(250m);
        added.LineCount.Should().Be(2);
        added.XmlPayload.Should().NotBeNullOrEmpty();
        id.Should().Be(added.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Idempotent_rerun_replaces_existing_xml_without_duplicating_declaration()
    {
        var existing = new TaxDeclaration(2026, 5, TaxDeclarationType.Kdv1);
        existing.Generate("<old/>", 999m, 99m, 0m, 99);
        _declarationRepository
            .GetByPeriodAsync(2026, 5, TaxDeclarationType.Kdv1, Arg.Any<CancellationToken>())
            .Returns(existing);
        _aggregationRepository
            .GetInvoiceTaxRowsForPeriodAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { BuildRow(taxable: 2000m, tax: 400m, withholding: 0m, rate: 20m) });

        var id = await _sut.Handle(new BuildKdv1ForPeriodCommand(2026, 5), default);

        await _declarationRepository.DidNotReceive().AddAsync(Arg.Any<TaxDeclaration>(), Arg.Any<CancellationToken>());
        existing.TotalAmount.Should().Be(2000m);
        existing.TaxAmount.Should().Be(400m);
        existing.LineCount.Should().Be(1);
        existing.XmlPayload.Should().NotContain("<old");
        id.Should().Be(existing.Id);
    }

    [Fact]
    public async Task No_invoices_in_period_still_creates_declaration_with_zero_totals()
    {
        _aggregationRepository
            .GetInvoiceTaxRowsForPeriodAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<InvoiceTaxAggregateRow>());
        _declarationRepository
            .GetByPeriodAsync(2026, 5, TaxDeclarationType.Kdv1, Arg.Any<CancellationToken>())
            .Returns((TaxDeclaration?)null);

        TaxDeclaration? added = null;
        await _declarationRepository.AddAsync(
            Arg.Do<TaxDeclaration>(d => added = d),
            Arg.Any<CancellationToken>());

        await _sut.Handle(new BuildKdv1ForPeriodCommand(2026, 5), default);

        added.Should().NotBeNull();
        added!.TotalAmount.Should().Be(0m);
        added.TaxAmount.Should().Be(0m);
        added.LineCount.Should().Be(0);
        added.XmlPayload.Should().Contain("<Beyanname>");
    }

    [Fact]
    public async Task Skips_invoice_with_unparseable_breakdown_but_still_includes_totals()
    {
        var good = BuildRow(taxable: 1000m, tax: 200m, withholding: 0m, rate: 20m);
        var bad = new InvoiceTaxAggregateRow(Guid.NewGuid(), 500m, 100m, 0m, "{not-json");
        _aggregationRepository
            .GetInvoiceTaxRowsForPeriodAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { good, bad });
        _declarationRepository
            .GetByPeriodAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<TaxDeclarationType>(), Arg.Any<CancellationToken>())
            .Returns((TaxDeclaration?)null);

        TaxDeclaration? added = null;
        await _declarationRepository.AddAsync(
            Arg.Do<TaxDeclaration>(d => added = d),
            Arg.Any<CancellationToken>());

        await _sut.Handle(new BuildKdv1ForPeriodCommand(2026, 5), default);

        added!.TotalAmount.Should().Be(1500m);
        added.TaxAmount.Should().Be(300m);
    }

    private static InvoiceTaxAggregateRow BuildRow(decimal taxable, decimal tax, decimal withholding, decimal rate)
    {
        var breakdown = new[] { new { rate, @base = taxable, amount = tax } };
        var json = JsonSerializer.Serialize(breakdown);
        return new InvoiceTaxAggregateRow(Guid.NewGuid(), taxable, tax, withholding, json);
    }
}
