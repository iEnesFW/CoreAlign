using CoreAlign.Application.Products.Images;

namespace CoreAlign.Application.Tests.Products.Images;

public class ProductImagePolicyTests
{
    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/jpg", true)]
    [InlineData("image/png", true)]
    [InlineData("image/webp", true)]
    [InlineData("image/gif", false)]
    [InlineData("application/pdf", false)]
    [InlineData("text/plain", false)]
    [InlineData("", false)]
    public void Recognises_allowed_content_types(string contentType, bool allowed)
    {
        ProductImagePolicy.IsAllowedContentType(contentType).Should().Be(allowed);
    }

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.JPEG", true)]
    [InlineData("photo.png", true)]
    [InlineData("photo.webp", true)]
    [InlineData("photo.gif", false)]
    [InlineData("photo", false)]
    public void Recognises_allowed_extensions(string fileName, bool allowed)
    {
        ProductImagePolicy.IsAllowedExtension(fileName).Should().Be(allowed);
    }

    [Theory]
    [InlineData("image/png", "photo.png", true)]
    [InlineData("image/jpeg", "photo.jpg", true)]
    [InlineData("image/jpeg", "photo.jpeg", true)]
    [InlineData("image/webp", "photo.webp", true)]
    [InlineData("image/png", "photo.jpg", false)]
    [InlineData("image/jpeg", "photo.png", false)]
    [InlineData("image/png", "photo.exe", false)]
    [InlineData("image/png", "", false)]
    public void Matches_extension_to_content_type(string contentType, string fileName, bool expected)
    {
        ProductImagePolicy.MatchesContentTypeAndExtension(contentType, fileName).Should().Be(expected);
    }

    [Fact]
    public async Task Looks_like_image_accepts_png_magic_bytes()
    {
        var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00, 0xFF };
        using var stream = new MemoryStream(pngHeader);
        (await ProductImagePolicy.LooksLikeImageAsync(stream)).Should().BeTrue();
    }

    [Fact]
    public async Task Looks_like_image_accepts_jpeg_magic_bytes()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        using var stream = new MemoryStream(jpeg);
        (await ProductImagePolicy.LooksLikeImageAsync(stream)).Should().BeTrue();
    }

    [Fact]
    public async Task Looks_like_image_accepts_webp_magic_bytes()
    {
        var webp = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x10, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
        using var stream = new MemoryStream(webp);
        (await ProductImagePolicy.LooksLikeImageAsync(stream)).Should().BeTrue();
    }

    [Fact]
    public async Task Looks_like_image_rejects_plain_text_payload()
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes("<?php evil(); ?>");
        using var stream = new MemoryStream(bytes);
        (await ProductImagePolicy.LooksLikeImageAsync(stream)).Should().BeFalse();
    }

    [Fact]
    public async Task Looks_like_image_rejects_pdf_disguised_as_png()
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7");
        using var stream = new MemoryStream(bytes);
        (await ProductImagePolicy.LooksLikeImageAsync(stream)).Should().BeFalse();
    }

    [Fact]
    public void Exposes_documented_limits()
    {
        ProductImagePolicy.MaxBytesPerImage.Should().Be(5L * 1024 * 1024);
        ProductImagePolicy.MaxImagesPerProduct.Should().Be(10);
        ProductImagePolicy.StorageScope.Should().Be("product-images");
    }
}
