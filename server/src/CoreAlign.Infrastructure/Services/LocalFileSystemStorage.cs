using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Services;

public class LocalFileSystemStorage : IFileStorage
{
    private readonly FileStorageOptions _options;
    private readonly ITenantContext _tenantContext;

    public LocalFileSystemStorage(IOptions<FileStorageOptions> options, ITenantContext tenantContext)
    {
        _options = options.Value;
        _tenantContext = tenantContext;
    }

    public async Task<StoredFile> SaveAsync(
        string scope,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Scope is required.", nameof(scope));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (content.Length > _options.MaxBytesPerFile)
        {
            throw new InvalidOperationException(
                $"File size {content.Length} bytes exceeds the maximum allowed {_options.MaxBytesPerFile}.");
        }

        var tenantSegment = (_tenantContext.CurrentTenantId ?? Guid.Empty).ToString("N");
        var safeName = SanitizeFileName(fileName);
        var fileId = Guid.NewGuid();
        var uniqueName = $"{fileId:N}_{safeName}";
        var relativePath = string.Join('/', tenantSegment, scope, uniqueName);
        var absolutePath = Path.Combine(GetRootDirectory(), tenantSegment, scope, uniqueName);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var fileStream = new FileStream(
            absolutePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);

        var publicUrl = ResolvePublicUrl(relativePath);
        return new StoredFile(relativePath, contentType, fileStream.Length, publicUrl);
    }

    public Task<FileMetadata?> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        if (fileId == Guid.Empty || !_tenantContext.HasTenant)
        {
            return Task.FromResult<FileMetadata?>(null);
        }

        var tenantId = _tenantContext.RequireTenantId();
        var tenantRoot = Path.Combine(GetRootDirectory(), tenantId.ToString("N"));
        if (!Directory.Exists(tenantRoot))
        {
            return Task.FromResult<FileMetadata?>(null);
        }

        var pattern = $"{fileId:N}_*";
        var match = Directory
            .EnumerateFiles(tenantRoot, pattern, SearchOption.AllDirectories)
            .FirstOrDefault();
        if (match is null)
        {
            return Task.FromResult<FileMetadata?>(null);
        }

        var info = new FileInfo(match);
        var relative = Path.GetRelativePath(GetRootDirectory(), match).Replace('\\', '/');
        var contentType = ResolveContentType(info.Extension);
        return Task.FromResult<FileMetadata?>(new FileMetadata(fileId, tenantId, relative, contentType, info.Length));
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolute = Path.Combine(GetRootDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolute))
        {
            throw new FileNotFoundException($"File not found: {relativePath}");
        }
        Stream stream = new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolute = Path.Combine(GetRootDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult(File.Exists(absolute));
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolute = Path.Combine(GetRootDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolute))
        {
            File.Delete(absolute);
        }
        return Task.CompletedTask;
    }

    public string ResolvePublicUrl(string relativePath) =>
        $"{_options.PublicBaseUrl.TrimEnd('/')}/{relativePath.Replace('\\', '/')}";

    private string GetRootDirectory()
    {
        return Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(AppContext.BaseDirectory, _options.RootPath);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(fileName.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "file" : clean;
    }

    private static string ResolveContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        ".heif" => "image/heif",
        ".gif" => "image/gif",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream",
    };
}
