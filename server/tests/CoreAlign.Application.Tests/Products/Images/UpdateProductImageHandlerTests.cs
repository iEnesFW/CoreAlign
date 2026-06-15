using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.Products.Images;
using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Products.Images;

public class UpdateProductImageHandlerTests
{
    private readonly IProductImageRepository _images = Substitute.For<IProductImageRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Throws_when_image_not_found()
    {
        _images.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProductImage?)null);
        var sut = new UpdateProductImageHandler(_images, _storage, _uow);
        var act = () => sut.Handle(new UpdateProductImageCommand(Guid.NewGuid(), Guid.NewGuid(), null, 0, false), default);
        await act.Should().ThrowAsync<ProductImageNotFoundException>();
    }

    [Fact]
    public async Task Throws_when_image_belongs_to_other_product()
    {
        var image = new ProductImage(Guid.NewGuid(), "k", "image/png", 1, null, 0, false);
        _images.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);

        var sut = new UpdateProductImageHandler(_images, _storage, _uow);
        var act = () => sut.Handle(new UpdateProductImageCommand(Guid.NewGuid(), image.Id, null, 0, false), default);
        await act.Should().ThrowAsync<ProductImageNotFoundException>();
    }

    [Fact]
    public async Task Promoting_to_primary_demotes_existing_primary_siblings()
    {
        var productId = Guid.NewGuid();
        var promoted = new ProductImage(productId, "k1", "image/png", 1, null, 1, false);
        var existingPrimary = new ProductImage(productId, "k0", "image/png", 1, null, 0, true);
        _images.GetByIdAsync(promoted.Id, Arg.Any<CancellationToken>()).Returns(promoted);
        _images.GetByProductAsync(productId, Arg.Any<CancellationToken>()).Returns(new List<ProductImage> { promoted, existingPrimary });
        _storage.ResolvePublicUrl(Arg.Any<string>()).Returns("https://cdn/x");

        var sut = new UpdateProductImageHandler(_images, _storage, _uow);
        var dto = await sut.Handle(new UpdateProductImageCommand(productId, promoted.Id, "new", 0, true), default);

        existingPrimary.IsPrimary.Should().BeFalse();
        promoted.IsPrimary.Should().BeTrue();
        promoted.AltText.Should().Be("new");
        dto.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Reorder_persists_new_display_order()
    {
        var productId = Guid.NewGuid();
        var image = new ProductImage(productId, "k", "image/png", 1, "alt", 2, false);
        _images.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);
        _storage.ResolvePublicUrl(Arg.Any<string>()).Returns("https://cdn/x");

        var sut = new UpdateProductImageHandler(_images, _storage, _uow);
        var dto = await sut.Handle(new UpdateProductImageCommand(productId, image.Id, "alt", 0, false), default);

        image.DisplayOrder.Should().Be(0);
        dto.DisplayOrder.Should().Be(0);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
