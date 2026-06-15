using CoreAlign.Application.Pricing.PriceListItems.Commands;
using CoreAlign.Application.Pricing.PriceListItems.Handlers;
using CoreAlign.Application.Pricing.PriceListItems.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Pricing;

public class PriceListItemHandlerTests
{
    private readonly IPriceListRepository _priceLists = Substitute.For<IPriceListRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static PriceList NewPriceList(Guid id)
    {
        var list = new PriceList("PL", "Default", "TRY");
        typeof(PriceList).GetProperty(nameof(PriceList.Id))!.SetValue(list, id);
        return list;
    }

    [Fact]
    public async Task Add_creates_item_and_persists()
    {
        var listId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var list = NewPriceList(listId);
        _priceLists.GetWithItemsAsync(listId, Arg.Any<CancellationToken>()).Returns(list);

        var sut = new AddPriceListItemHandler(_priceLists, _uow);
        var dto = await sut.Handle(new AddPriceListItemCommand(listId, productId, 100m, 1m, 10m, 5m), default);

        dto.PriceListId.Should().Be(listId);
        dto.Price.Should().Be(100m);
        dto.DiscountPercent.Should().Be(5m);
        await _priceLists.Received(1).AddItemAsync(Arg.Any<PriceListItem>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_throws_when_pricelist_missing()
    {
        _priceLists.GetWithItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PriceList?)null);
        var sut = new AddPriceListItemHandler(_priceLists, _uow);

        var act = () => sut.Handle(new AddPriceListItemCommand(Guid.NewGuid(), Guid.NewGuid(), 10m), default);
        await act.Should().ThrowAsync<PriceListNotFoundException>();
    }

    [Fact]
    public async Task Add_throws_on_overlapping_tier_for_same_product()
    {
        var listId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var list = NewPriceList(listId);
        var existing = new PriceListItem(listId, productId, 100m, 1m, 10m, null);
        list.Items.Add(existing);
        _priceLists.GetWithItemsAsync(listId, Arg.Any<CancellationToken>()).Returns(list);

        var sut = new AddPriceListItemHandler(_priceLists, _uow);
        var act = () => sut.Handle(new AddPriceListItemCommand(listId, productId, 80m, 5m, 15m, null), default);
        await act.Should().ThrowAsync<PriceListItemConflictException>();
    }

    [Fact]
    public async Task Add_allows_adjacent_non_overlapping_tier()
    {
        var listId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var list = NewPriceList(listId);
        list.Items.Add(new PriceListItem(listId, productId, 100m, 1m, 10m, null));
        _priceLists.GetWithItemsAsync(listId, Arg.Any<CancellationToken>()).Returns(list);

        var sut = new AddPriceListItemHandler(_priceLists, _uow);
        var dto = await sut.Handle(new AddPriceListItemCommand(listId, productId, 80m, 11m, 50m, null), default);
        dto.MinQuantity.Should().Be(11m);
    }

    [Fact]
    public async Task Update_changes_existing_item()
    {
        var listId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var item = new PriceListItem(listId, productId, 50m);
        var list = NewPriceList(listId);
        list.Items.Add(item);
        _priceLists.GetWithItemsAsync(listId, Arg.Any<CancellationToken>()).Returns(list);

        var sut = new UpdatePriceListItemHandler(_priceLists, _uow);
        var dto = await sut.Handle(new UpdatePriceListItemCommand(listId, item.Id, 75m, 1m, 100m, 10m), default);

        dto.Price.Should().Be(75m);
        dto.DiscountPercent.Should().Be(10m);
        _priceLists.Received(1).UpdateItem(Arg.Any<PriceListItem>());
    }

    [Fact]
    public async Task Update_throws_when_item_not_in_list()
    {
        var listId = Guid.NewGuid();
        var list = NewPriceList(listId);
        _priceLists.GetWithItemsAsync(listId, Arg.Any<CancellationToken>()).Returns(list);

        var sut = new UpdatePriceListItemHandler(_priceLists, _uow);
        var act = () => sut.Handle(new UpdatePriceListItemCommand(listId, Guid.NewGuid(), 10m, null, null, null), default);
        await act.Should().ThrowAsync<PriceListItemNotFoundException>();
    }

    [Fact]
    public async Task Remove_returns_true_when_item_exists()
    {
        var listId = Guid.NewGuid();
        var item = new PriceListItem(listId, Guid.NewGuid(), 25m);
        var list = NewPriceList(listId);
        list.Items.Add(item);
        _priceLists.GetWithItemsAsync(listId, Arg.Any<CancellationToken>()).Returns(list);

        var sut = new RemovePriceListItemHandler(_priceLists, _uow);
        var result = await sut.Handle(new RemovePriceListItemCommand(listId, item.Id), default);

        result.Should().BeTrue();
        _priceLists.Received(1).RemoveItem(item);
    }

    [Fact]
    public async Task List_orders_by_product_then_min_quantity()
    {
        var listId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var list = NewPriceList(listId);
        list.Items.Add(new PriceListItem(listId, productA, 80m, 10m, 100m));
        list.Items.Add(new PriceListItem(listId, productA, 100m, 1m, 9m));
        _priceLists.GetWithItemsAsync(listId, Arg.Any<CancellationToken>()).Returns(list);

        var sut = new ListPriceListItemsHandler(_priceLists);
        var result = await sut.Handle(new ListPriceListItemsQuery(listId), default);

        result.Should().HaveCount(2);
        result[0].MinQuantity.Should().Be(1m);
        result[1].MinQuantity.Should().Be(10m);
    }

    [Fact]
    public void Quantity_tier_resolution_picks_correct_row()
    {
        var listId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var tiers = new[]
        {
            new PriceListItem(listId, productId, 100m, 1m, 9m),
            new PriceListItem(listId, productId, 90m, 10m, 49m),
            new PriceListItem(listId, productId, 80m, 50m, null),
        };

        var t1 = tiers.First(t => t.MatchesQuantity(5m));
        var t2 = tiers.First(t => t.MatchesQuantity(25m));
        var t3 = tiers.First(t => t.MatchesQuantity(500m));

        t1.Price.Should().Be(100m);
        t2.Price.Should().Be(90m);
        t3.Price.Should().Be(80m);
    }
}
