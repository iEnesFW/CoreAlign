using System;
using System.Text;

namespace CoreAlign.Application.Common.Upload;

public enum DetectedFileType
{
    Unknown,
    Jpeg,
    Png,
    Gif,
    Webp,
    Heif,
    Pdf,
    Svg,
    Ico,
    Zip,
    Ole,
    Csv,
}

public static class FileSignatureInspector
{
    public static DetectedFileType Detect(ReadOnlySpan<byte> header, string declaredContentType)
    {
        if (IsJpeg(header))
        {
            return DetectedFileType.Jpeg;
        }
        if (IsPng(header))
        {
            return DetectedFileType.Png;
        }
        if (IsGif(header))
        {
            return DetectedFileType.Gif;
        }
        if (IsWebp(header))
        {
            return DetectedFileType.Webp;
        }
        if (IsHeif(header))
        {
            return DetectedFileType.Heif;
        }
        if (IsPdf(header))
        {
            return DetectedFileType.Pdf;
        }
        if (IsIco(header))
        {
            return DetectedFileType.Ico;
        }
        if (IsSvg(header, declaredContentType))
        {
            return DetectedFileType.Svg;
        }
        return DetectedFileType.Unknown;
    }

    public static string ContentTypeFor(DetectedFileType type) => type switch
    {
        DetectedFileType.Jpeg => "image/jpeg",
        DetectedFileType.Png => "image/png",
        DetectedFileType.Gif => "image/gif",
        DetectedFileType.Webp => "image/webp",
        DetectedFileType.Heif => "image/heif",
        DetectedFileType.Pdf => "application/pdf",
        DetectedFileType.Svg => "image/svg+xml",
        DetectedFileType.Ico => "image/x-icon",
        DetectedFileType.Zip => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        DetectedFileType.Ole => "application/vnd.ms-excel",
        DetectedFileType.Csv => "text/csv",
        _ => "application/octet-stream",
    };

    public static bool ExtensionMatches(DetectedFileType type, string extension) => type switch
    {
        DetectedFileType.Jpeg => extension is ".jpg" or ".jpeg",
        DetectedFileType.Png => extension is ".png",
        DetectedFileType.Gif => extension is ".gif",
        DetectedFileType.Webp => extension is ".webp",
        DetectedFileType.Heif => extension is ".heic" or ".heif",
        DetectedFileType.Pdf => extension is ".pdf",
        DetectedFileType.Svg => extension is ".svg",
        DetectedFileType.Ico => extension is ".ico",
        _ => false,
    };

    public static bool MatchesDataExtension(string extension, ReadOnlySpan<byte> header) => extension switch
    {
        ".xlsx" or ".xlsm" => IsZip(header),
        ".xls" => IsOle(header),
        ".csv" or ".tsv" or ".txt" => LooksLikeText(header),
        _ => false,
    };

    public static DetectedFileType DataTypeForExtension(string extension) => extension switch
    {
        ".xlsx" or ".xlsm" => DetectedFileType.Zip,
        ".xls" => DetectedFileType.Ole,
        ".csv" or ".tsv" or ".txt" => DetectedFileType.Csv,
        _ => DetectedFileType.Unknown,
    };

    private static bool IsJpeg(ReadOnlySpan<byte> h) =>
        h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> h) =>
        h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47
        && h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A;

    private static bool IsGif(ReadOnlySpan<byte> h) =>
        h.Length >= 6 && h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38
        && (h[4] == 0x37 || h[4] == 0x39) && h[5] == 0x61;

    private static bool IsWebp(ReadOnlySpan<byte> h) =>
        h.Length >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
        && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50;

    private static bool IsHeif(ReadOnlySpan<byte> h)
    {
        if (h.Length < 12 || h[4] != 0x66 || h[5] != 0x74 || h[6] != 0x79 || h[7] != 0x70)
        {
            return false;
        }
        var brand = Encoding.ASCII.GetString(h.Slice(8, 4));
        return brand is "heic" or "heix" or "heim" or "heis" or "mif1" or "heif" or "hevc" or "msf1";
    }

    private static bool IsPdf(ReadOnlySpan<byte> h) =>
        h.Length >= 5 && h[0] == 0x25 && h[1] == 0x50 && h[2] == 0x44 && h[3] == 0x46 && h[4] == 0x2D;

    private static bool IsIco(ReadOnlySpan<byte> h) =>
        h.Length >= 4 && h[0] == 0x00 && h[1] == 0x00 && h[2] == 0x01 && h[3] == 0x00;

    private static bool IsZip(ReadOnlySpan<byte> h) =>
        h.Length >= 4 && h[0] == 0x50 && h[1] == 0x4B
        && ((h[2] == 0x03 && h[3] == 0x04) || (h[2] == 0x05 && h[3] == 0x06) || (h[2] == 0x07 && h[3] == 0x08));

    private static bool IsOle(ReadOnlySpan<byte> h) =>
        h.Length >= 8 && h[0] == 0xD0 && h[1] == 0xCF && h[2] == 0x11 && h[3] == 0xE0
        && h[4] == 0xA1 && h[5] == 0xB1 && h[6] == 0x1A && h[7] == 0xE1;

    private static bool LooksLikeText(ReadOnlySpan<byte> h)
    {
        if (h.IsEmpty)
        {
            return false;
        }
        if (h.Length >= 3 && h[0] == 0xEF && h[1] == 0xBB && h[2] == 0xBF)
        {
            return true;
        }
        if (h.Length >= 2 && ((h[0] == 0xFF && h[1] == 0xFE) || (h[0] == 0xFE && h[1] == 0xFF)))
        {
            return true;
        }
        foreach (var b in h)
        {
            if (b == 0x00)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsSvg(ReadOnlySpan<byte> h, string declaredContentType)
    {
        if (!string.Equals(declaredContentType, "image/svg+xml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var text = Encoding.UTF8.GetString(h).TrimStart('﻿', ' ', '\t', '\r', '\n');
        return text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
    }
}
