using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.Common.Upload;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Infrastructure.Storage;

public sealed class FileUploadService : IFileUploadService
{
    private static readonly char[] InvalidScopeChars = { '\\', ':', '\0', '<', '>', '|', '?', '*' };

    private readonly IFileStorage _storage;

    public FileUploadService(IFileStorage storage)
    {
        _storage = storage;
    }

    public async Task<UploadedFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profile = FileUploadProfiles.Resolve(request.ProfileName);
        var scope = SanitizeScope(request.Scope);
        var contentType = FileUploadValidator.NormalizeContentType(request.ContentType);

        // Seekable sources (e.g. a buffered IFormFile spooled to a temp file) stream straight
        // through to storage so large uploads never materialize fully in managed memory.
        if (request.Content.CanSeek)
        {
            if (request.Content.Length > profile.MaxBytes)
            {
                throw new FileUploadValidationException($"File exceeds the maximum size of {profile.MaxBytes / (1024 * 1024)} MB.");
            }

            var header = await ReadHeaderFromSeekableAsync(request.Content, cancellationToken).ConfigureAwait(false);
            var storedName = FileUploadValidator.Validate(profile, request.FileName, request.ContentType, request.Content.Length, header);
            await EnsureSvgSafeAsync(profile, header, contentType, request.Content, cancellationToken).ConfigureAwait(false);
            request.Content.Position = 0;
            return await StoreAsync(scope, storedName, request.Content, contentType, cancellationToken).ConfigureAwait(false);
        }

        using var buffer = await BufferAsync(request.Content, profile.MaxBytes, cancellationToken).ConfigureAwait(false);
        var bufferedHeader = ReadHeader(buffer);
        var bufferedName = FileUploadValidator.Validate(profile, request.FileName, request.ContentType, buffer.Length, bufferedHeader);
        await EnsureSvgSafeAsync(profile, bufferedHeader, contentType, buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        return await StoreAsync(scope, bufferedName, buffer, contentType, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ValidatedFile> ValidateAsync(FileValidationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profile = FileUploadProfiles.Resolve(request.ProfileName);

        var buffer = await BufferAsync(request.Content, profile.MaxBytes, cancellationToken).ConfigureAwait(false);
        try
        {
            var header = ReadHeader(buffer);
            var contentType = FileUploadValidator.NormalizeContentType(request.ContentType);

            DetectedFileType detected;
            if (profile.ContentKind == FileUploadContentKind.Data)
            {
                detected = FileUploadValidator.ValidateDataFile(profile, request.FileName, buffer.Length, header);
            }
            else
            {
                FileUploadValidator.Validate(profile, request.FileName, request.ContentType, buffer.Length, header);
                detected = FileSignatureInspector.Detect(header, contentType);
            }

            buffer.Position = 0;
            return new ValidatedFile(buffer, request.FileName, contentType, buffer.Length, detected);
        }
        catch
        {
            await buffer.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<UploadedFile> StoreAsync(string scope, string storedName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        StoredFile stored;
        try
        {
            stored = await _storage.SaveAsync(scope, storedName, content, contentType, cancellationToken).ConfigureAwait(false);
        }
        catch (VirusScanRejectedException)
        {
            throw new FileUploadValidationException("File failed a security scan and was rejected.");
        }

        return new UploadedFile(stored.RelativePath, stored.ContentType, stored.SizeBytes, storedName, stored.PublicUrl);
    }

    private static async Task EnsureSvgSafeAsync(
        FileUploadProfile profile,
        byte[] header,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (!profile.AllowSvg || FileSignatureInspector.Detect(header, contentType) != DetectedFileType.Svg)
        {
            return;
        }

        content.Position = 0;
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var svg = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        content.Position = 0;
        SvgSafetyValidator.EnsureSafe(svg);
    }

    private static async Task<MemoryStream> BufferAsync(Stream content, long maxBytes, CancellationToken cancellationToken)
    {
        var initialCapacity = content.CanSeek ? (int)Math.Min(content.Length, maxBytes) : 0;
        var buffer = new MemoryStream(initialCapacity);
        try
        {
            var chunk = new byte[81920];
            long total = 0;
            int read;
            while ((read = await content.ReadAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new FileUploadValidationException($"File exceeds the maximum size of {maxBytes / (1024 * 1024)} MB.");
                }
                await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
            buffer.Position = 0;
            return buffer;
        }
        catch
        {
            await buffer.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<byte[]> ReadHeaderFromSeekableAsync(Stream content, CancellationToken cancellationToken)
    {
        content.Position = 0;
        var headerLength = (int)Math.Min(FileUploadValidator.HeaderBytes, content.Length);
        var header = new byte[headerLength];
        var read = 0;
        while (read < headerLength)
        {
            var n = await content.ReadAsync(header.AsMemory(read, headerLength - read), cancellationToken).ConfigureAwait(false);
            if (n == 0)
            {
                break;
            }
            read += n;
        }
        content.Position = 0;
        return read == headerLength ? header : header[..read];
    }

    private static byte[] ReadHeader(MemoryStream buffer)
    {
        var headerLength = (int)Math.Min(FileUploadValidator.HeaderBytes, buffer.Length);
        var header = new byte[headerLength];
        var headerRead = 0;
        buffer.Position = 0;
        while (headerRead < headerLength)
        {
            var n = buffer.Read(header, headerRead, headerLength - headerRead);
            if (n == 0)
            {
                break;
            }
            headerRead += n;
        }
        buffer.Position = 0;
        return header;
    }

    private static string SanitizeScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new FileUploadValidationException("Upload scope is required.");
        }
        foreach (var segment in scope.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment is "." or ".." || segment.IndexOfAny(InvalidScopeChars) >= 0)
            {
                throw new FileUploadValidationException("Invalid upload scope.");
            }
        }
        return scope.Trim('/');
    }
}
