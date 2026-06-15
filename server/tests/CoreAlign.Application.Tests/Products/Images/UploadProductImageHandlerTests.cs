using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.Products.Images;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Products.Images;

public class UploadProductImageHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IProductImageRepository _images = Substitute.For<IProductImageRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static readonly byte[] PngHeader =
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00,
    };

    private static MemoryStream NewPngStream() => new(PngHeader, writable: false);

    [Fact]
    public async Task Rejects_disallowed_content_type()
    {
        var sut = new UploadProductImageHandler(_products, _images, _storage, _uow);
        using var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadProductImageCommand(Guid.NewGuid(), "x.gif", "image/gif", 3, content, null, false);

        var act = () => sut.Handle(command, default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Rejects_when_extension_disagrees_with_content_type()
    {
        var sut = new UploadProductImageHandler(_products, _images, _storage, _uow);
        using var content = NewPngStream();
        var command = new UploadProductImageCommand(Guid.NewGuid(), "x.jpg", "image/png", PngHeader.Length, content, null, false);

        var act = () => sut.Handle(command, default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Rejects_when_payload_is_not_a_real_image()
    {
        var sut = new UploadProductImageHandler(_products, _images, _storage, _uow);
        using var content = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("not-an-image-but-named-png"));
        var command = new UploadProductImageCommand(Guid.NewGuid(), "x.png", "image/png", content.Length, content, null, false);

        var act = () => sut.Handle(command, default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Rejects_oversized_image()
    {
        var sut = new UploadProductImageHandler(_products, _images, _storage, _uow);
        using var content = NewPngStream();
        var command = new UploadProductImageCommand(
            Guid.NewGuid(),
            "x.png",
            "image/png",
            ProductImagePolicy.MaxBytesPerImage + 1,
            content,
            null,
            false);

        var act = () => sut.Handle(command, default);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Throws_when_product_not_found()
    {
        _products.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        var sut = new UploadProductImageHandler(_products, _images, _storage, _uow);
        using var content = NewPngStream();
        var command = new UploadProductImageCommand(Guid.NewGuid(), "x.png", "image/png", content.Length, content, null, false);
        var act = () => sut.Handle(command, default);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Throws_when_max_images_reached()
    {
        var productId = Guid.NewGuid();
        var product = new Product("SKU", "Name");
        product.Id = productId;
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var existing = Enumerable.Range(0, ProductImagePolicy.MaxImagesPerProduct)
            .Select(i => new ProductImage(productId, $"k{i}", "image/png", 1, null, i, i == 0))
            .ToList();
        _images.GetByProductAsync(productId, Arg.Any<CancellationToken>()).Returns(existing);

        using var content = NewPngStream();
        var command = new UploadProductImageCommand(productId, "x.png", "image/png", content.Length, content, null, false);
        var sut = new UploadProductImageHandler(_products, _images, _storage, _uow);
        var act = () => sut.Handle(command, default);
        await act.Should().ThrowAsync<ProductImageLimitExceededException>();
    }

    [Fact]
    public async Task First_image_is_marked_primary_even_when_caller_did_not_request_it()
    {
        var productId = Guid.NewGuid();
        var product = new Product("SKU", "Name");
        product.Id = productId;
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
        _images.GetByProductAsync(productId, Arg.Any<CancellationToken>()).Returns(new List<ProductImage>());
        _storage.SaveAsync("product-images", "x.png", Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>())
            .Returns(new StoredFile("tenant/product-images/x.png", "image/png", PngHeader.Length, "https://cdn/x.png"));
        _storage.ResolvePublicUrl("tenant/product-images/x.png").Returns("https://cdn/x.png");

        ProductImage? captured = null;
        await _images.AddAsync(Arg.Do<ProductImage>(i => captured = i), Arg.Any<CancellationToken>());

        using var content = NewPngStream();
        var command = new UploadProductImageCommand(productId, "x.png", "image/png", content.Length, content, "front", false);
        var sut = new UploadProductImageHandler(_products, _images, _storage, _uow);
        var dto = await sut.Handle(command, default);

        captured.Should().NotBeNull();
        captured!.IsPrimary.Should().BeTrue();
        dto.IsPrimary.Should().BeTrue();
        dto.PublicUrl.Should().Be("https://cdn/x.png");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Demotes_previous_primary_when_make_primary_requested()
    {
        var productId = Guid.NewGuid();
        var product = new Product("SKU", "Name");
        product.Id = productId;
        _products.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
        var existingPrimary = new ProductImage(productId, "k0", "image/png", 1, null, 0, true);
        _images.GetByProductAsync(productId, Arg.Any<CancellationToken>()).Returns(new List<ProductImage> { existingPrimary });
        _storage.SaveAsync("product-images", "x.png", Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>())
            .Returns(new StoredFile("tenant/product-images/x.png", "image/png", PngHeader.Length, "https://cdn/x.png"));
        _storage.ResolvePublicUrl(Arg.Any<string>()).Returns("https://cdn/x.png");

        using var content = NewPngStream();
        var command = new UploadProductImageCommand(productId, "x.png", "image/png", content.Length, content, null, true);
        var sut = new UploadProductImageHandler(_products, _images, _storage, _uow);
        var dto = await sut.Handle(command, default);

        existingPrimary.IsPrimary.Should().BeFalse();
        dto.IsPrimary.Should().BeTrue();
    }
}
