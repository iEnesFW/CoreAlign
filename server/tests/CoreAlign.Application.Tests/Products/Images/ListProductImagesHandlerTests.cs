using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.Products.Images;
using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Products.Images;

public class ListProductImagesHandlerTests
{
    private readonly IProductImageRepository _images = Substitute.For<IProductImageRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();

    [Fact]
    public async Task Returns_resolved_public_urls()
    {
        var productId = Guid.NewGuid();
        var rows = new List<ProductImage>
        {
            new(productId, "tenant/product-images/a.png", "image/png", 100, "alt-a", 0, true),
            new(productId, "tenant/product-images/b.png", "image/png", 200, null, 1, false),
        };
        _images.GetByProductAsync(productId, Arg.Any<CancellationToken>()).Returns(rows);
        _storage.ResolvePublicUrl("tenant/product-images/a.png").Returns("https://cdn/a");
        _storage.ResolvePublicUrl("tenant/product-images/b.png").Returns("https://cdn/b");

        var sut = new ListProductImagesHandler(_images, _storage);
        var result = await sut.Handle(new ListProductImagesQuery(productId), default);

        result.Should().HaveCount(2);
        result[0].PublicUrl.Should().Be("https://cdn/a");
        result[0].IsPrimary.Should().BeTrue();
        result[1].PublicUrl.Should().Be("https://cdn/b");
    }

    [Fact]
    public async Task Returns_empty_when_no_images()
    {
        _images.GetByProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<ProductImage>());
        var sut = new ListProductImagesHandler(_images, _storage);
        var result = await sut.Handle(new ListProductImagesQuery(Guid.NewGuid()), default);
        result.Should().BeEmpty();
    }
}
