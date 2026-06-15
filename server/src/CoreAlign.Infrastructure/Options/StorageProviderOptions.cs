namespace CoreAlign.Infrastructure.Options;

public static class StorageProviderNames
{
    public const string Local = "Local";
    public const string S3 = "S3";
    public const string AzureBlob = "AzureBlob";
}

public class StorageProviderOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = StorageProviderNames.Local;

    public S3StorageOptions S3 { get; set; } = new();

    public AzureBlobStorageOptions AzureBlob { get; set; } = new();
}

public class S3StorageOptions
{
    public string? Bucket { get; set; }

    public string? Region { get; set; }

    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }

    public string? PublicBaseUrl { get; set; }
}

public class AzureBlobStorageOptions
{
    public string? ConnectionString { get; set; }

    public string? Container { get; set; }

    public bool ContainerPerTenant { get; set; }

    public string? PublicBaseUrl { get; set; }
}
