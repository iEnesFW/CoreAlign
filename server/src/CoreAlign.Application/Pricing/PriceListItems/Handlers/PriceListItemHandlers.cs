using CoreAlign.Application.Pricing.Common;
using CoreAlign.Application.Pricing.PriceListItems.Commands;
using CoreAlign.Application.Pricing.PriceListItems.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Pricing.PriceListItems.Handlers;

public class ListPriceListItemsHandler : IRequestHandler<ListPriceListItemsQuery, IReadOnlyList<PriceListItemDto>>
{
    private readonly IPriceListRepository _priceLists;
    public ListPriceListItemsHandler(IPriceListRepository priceLists) => _priceLists = priceLists;

    public async Task<IReadOnlyList<PriceListItemDto>> Handle(ListPriceListItemsQuery q, CancellationToken ct)
    {
        var list = await _priceLists.GetWithItemsAsync(q.PriceListId, ct)
            ?? throw new PriceListNotFoundException(q.PriceListId);
        return list.Items
            .OrderBy(i => i.ProductId)
            .ThenBy(i => i.MinQuantity ?? 0m)
            .Select(PricingMappers.ToDto)
            .ToList();
    }
}

public class GetPriceListItemByIdHandler : IRequestHandler<GetPriceListItemByIdQuery, PriceListItemDto?>
{
    private readonly IPriceListRepository _priceLists;
    public GetPriceListItemByIdHandler(IPriceListRepository priceLists) => _priceLists = priceLists;

    public async Task<PriceListItemDto?> Handle(GetPriceListItemByIdQuery q, CancellationToken ct)
    {
        var list = await _priceLists.GetWithItemsAsync(q.PriceListId, ct)
            ?? throw new PriceListNotFoundException(q.PriceListId);
        var item = list.Items.FirstOrDefault(i => i.Id == q.Id);
        return item is null ? null : PricingMappers.ToDto(item);
    }
}

public class AddPriceListItemHandler : IRequestHandler<AddPriceListItemCommand, PriceListItemDto>
{
    private readonly IPriceListRepository _priceLists;
    private readonly IUnitOfWork _uow;

    public AddPriceListItemHandler(IPriceListRepository priceLists, IUnitOfWork uow)
    {
        _priceLists = priceLists;
        _uow = uow;
    }

    public async Task<PriceListItemDto> Handle(AddPriceListItemCommand c, CancellationToken ct)
    {
        var list = await _priceLists.GetWithItemsAsync(c.PriceListId, ct)
            ?? throw new PriceListNotFoundException(c.PriceListId);

        if (HasOverlappingTier(list.Items, c.ProductId, c.MinQuantity, c.MaxQuantity, excludeItemId: null))
        {
            throw new PriceListItemConflictException(c.PriceListId, c.ProductId);
        }

        var item = new PriceListItem(c.PriceListId, c.ProductId, c.Price, c.MinQuantity, c.MaxQuantity, c.DiscountPercent);
        await _priceLists.AddItemAsync(item, ct);
        await _uow.SaveChangesAsync(ct);
        return PricingMappers.ToDto(item);
    }

    internal static bool HasOverlappingTier(
        IEnumerable<PriceListItem> existing,
        Guid productId,
        decimal? minQuantity,
        decimal? maxQuantity,
        Guid? excludeItemId)
    {
        var min = minQuantity ?? decimal.MinValue;
        var max = maxQuantity ?? decimal.MaxValue;
        foreach (var item in existing.Where(i => i.ProductId == productId && i.Id != excludeItemId))
        {
            var existingMin = item.MinQuantity ?? decimal.MinValue;
            var existingMax = item.MaxQuantity ?? decimal.MaxValue;
            if (min <= existingMax && max >= existingMin)
            {
                return true;
            }
        }
        return false;
    }
}

public class UpdatePriceListItemHandler : IRequestHandler<UpdatePriceListItemCommand, PriceListItemDto>
{
    private readonly IPriceListRepository _priceLists;
    private readonly IUnitOfWork _uow;

    public UpdatePriceListItemHandler(IPriceListRepository priceLists, IUnitOfWork uow)
    {
        _priceLists = priceLists;
        _uow = uow;
    }

    public async Task<PriceListItemDto> Handle(UpdatePriceListItemCommand c, CancellationToken ct)
    {
        var list = await _priceLists.GetWithItemsAsync(c.PriceListId, ct)
            ?? throw new PriceListNotFoundException(c.PriceListId);
        var item = list.Items.FirstOrDefault(i => i.Id == c.Id)
            ?? throw new PriceListItemNotFoundException(c.Id);

        if (AddPriceListItemHandler.HasOverlappingTier(list.Items, item.ProductId, c.MinQuantity, c.MaxQuantity, excludeItemId: c.Id))
        {
            throw new PriceListItemConflictException(c.PriceListId, item.ProductId);
        }

        item.Update(c.Price, c.MinQuantity, c.MaxQuantity, c.DiscountPercent);
        _priceLists.UpdateItem(item);
        await _uow.SaveChangesAsync(ct);
        return PricingMappers.ToDto(item);
    }
}

public class RemovePriceListItemHandler : IRequestHandler<RemovePriceListItemCommand, bool>
{
    private readonly IPriceListRepository _priceLists;
    private readonly IUnitOfWork _uow;

    public RemovePriceListItemHandler(IPriceListRepository priceLists, IUnitOfWork uow)
    {
        _priceLists = priceLists;
        _uow = uow;
    }

    public async Task<bool> Handle(RemovePriceListItemCommand c, CancellationToken ct)
    {
        var list = await _priceLists.GetWithItemsAsync(c.PriceListId, ct)
            ?? throw new PriceListNotFoundException(c.PriceListId);
        var item = list.Items.FirstOrDefault(i => i.Id == c.Id);
        if (item is null) return false;
        _priceLists.RemoveItem(item);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
