using CoreAlign.Application.Products.Images;

namespace CoreAlign.Application.Tests.Products.Images;

public class ProductImagePolicyTests
{
    [Fact]
    public void Exposes_documented_limits()
    {
        ProductImagePolicy.MaxBytesPerImage.Should().Be(5L * 1024 * 1024);
        ProductImagePolicy.MaxImagesPerProduct.Should().Be(10);
        ProductImagePolicy.StorageScope.Should().Be("product-images");
    }
}
