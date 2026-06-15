using CoreAlign.Application.Common.Storage;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Storage;

public sealed class VirusScanFileStorage : IFileStorage
{
    private readonly IFileStorage _inner;
    private readonly IVirusScanner _scanner;
    private readonly ILogger<VirusScanFileStorage> _logger;

    public VirusScanFileStorage(IFileStorage inner, IVirusScanner scanner, ILogger<VirusScanFileStorage> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _logger = logger;
    }

    public async Task<StoredFile> SaveAsync(
        string scope,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));

        var scanBuffer = await BufferIfNotSeekableAsync(content, cancellationToken);
        try
        {
            var scanned = await _scanner.ScanAsync(scanBuffer, cancellationToken);
            if (!scanned.IsClean)
            {
                _logger.LogWarning(
                    "Virus scan rejected upload {FileName} in scope {Scope}: {Threat} ({Provider})",
                    fileName,
                    scope,
                    scanned.ThreatName,
                    scanned.Provider);
                throw new VirusScanRejectedException(scanned.ThreatName ?? "unknown", scanned.Provider);
            }

            if (scanBuffer.CanSeek)
            {
                scanBuffer.Position = 0;
            }

            return await _inner.SaveAsync(scope, fileName, scanBuffer, contentType, cancellationToken);
        }
        finally
        {
            if (!ReferenceEquals(scanBuffer, content))
            {
                await scanBuffer.DisposeAsync();
            }
        }
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        => _inner.OpenReadAsync(relativePath, cancellationToken);

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
        => _inner.ExistsAsync(relativePath, cancellationToken);

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        => _inner.DeleteAsync(relativePath, cancellationToken);

    public Task<FileMetadata?> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
        => _inner.GetMetadataAsync(fileId, cancellationToken);

    public string ResolvePublicUrl(string relativePath) => _inner.ResolvePublicUrl(relativePath);

    private static async Task<Stream> BufferIfNotSeekableAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
            return content;
        }

        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }
}
