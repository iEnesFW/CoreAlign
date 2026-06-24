namespace CoreAlign.Application.Products.Images;

public static class ProductImagePolicy
{
    public const long MaxBytesPerImage = 5L * 1024L * 1024L;
    public const int MaxImagesPerProduct = 10;
    public const string StorageScope = "product-images";
}
