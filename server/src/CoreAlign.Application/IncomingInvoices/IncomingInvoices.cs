using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.IncomingInvoices;

public sealed record IncomingInvoiceDto(
    Guid Id,
    string Ettn,
    string SenderVkn,
    string? SenderName,
    string InvoiceNumber,
    DateTime IssueDate,
    string ProviderName,
    string? ProviderStatus,
    IncomingInvoiceStatus Status,
    Guid? LinkedVendorBillId,
    DateTime? ProcessedAtUtc,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record IncomingInvoiceListResult(IReadOnlyList<IncomingInvoiceDto> Items, int Total, int Page, int PageSize);

public sealed record ListIncomingInvoicesQuery(IncomingInvoiceStatus? Status, int Page = 1, int PageSize = 20)
    : IRequest<IncomingInvoiceListResult>;

public sealed record GetIncomingInvoiceQuery(Guid Id) : IRequest<IncomingInvoiceDto>;

public sealed record ProcessIncomingInvoiceCommand(
    Guid Id,
    decimal Subtotal,
    decimal TaxAmount,
    string? VendorName = null,
    string? Currency = null) : IRequest<IncomingInvoiceProcessResult>, ITransactionalRequest;

public sealed record IncomingInvoiceProcessResult(Guid IncomingInvoiceId, Guid VendorBillId, Guid VendorId, bool VendorCreated);

public sealed record IgnoreIncomingInvoiceCommand(Guid Id, string? Reason = null)
    : IRequest<IncomingInvoiceDto>, ITransactionalRequest;

public sealed class ProcessIncomingInvoiceCommandValidator : AbstractValidator<ProcessIncomingInvoiceCommand>
{
    public ProcessIncomingInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Subtotal).GreaterThanOrEqualTo(0m).WithMessage("Validation.NonNegative");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0m).WithMessage("Validation.NonNegative");
        RuleFor(x => x.VendorName)
            .MaximumLength(300).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.VendorName));
    }
}

public sealed class IgnoreIncomingInvoiceCommandValidator : AbstractValidator<IgnoreIncomingInvoiceCommand>
{
    public IgnoreIncomingInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

internal static class IncomingInvoiceMapper
{
    public static IncomingInvoiceDto ToDto(IncomingInvoice x) => new(
        x.Id, x.Ettn, x.SenderVkn, x.SenderName, x.InvoiceNumber, x.IssueDate,
        x.ProviderName, x.ProviderStatus, x.Status, x.LinkedVendorBillId, x.ProcessedAtUtc, x.Notes, x.CreatedAtUtc);
}

public sealed class ListIncomingInvoicesHandler : IRequestHandler<ListIncomingInvoicesQuery, IncomingInvoiceListResult>
{
    private readonly IIncomingInvoiceRepository _repository;

    public ListIncomingInvoicesHandler(IIncomingInvoiceRepository repository) => _repository = repository;

    public async Task<IncomingInvoiceListResult> Handle(ListIncomingInvoicesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var (items, total) = await _repository.SearchAsync(request.Status, page, pageSize, cancellationToken);
        return new IncomingInvoiceListResult(
            items.Select(IncomingInvoiceMapper.ToDto).ToList(), total, page, pageSize);
    }
}

public sealed class GetIncomingInvoiceHandler : IRequestHandler<GetIncomingInvoiceQuery, IncomingInvoiceDto>
{
    private readonly IIncomingInvoiceRepository _repository;

    public GetIncomingInvoiceHandler(IIncomingInvoiceRepository repository) => _repository = repository;

    public async Task<IncomingInvoiceDto> Handle(GetIncomingInvoiceQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new IncomingInvoiceNotFoundException();
        return IncomingInvoiceMapper.ToDto(invoice);
    }
}

public sealed class ProcessIncomingInvoiceHandler : IRequestHandler<ProcessIncomingInvoiceCommand, IncomingInvoiceProcessResult>
{
    private const string DefaultCurrency = "TRY";

    private readonly IIncomingInvoiceRepository _incoming;
    private readonly IVendorRepository _vendors;
    private readonly IVendorBillRepository _bills;

    public ProcessIncomingInvoiceHandler(
        IIncomingInvoiceRepository incoming,
        IVendorRepository vendors,
        IVendorBillRepository bills)
    {
        _incoming = incoming;
        _vendors = vendors;
        _bills = bills;
    }

    public async Task<IncomingInvoiceProcessResult> Handle(ProcessIncomingInvoiceCommand request, CancellationToken cancellationToken)
    {
        var incoming = await _incoming.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new IncomingInvoiceNotFoundException();

        var vendorCreated = false;
        Vendor? vendor = string.IsNullOrWhiteSpace(incoming.SenderVkn)
            ? null
            : await _vendors.GetByTaxNumberAsync(incoming.SenderVkn, cancellationToken);

        if (vendor is null)
        {
            var vendorName = request.VendorName
                ?? incoming.SenderName
                ?? (string.IsNullOrWhiteSpace(incoming.SenderVkn) ? "Tedarikçi" : $"Tedarikçi {incoming.SenderVkn}");
            vendor = new Vendor(
                name: vendorName,
                type: VendorType.Business,
                taxNumber: string.IsNullOrWhiteSpace(incoming.SenderVkn) ? null : incoming.SenderVkn);
            await _vendors.AddAsync(vendor, cancellationToken);
            vendorCreated = true;
        }

        var billNumber = string.IsNullOrWhiteSpace(incoming.InvoiceNumber) ? incoming.Ettn : incoming.InvoiceNumber;
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? DefaultCurrency : request.Currency.ToUpperInvariant();

        if (await _bills.BillNumberExistsAsync(vendor.Id, billNumber, null, cancellationToken))
        {
            throw new DuplicateVendorBillNumberException();
        }

        var bill = new VendorBill(
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            billNumber: billNumber,
            billDate: incoming.IssueDate,
            currency: currency,
            subtotal: request.Subtotal,
            taxAmount: request.TaxAmount,
            notes: $"Gelen e-Fatura: {incoming.Ettn}");
        await _bills.AddAsync(bill, cancellationToken);

        incoming.MarkProcessed(bill.Id);

        return new IncomingInvoiceProcessResult(incoming.Id, bill.Id, vendor.Id, vendorCreated);
    }
}

public sealed class IgnoreIncomingInvoiceHandler : IRequestHandler<IgnoreIncomingInvoiceCommand, IncomingInvoiceDto>
{
    private readonly IIncomingInvoiceRepository _repository;

    public IgnoreIncomingInvoiceHandler(IIncomingInvoiceRepository repository) => _repository = repository;

    public async Task<IncomingInvoiceDto> Handle(IgnoreIncomingInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new IncomingInvoiceNotFoundException();
        invoice.MarkIgnored(request.Reason);
        return IncomingInvoiceMapper.ToDto(invoice);
    }
}
