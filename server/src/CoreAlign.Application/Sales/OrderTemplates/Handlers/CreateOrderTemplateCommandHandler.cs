using CoreAlign.Application.B2B;
using CoreAlign.Application.Sales.OrderTemplates.Commands;
using CoreAlign.Application.Sales.OrderTemplates.DTOs;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Sales.OrderTemplates.Handlers;

public class CreateOrderTemplateCommandHandler : IRequestHandler<CreateOrderTemplateCommand, OrderTemplateDto>
{
    private readonly IOrderTemplateRepository _repository;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderTemplateCommandHandler(
        IOrderTemplateRepository repository,
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

    public async Task<OrderTemplateDto> Handle(CreateOrderTemplateCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }

        var userId = _currentUser.UserIdOrThrow();
        var template = new OrderTemplate(
            name: request.Name,
            customerId: request.CustomerId,
            currency: request.Currency,
            createdByUserId: userId,
            priceListId: request.PriceListId,
            notes: request.Notes);

        var lines = request.Lines.Select(l =>
        {
            var product = products[l.ProductId];
            return new OrderTemplateLine(product.Id, product.Sku, product.Name, l.Quantity, l.UnitPrice, l.Notes);
        });
        template.ReplaceLines(lines);
        template.SetSchedule(request.Frequency, request.FirstRunAtUtc, DateTime.UtcNow);

        await _repository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderTemplateMapper.ToDto(template);
    }
}
