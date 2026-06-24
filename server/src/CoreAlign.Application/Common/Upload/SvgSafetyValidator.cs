using System.Text.RegularExpressions;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Common.Upload;

public static partial class SvgSafetyValidator
{
    [GeneratedRegex(
        @"<\s*script|<\s*foreignobject|javascript:|<!entity|[\s""';]on[a-z]+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DangerousContent();

    public static bool IsSafe(string? svgContent) =>
        string.IsNullOrEmpty(svgContent) || !DangerousContent().IsMatch(svgContent);

    public static void EnsureSafe(string? svgContent)
    {
        if (!IsSafe(svgContent))
        {
            throw new FileUploadValidationException("SVG contains scripting or active content and was rejected.");
        }
    }
}
