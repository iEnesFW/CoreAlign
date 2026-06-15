using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.Products.Images;
using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Products.Images;

public class DeleteProductImageHandlerTests
{
    private readonly IProductImageRepository _images = Substitute.For<IProductImageRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Throws_when_image_missing()
    {
        _images.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProductImage?)null);
        var sut = new DeleteProductImageHandler(_images, _storage, _uow);
        var act = () => sut.Handle(new DeleteProductImageCommand(Guid.NewGuid(), Guid.NewGuid()), default);
        await act.Should().ThrowAsync<ProductImageNotFoundException>();
    }

    [Fact]
    public async Task Removes_image_and_deletes_storage_payload()
    {
        var productId = Guid.NewGuid();
        var image = new ProductImage(productId, "tenant/product-images/file.png", "image/png", 1, null, 0, false);
        _images.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);
        _images.GetByProductAsync(productId, Arg.Any<CancellationToken>()).Returns(new List<ProductImage>());

        var sut = new DeleteProductImageHandler(_images, _storage, _uow);
        var ok = await sut.Handle(new DeleteProductImageCommand(productId, image.Id), default);

        ok.Should().BeTrue();
        _images.Received(1).Remove(image);
        await _storage.Received(1).DeleteAsync("tenant/product-images/file.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Promotes_next_image_when_primary_is_deleted()
    {
        var productId = Guid.NewGuid();
        var primary = new ProductImage(productId, "k0", "image/png", 1, null, 0, true);
        var secondary = new ProductImage(productId, "k1", "image/png", 1, null, 1, false);
        _images.GetByIdAsync(primary.Id, Arg.Any<CancellationToken>()).Returns(primary);
        _images.GetByProductAsync(productId, Arg.Any<CancellationToken>()).Returns(new List<ProductImage> { secondary });

        var sut = new DeleteProductImageHandler(_images, _storage, _uow);
        await sut.Handle(new DeleteProductImageCommand(productId, primary.Id), default);

        secondary.IsPrimary.Should().BeTrue();
        _images.Received(1).Update(secondary);
    }
}
