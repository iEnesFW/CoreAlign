using CoreAlign.Application.CustomerPortal.Credit;
using CoreAlign.Application.EInvoice;
using CoreAlign.Application.Fx;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class CreateStandaloneInvoiceCommandHandler : IRequestHandler<CreateStandaloneInvoiceCommand, InvoiceDto>
{
    private const string BaseCurrency = "TRY";

    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerAddressRepository _addressRepository;
    private readonly IPaymentTermRepository _paymentTermRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IAccountingPeriodRepository _periodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IEInvoiceSubmissionOutbox _eInvoiceOutbox;
    private readonly ICreditLimitGuard _creditGuard;
    private readonly IFxRateResolverDetailed? _fxResolver;
    private readonly ITenantContext? _tenantContext;
    private readonly IGibCodeRepository? _gibCodeRepository;

    public CreateStandaloneInvoiceCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerAddressRepository addressRepository,
        IPaymentTermRepository paymentTermRepository,
        IInvoiceRepository invoiceRepository,
        IProductRepository productRepository,
        IDocumentSequenceRepository sequenceRepository,
        IAccountingPeriodRepository periodRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IEInvoiceSubmissionOutbox eInvoiceOutbox,
        ICreditLimitGuard creditGuard,
        IFxRateResolverDetailed? fxResolver = null,
        ITenantContext? tenantContext = null,
        IGibCodeRepository? gibCodeRepository = null)
    {
        _customerRepository = customerRepository;
        _addressRepository = addressRepository;
        _paymentTermRepository = paymentTermRepository;
        _invoiceRepository = invoiceRepository;
        _productRepository = productRepository;
        _sequenceRepository = sequenceRepository;
        _periodRepository = periodRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _eInvoiceOutbox = eInvoiceOutbox;
        _creditGuard = creditGuard;
        _fxResolver = fxResolver;
        _tenantContext = tenantContext;
        _gibCodeRepository = gibCodeRepository;
    }

    public async Task<InvoiceDto> Handle(CreateStandaloneInvoiceCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIdsWithValue = request.Lines
            .Where(l => l.ProductId.HasValue)
            .Select(l => l.ProductId!.Value)
            .Distinct()
            .ToList();
        var productsById = productIdsWithValue.Count == 0
            ? new Dictionary<Guid, Product>()
            : await _productRepository.GetByIdsAsync(productIdsWithValue, cancellationToken);
        if (productsById.Count != productIdsWithValue.Count)
        {
            throw new InvalidInvoiceLineException("Validation.ProductNotFoundOrCrossTenant");
        }

        var withholdingCodeIds = request.Lines
            .Where(l => l.WithholdingTaxCodeId.HasValue)
            .Select(l => l.WithholdingTaxCodeId!.Value)
            .Distinct()
            .ToList();
        IReadOnlyDictionary<Guid, WithholdingTaxCode> withholdingCodesById =
            withholdingCodeIds.Count == 0 || _gibCodeRepository is null
                ? new Dictionary<Guid, WithholdingTaxCode>()
                : await _gibCodeRepository.GetWithholdingByIdsAsync(withholdingCodeIds, cancellationToken);
        if (withholdingCodesById.Count != withholdingCodeIds.Count)
        {
            throw new InvalidInvoiceLineException("Validation.WithholdingCodeNotFound");
        }

        var now = DateTime.UtcNow;
        var period = await _periodRepository.GetByDateAsync(now.Date, cancellationToken);
        period?.EnsurePostingAllowed(now);

        var invoiceNumber = await _sequenceRepository.ConsumeAsync(
            DocumentSequenceType.InvoiceNumber, now, cancellationToken);

        var customerLegalName = customer.LegalName ?? customer.Name;
        var invoice = new Invoice(
            invoiceNumber,
            customer.Id,
            customerLegalName,
            request.Currency,
            InvoiceType.SalesInvoice);

        var paymentTermsId = request.PaymentTermsId ?? customer.PaymentTermsId;
        PaymentTerm? paymentTerm = null;
        int? netDaysSnapshot = null;
        if (paymentTermsId.HasValue)
        {
            paymentTerm = await _paymentTermRepository.GetByIdAsync(paymentTermsId.Value, cancellationToken);
            netDaysSnapshot = paymentTerm?.NetDays;
        }

        var resolvedDueDays = netDaysSnapshot ?? request.DueDays;
        invoice.UpdateDetails(
            issueDate: request.IssueDate,
            dueDate: request.IssueDate.AddDays(resolvedDueDays),
            postingDate: request.IssueDate.Date,
            exchangeRate: request.ExchangeRate ?? 1m,
            paymentTermsId: paymentTermsId,
            paymentTermsNetDaysSnapshot: netDaysSnapshot,
            headerDiscountPercent: request.HeaderDiscountPercent ?? 0m,
            headerDiscountAmount: request.HeaderDiscountAmount ?? 0m,
            shippingCost: request.ShippingCost ?? 0m,
            roundingAdjustment: request.RoundingAdjustment ?? 0m,
            internalNotes: request.InternalNotes,
            publicNotes: request.PublicNotes,
            termsAndConditions: request.TermsAndConditions,
            notes: request.Notes);

        var billingAddress = request.BillingAddressId.HasValue
            ? await _addressRepository.GetByIdAsync(request.BillingAddressId.Value, cancellationToken)
            : null;
        var shippingAddress = request.ShippingAddressId.HasValue
            ? await _addressRepository.GetByIdAsync(request.ShippingAddressId.Value, cancellationToken)
            : null;

        invoice.ApplySnapshots(
            new CustomerSnapshot
            {
                Code = customer.Code,
                LegalName = customerLegalName,
                TradeName = customer.TradeName,
                TaxNumber = customer.TaxNumber,
                TaxOffice = customer.TaxOffice,
                NationalId = customer.NationalId,
                Email = customer.Email,
                Phone = customer.Phone,
            },
            billingAddress is null ? null : ToSnapshot(billingAddress),
            shippingAddress is null ? null : ToSnapshot(shippingAddress));

        var lineNumber = 1;
        var lines = new List<InvoiceLine>();
        foreach (var input in request.Lines)
        {
            InvoiceLine line;
            if (input.ProductId.HasValue)
            {
                if (!productsById.TryGetValue(input.ProductId.Value, out _))
                {
                    throw new InvalidInvoiceLineException("Validation.ProductNotFoundOrCrossTenant");
                }
                line = new InvoiceLine(input.ProductId.Value, input.ProductSku, input.ProductName, input.Quantity, input.UnitPrice);
            }
            else
            {
                line = new InvoiceLine(input.ProductSku, input.ProductName, input.Description, input.Quantity, input.UnitPrice);
            }

            var withholdingCode = input.WithholdingTaxCodeId.HasValue
                ? withholdingCodesById[input.WithholdingTaxCodeId.Value]
                : null;

            line.SetLineNumber(lineNumber++);
            line.ApplyPricing(
                quantity: input.Quantity,
                unitPrice: input.UnitPrice,
                lineDiscountPercent: input.LineDiscountPercent ?? 0m,
                lineDiscountAmount: input.LineDiscountAmount ?? 0m,
                taxRatePercent: input.TaxRatePercent,
                taxRateId: input.TaxRateId,
                isTaxInclusive: input.IsTaxInclusive,
                withholdingRatePercent: input.WithholdingRatePercent ?? 0m,
                uomId: input.UomId,
                uomCode: input.UomCode,
                description: input.Description,
                revenueAccountCode: null,
                costCenter: null,
                project: null,
                originOrderLineId: null,
                withholdingTaxCodeId: withholdingCode?.Id,
                withholdingCode: withholdingCode?.Code,
                withholdingNumerator: withholdingCode?.Numerator,
                withholdingDenominator: withholdingCode?.Denominator);
            lines.Add(line);
        }
        invoice.ReplaceLines(lines);

        if (request.VatExemptionCodeId.HasValue)
        {
            var exemption = _gibCodeRepository is null
                ? null
                : await _gibCodeRepository.GetExemptionByIdAsync(request.VatExemptionCodeId.Value, cancellationToken);
            if (exemption is null)
            {
                throw new InvalidInvoiceLineException("Validation.VatExemptionCodeNotFound");
            }

            invoice.SetVatExemption(exemption.Id, exemption.Code, request.VatExemptionReason);
        }

        if (_fxResolver is not null &&
            !string.Equals(invoice.Currency, BaseCurrency, StringComparison.OrdinalIgnoreCase))
        {
            var tenantId = _tenantContext?.CurrentTenantId;
            var fxLock = await _fxResolver.ResolveDetailedAsync(invoice.Currency, request.IssueDate, tenantId, cancellationToken);
            if (fxLock is not null)
            {
                invoice.ApplyFxRateSnapshot(fxLock.Snapshot.BuyingRate, fxLock.Snapshot.Source, DateTime.UtcNow);
            }
        }

        await _creditGuard.EnsureWithinLimitAsync(customer, invoice.Total, cancellationToken);

        invoice.Issue(invoiceNumber);

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _eInvoiceOutbox.EnqueueSubmissionAsync(
            new EInvoiceSubmissionRequestedPayload(invoice.TenantId, invoice.Id),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        invoice.Customer = customer;

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            await _emailService.SendInvoiceIssuedAsync(
                customer.Email!,
                invoice.InvoiceNumber,
                invoice.CustomerNameSnapshot,
                invoice.Total,
                invoice.Currency,
                cancellationToken);
        }

        return InvoiceMapper.ToDto(invoice);
    }

    private static AddressSnapshot ToSnapshot(CustomerAddress a) => new()
    {
        Label = a.Label,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
    };
}
