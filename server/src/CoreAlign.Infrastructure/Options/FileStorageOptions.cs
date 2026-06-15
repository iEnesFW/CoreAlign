using System.ComponentModel.DataAnnotations;

namespace CoreAlign.Infrastructure.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    [Required]
    public string Provider { get; set; } = "LocalFileSystem";

    [Required]
    public string RootPath { get; set; } = "storage";

    [Required]
    public string PublicBaseUrl { get; set; } = "/files";

    public long MaxBytesPerFile { get; set; } = 30_000_000L;
}
