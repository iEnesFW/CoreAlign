using System.ComponentModel.DataAnnotations;

namespace CoreAlign.API.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    [Required, MinLength(1)]
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
