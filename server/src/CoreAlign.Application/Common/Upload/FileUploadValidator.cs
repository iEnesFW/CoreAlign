using System;
using System.IO;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Common.Upload;

public static class FileUploadValidator
{
    public const int HeaderBytes = 512;

    public static string Validate(
        FileUploadProfile profile,
        string fileName,
        string declaredContentType,
        long sizeBytes,
        ReadOnlySpan<byte> header)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (sizeBytes <= 0)
        {
            throw new FileUploadValidationException("File is empty.");
        }
        if (sizeBytes > profile.MaxBytes)
        {
            throw new FileUploadValidationException($"File exceeds the maximum size of {profile.MaxBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (extension.Length == 0 || !profile.AllowedExtensions.Contains(extension))
        {
            throw new FileUploadValidationException("File extension is not allowed for this upload.");
        }

        var contentType = NormalizeContentType(declaredContentType);
        if (!profile.AllowedContentTypes.Contains(contentType))
        {
            throw new FileUploadValidationException("File content type is not allowed for this upload.");
        }

        var detected = FileSignatureInspector.Detect(header, contentType);
        if (detected == DetectedFileType.Unknown)
        {
            throw new FileUploadValidationException("File content could not be verified.");
        }
        if (detected == DetectedFileType.Svg && !profile.AllowSvg)
        {
            throw new FileUploadValidationException("SVG files are not allowed for this upload.");
        }

        var detectedContentType = FileSignatureInspector.ContentTypeFor(detected);
        if (!profile.AllowedContentTypes.Contains(detectedContentType))
        {
            throw new FileUploadValidationException("Actual file content does not match an allowed type.");
        }
        if (!ContentTypesCompatible(contentType, detectedContentType))
        {
            throw new FileUploadValidationException("File content does not match its declared type.");
        }
        if (!FileSignatureInspector.ExtensionMatches(detected, extension))
        {
            throw new FileUploadValidationException("File content does not match its extension.");
        }

        return $"{Guid.NewGuid():N}{extension}";
    }

    public static DetectedFileType ValidateDataFile(
        FileUploadProfile profile,
        string fileName,
        long sizeBytes,
        ReadOnlySpan<byte> header)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (sizeBytes <= 0)
        {
            throw new FileUploadValidationException("File is empty.");
        }
        if (sizeBytes > profile.MaxBytes)
        {
            throw new FileUploadValidationException($"File exceeds the maximum size of {profile.MaxBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (extension.Length == 0 || !profile.AllowedExtensions.Contains(extension))
        {
            throw new FileUploadValidationException("File extension is not allowed for this upload.");
        }

        if (!FileSignatureInspector.MatchesDataExtension(extension, header))
        {
            throw new FileUploadValidationException("File content does not match its extension.");
        }

        return FileSignatureInspector.DataTypeForExtension(extension);
    }

    public static string NormalizeContentType(string? declaredContentType) =>
        (declaredContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();

    private static bool ContentTypesCompatible(string declared, string detected)
    {
        if (string.Equals(declared, detected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (detected == "image/jpeg" && declared == "image/jpg")
        {
            return true;
        }
        if (detected == "image/heif" && declared is "image/heic" or "image/heif")
        {
            return true;
        }
        if (detected == "image/x-icon" && declared is "image/x-icon" or "image/vnd.microsoft.icon")
        {
            return true;
        }
        return false;
    }
}
