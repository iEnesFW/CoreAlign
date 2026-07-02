using CoreAlign.Application.Invoices.Recurring.Commands;
using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Recurring.Handlers;

public class UpdateRecurringInvoiceTemplateCommandHandler
    : IRequestHandler<UpdateRecurringInvoiceTemplateCommand, RecurringInvoiceTemplateDto>
{
    private readonly IRecurringInvoiceTemplateRepository _repository;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRecurringInvoiceTemplateCommandHandler(
        IRecurringInvoiceTemplateRepository repository,
        ICustomerRepository customers,
        IProductRepository products,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _customers = customers;
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringInvoiceTemplateDto> Handle(
        UpdateRecurringInvoiceTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new RecurringInvoiceTemplateNotFoundException();

        _ = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = RecurringInvoiceLineBuilder.ProductIds(request.Lines);
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidInvoiceLineException("One or more products were not found.");
        }

        template.UpdateDetails(
            name: request.Name,
            customerId: request.CustomerId,
            currency: request.Currency,
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

        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RecurringInvoiceMapper.ToDto(template);
    }
}
