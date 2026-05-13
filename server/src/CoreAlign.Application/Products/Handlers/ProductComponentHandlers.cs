using CoreAlign.Application.Products.Commands;
using CoreAlign.Application.Products.DTOs;
using CoreAlign.Application.Products.Mapping;
using CoreAlign.Application.Products.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Handlers;

public class GetProductComponentsQueryHandler : IRequestHandler<GetProductComponentsQuery, List<ProductComponentDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductComponentRepository _componentRepository;

    public GetProductComponentsQueryHandler(
        IProductRepository productRepository,
        IProductComponentRepository componentRepository)
    {
        _productRepository = productRepository;
        _componentRepository = componentRepository;
    }

    public async Task<List<ProductComponentDto>> Handle(GetProductComponentsQuery request, CancellationToken cancellationToken)
    {
        var parent = await _productRepository.GetByIdAsync(request.ParentProductId, cancellationToken)
            ?? throw new ProductNotFoundException();

        var items = await _componentRepository.GetByParentAsync(parent.Id, cancellationToken);
        return items.Select(ProductComponentMapper.ToDto).ToList();
    }
}

public class AddProductComponentCommandHandler : IRequestHandler<AddProductComponentCommand, ProductComponentDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductComponentRepository _componentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddProductComponentCommandHandler(
        IProductRepository productRepository,
        IProductComponentRepository componentRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _componentRepository = componentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductComponentDto> Handle(AddProductComponentCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentProductId == request.ComponentProductId)
        {
            throw new CircularProductComponentException("self", "self");
        }

        var parent = await _productRepository.GetByIdAsync(request.ParentProductId, cancellationToken)
            ?? throw new ProductNotFoundException();

        var component = await _productRepository.GetByIdAsync(request.ComponentProductId, cancellationToken)
            ?? throw new ProductNotFoundException();

        if (await _componentRepository.ExistsAsync(parent.Id, component.Id, cancellationToken))
        {
            throw new DuplicateProductComponentException();
        }

        if (await _componentRepository.WouldCreateCycleAsync(parent.Id, component.Id, cancellationToken))
        {
            throw new CircularProductComponentException(parent.Sku, component.Sku);
        }

        var entity = new ProductComponent(parent.Id, component.Id, request.Quantity, request.Notes)
        {
            TenantId = parent.TenantId,
            ParentProduct = parent,
            ComponentProduct = component
        };

        await _componentRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductComponentMapper.ToDto(entity);
    }
}

public class UpdateProductComponentCommandHandler : IRequestHandler<UpdateProductComponentCommand, ProductComponentDto>
{
    private readonly IProductComponentRepository _componentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductComponentCommandHandler(IProductComponentRepository componentRepository, IUnitOfWork unitOfWork)
    {
        _componentRepository = componentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductComponentDto> Handle(UpdateProductComponentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _componentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ProductComponentNotFoundException();

        if (entity.ParentProductId != request.ParentProductId)
        {
            throw new ProductComponentNotFoundException();
        }

        entity.Update(request.Quantity, request.Notes);
        _componentRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductComponentMapper.ToDto(entity);
    }
}

public class RemoveProductComponentCommandHandler : IRequestHandler<RemoveProductComponentCommand, bool>
{
    private readonly IProductComponentRepository _componentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveProductComponentCommandHandler(IProductComponentRepository componentRepository, IUnitOfWork unitOfWork)
    {
        _componentRepository = componentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RemoveProductComponentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _componentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ProductComponentNotFoundException();

        if (entity.ParentProductId != request.ParentProductId)
        {
            throw new ProductComponentNotFoundException();
        }

        _componentRepository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
