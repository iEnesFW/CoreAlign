using CoreAlign.Application.B2B;
using CoreAlign.Application.Invoices.Recurring.Commands;
using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Domain.Entities.Invoices;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Recurring.Handlers;

public class CreateRecurringInvoiceTemplateCommandHandler
    : IRequestHandler<CreateRecurringInvoiceTemplateCommand, RecurringInvoiceTemplateDto>
{
    private readonly IRecurringInvoiceTemplateRepository _repository;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRecurringInvoiceTemplateCommandHandler(
        IRecurringInvoiceTemplateRepository repository,
        ICustomerRepository customers,
        IProductRepository products,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _customers = customers;
        _products = products;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringInvoiceTemplateDto> Handle(
        CreateRecurringInvoiceTemplateCommand request,
        CancellationToken cancellationToken)
    {
        _ = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = RecurringInvoiceLineBuilder.ProductIds(request.Lines);
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidInvoiceLineException("One or more products were not found.");
        }

        var userId = _currentUser.UserIdOrThrow();
        var template = new RecurringInvoiceTemplate(
            name: request.Name,
            customerId: request.CustomerId,
            currency: request.Currency,
            createdByUserId: userId,
            frequency: request.Frequency,
            intervalCount: request.IntervalCount,
            anchorDayOfMonth: request.AnchorDayOfMonth,
            anchorDayOfWeek: request.AnchorDayOfWeek,
            startDate: request.StartDate,
            endDate: request.EndDate,
            maxOccurrences: request.MaxOccurrences,
            dueDays: request.DueDays,
            paymentTermsId: request.PaymentTermsId,
            headerDiscountPercent: request.HeaderDiscountPercent,
            headerDiscountAmount: request.HeaderDiscountAmount,
            shippingCost: request.ShippingCost,
            roundingAdjustment: request.RoundingAdjustment,
            autoConfirm: request.AutoConfirm,
            publicNotes: request.PublicNotes,
            internalNotes: request.InternalNotes);

        template.ReplaceLines(RecurringInvoiceLineBuilder.Build(request.Lines, products));

        await _repository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RecurringInvoiceMapper.ToDto(template);
    }
}
