using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class GenerateInvoiceFromOrderCommandHandler : IRequestHandler<GenerateInvoiceFromOrderCommand, InvoiceDto>
{
    private static readonly OrderStatus[] EligibleOrderStatuses =
    {
        OrderStatus.Confirmed,
        OrderStatus.Shipped,
        OrderStatus.PartiallyShipped,
        OrderStatus.Delivered,
        OrderStatus.Closed
    };

    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IAccountingPeriodRepository? _periodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateInvoiceFromOrderCommandHandler(
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        IDocumentSequenceRepository sequenceRepository,
        IAccountingPeriodRepository periodRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _sequenceRepository = sequenceRepository;
        _periodRepository = periodRepository;
        _unitOfWork = unitOfWork;
    }

    public GenerateInvoiceFromOrderCommandHandler(
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork)
        : this(orderRepository, invoiceRepository, null!, null!, unitOfWork)
    {
    }

    public async Task<InvoiceDto> Handle(GenerateInvoiceFromOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        if (!EligibleOrderStatuses.Contains(order.Status))
        {
            throw new OrderNotEligibleForInvoicingException(order.Status.ToString());
        }

        if (await _invoiceRepository.ExistsForOrderAsync(order.Id, cancellationToken))
        {
            throw new InvoiceAlreadyExistsForOrderException();
        }

        var now = DateTime.UtcNow;
        if (_periodRepository is not null)
        {
            var period = await _periodRepository.GetByDateAsync(now.Date, cancellationToken);
            period?.EnsurePostingAllowed(now);
        }

        var draftNumber = _sequenceRepository is null
            ? InvoiceMapper.GenerateInvoiceNumber()
            : await _sequenceRepository.ConsumeAsync(DocumentSequenceType.InvoiceNumber, now, cancellationToken);

        var invoice = new Invoice(
            draftNumber,
            order.CustomerId,
            order.Customer?.Name ?? order.CustomerSnapshot?.LegalName ?? string.Empty,
            order.Currency);

        invoice.AttachToOrder(order.Id);
        invoice.UpdateDetails(
            issueDate: now,
            dueDate: now.AddDays(request.DueDays),
            postingDate: now.Date,
            exchangeRate: order.ExchangeRate,
            paymentTermsId: order.PaymentTermsId,
            paymentTermsNetDaysSnapshot: order.PaymentTermsNetDaysSnapshot,
            headerDiscountPercent: order.HeaderDiscountPercent,
            headerDiscountAmount: order.HeaderDiscountAmount,
            shippingCost: order.ShippingCost,
            roundingAdjustment: order.RoundingAdjustment,
            internalNotes: null,
            publicNotes: null,
            termsAndConditions: null,
            notes: request.Notes);

        if (order.CustomerSnapshot != null)
        {
            invoice.ApplySnapshots(order.CustomerSnapshot, order.BillingAddressSnapshot, order.ShippingAddressSnapshot);
        }
        else if (order.Customer is not null)
        {
            var cs = new CustomerSnapshot
            {
                Code = order.Customer.Code,
                LegalName = order.Customer.LegalName ?? order.Customer.Name,
                TradeName = order.Customer.TradeName,
                TaxNumber = order.Customer.TaxNumber,
                TaxOffice = order.Customer.TaxOffice,
                NationalId = order.Customer.NationalId,
                Email = order.Customer.Email,
                Phone = order.Customer.Phone,
            };
            invoice.ApplySnapshots(cs, null, null);
        }

        var lineNumber = 1;
        foreach (var line in order.Lines)
        {
            var invLine = new InvoiceLine(line.ProductId, line.ProductSku, line.ProductName, line.Quantity, line.UnitPrice);
            invLine.SetLineNumber(lineNumber++);
            invLine.ApplyPricing(
                quantity: line.Quantity,
                unitPrice: line.UnitPrice,
                lineDiscountPercent: line.LineDiscountPercent,
                lineDiscountAmount: line.LineDiscountAmount,
                taxRatePercent: line.TaxRatePercent,
                taxRateId: line.TaxRateId,
                isTaxInclusive: line.IsTaxInclusive,
                withholdingRatePercent: line.WithholdingRatePercent,
                uomId: line.UomId,
                uomCode: line.UomCode,
                description: line.ProductDescriptionSnapshot,
                revenueAccountCode: null,
                costCenter: null,
                project: null,
                originOrderLineId: line.Id);
            invoice.Lines.Add(invLine);
        }

        invoice.Issue(draftNumber);

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (order.Customer is not null)
        {
            invoice.Customer = order.Customer;
        }
        return InvoiceMapper.ToDto(invoice);
    }
}
