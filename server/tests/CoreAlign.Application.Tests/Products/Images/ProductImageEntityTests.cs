using CoreAlign.Domain.Entities.Catalog;

namespace CoreAlign.Application.Tests.Products.Images;

public class ProductImageEntityTests
{
    [Fact]
    public void Constructor_sets_metadata()
    {
        var productId = Guid.NewGuid();
        var image = new ProductImage(productId, "tenant/scope/file.png", "image/png", 12_345, "Front view", 2, true);

        image.ProductId.Should().Be(productId);
        image.StorageKey.Should().Be("tenant/scope/file.png");
        image.ContentType.Should().Be("image/png");
        image.SizeBytes.Should().Be(12_345);
        image.AltText.Should().Be("Front view");
        image.DisplayOrder.Should().Be(2);
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Constructor_rejects_empty_product_id()
    {
        var act = () => new ProductImage(Guid.Empty, "k", "image/png", 1, null, 0, false);
        act.Should().Throw<ArgumentException>().WithMessage("*ProductId*");
    }

    [Fact]
    public void Constructor_rejects_blank_storage_key()
    {
        var act = () => new ProductImage(Guid.NewGuid(), " ", "image/png", 1, null, 0, false);
        act.Should().Throw<ArgumentException>().WithMessage("*StorageKey*");
    }

    [Fact]
    public void UpdateMetadata_normalises_blank_alt_to_null()
    {
        var image = new ProductImage(Guid.NewGuid(), "k", "image/png", 1, "alt", 0, false);
        image.UpdateMetadata("   ", 5, true);
        image.AltText.Should().BeNull();
        image.DisplayOrder.Should().Be(5);
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Reorder_clamps_negative_display_order_to_zero()
    {
        var image = new ProductImage(Guid.NewGuid(), "k", "image/png", 1, null, 3, false);
        image.Reorder(-7);
        image.DisplayOrder.Should().Be(0);
    }

    [Fact]
    public void MarkPrimary_toggles_value()
    {
        var image = new ProductImage(Guid.NewGuid(), "k", "image/png", 1, null, 0, false);
        image.IsPrimary.Should().BeFalse();
        image.MarkPrimary(true);
        image.IsPrimary.Should().BeTrue();
        image.MarkPrimary(false);
        image.IsPrimary.Should().BeFalse();
    }
}
