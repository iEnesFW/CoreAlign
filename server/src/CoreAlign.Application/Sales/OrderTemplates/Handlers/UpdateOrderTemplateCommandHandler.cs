using CoreAlign.Application.Sales.OrderTemplates.Commands;
using CoreAlign.Application.Sales.OrderTemplates.DTOs;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Sales.OrderTemplates.Handlers;

public class UpdateOrderTemplateCommandHandler : IRequestHandler<UpdateOrderTemplateCommand, OrderTemplateDto>
{
    private readonly IOrderTemplateRepository _repository;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderTemplateCommandHandler(
        IOrderTemplateRepository repository,
        IProductRepository products,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderTemplateDto> Handle(UpdateOrderTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new OrderTemplateNotFoundException();

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }

        template.UpdateHeader(request.Name, request.CustomerId, request.Currency, request.PriceListId, request.Notes);

        var lines = request.Lines.Select(l =>
        {
            var product = products[l.ProductId];
            return new OrderTemplateLine(product.Id, product.Sku, product.Name, l.Quantity, l.UnitPrice, l.Notes);
        });
        template.ReplaceLines(lines);

        template.SetSchedule(request.Frequency, request.NextRunAtUtc, DateTime.UtcNow);
        template.SetActive(request.IsActive);

        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderTemplateMapper.ToDto(template);
    }
}
