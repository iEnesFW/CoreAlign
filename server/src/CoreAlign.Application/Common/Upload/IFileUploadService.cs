using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAlign.Application.Common.Upload;

public sealed record FileUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    string ProfileName,
    string Scope);

public sealed record UploadedFile(
    string RelativePath,
    string ContentType,
    long SizeBytes,
    string FileName,
    string PublicUrl);

public sealed record FileValidationRequest(
    Stream Content,
    string FileName,
    string ContentType,
    string ProfileName);

public sealed class ValidatedFile : IDisposable
{
    public ValidatedFile(Stream content, string fileName, string contentType, long sizeBytes, DetectedFileType detectedType)
    {
        Content = content;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        DetectedType = detectedType;
    }

    public Stream Content { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public long SizeBytes { get; }

    public DetectedFileType DetectedType { get; }

    public void Dispose() => Content.Dispose();
}

public interface IFileUploadService
{
    Task<UploadedFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);

    Task<ValidatedFile> ValidateAsync(FileValidationRequest request, CancellationToken cancellationToken = default);
}
