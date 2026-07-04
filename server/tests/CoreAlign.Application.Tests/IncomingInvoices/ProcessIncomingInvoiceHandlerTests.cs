using CoreAlign.Application.IncomingInvoices;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.IncomingInvoices;

public class ProcessIncomingInvoiceHandlerTests
{
    private readonly IIncomingInvoiceRepository _incoming = Substitute.For<IIncomingInvoiceRepository>();
    private readonly IVendorRepository _vendors = Substitute.For<IVendorRepository>();
    private readonly IVendorBillRepository _bills = Substitute.For<IVendorBillRepository>();

    private ProcessIncomingInvoiceHandler BuildHandler() => new(_incoming, _vendors, _bills);

    private static IncomingInvoice BuildIncoming(string vkn = "1234567890") => new(
        "ETTN-1", vkn, "Tedarikçi A.Ş.", "GIB-2026-001", new DateTime(2026, 7, 1), "nilvera", "Delivered");

    [Fact]
    public async Task Processing_with_existing_vendor_creates_bill_and_links_it()
    {
        var incoming = BuildIncoming();
        var vendor = new Vendor("Tedarikçi A.Ş.", VendorType.Business, taxNumber: "1234567890");
        _incoming.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(incoming);
        _vendors.GetByTaxNumberAsync("1234567890", Arg.Any<CancellationToken>()).Returns(vendor);
        _bills.BillNumberExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await BuildHandler().Handle(
            new ProcessIncomingInvoiceCommand(incoming.Id, 1000m, 200m), CancellationToken.None);

        result.VendorCreated.Should().BeFalse();
        result.VendorId.Should().Be(vendor.Id);
        result.VendorBillId.Should().NotBeEmpty();
        incoming.Status.Should().Be(IncomingInvoiceStatus.Processed);
        incoming.LinkedVendorBillId.Should().Be(result.VendorBillId);
        await _bills.Received(1).AddAsync(Arg.Any<VendorBill>(), Arg.Any<CancellationToken>());
        await _vendors.DidNotReceive().AddAsync(Arg.Any<Vendor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Processing_without_matching_vendor_quick_creates_one_from_vkn()
    {
        var incoming = BuildIncoming("9998887776");
        _incoming.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(incoming);
        _vendors.GetByTaxNumberAsync("9998887776", Arg.Any<CancellationToken>()).Returns((Vendor?)null);
        _bills.BillNumberExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await BuildHandler().Handle(
            new ProcessIncomingInvoiceCommand(incoming.Id, 500m, 100m), CancellationToken.None);

        result.VendorCreated.Should().BeTrue();
        await _vendors.Received(1).AddAsync(
            Arg.Is<Vendor>(v => v.TaxNumber == "9998887776"), Arg.Any<CancellationToken>());
        await _bills.Received(1).AddAsync(Arg.Any<VendorBill>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Processing_missing_invoice_throws_not_found()
    {
        _incoming.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((IncomingInvoice?)null);

        var act = () => BuildHandler().Handle(
            new ProcessIncomingInvoiceCommand(Guid.NewGuid(), 1m, 0m), CancellationToken.None);

        await act.Should().ThrowAsync<IncomingInvoiceNotFoundException>();
    }

    [Fact]
    public async Task Duplicate_bill_number_for_vendor_throws()
    {
        var incoming = BuildIncoming();
        var vendor = new Vendor("Tedarikçi", VendorType.Business, taxNumber: "1234567890");
        _incoming.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(incoming);
        _vendors.GetByTaxNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(vendor);
        _bills.BillNumberExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => BuildHandler().Handle(
            new ProcessIncomingInvoiceCommand(incoming.Id, 1000m, 200m), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateVendorBillNumberException>();
    }
}
