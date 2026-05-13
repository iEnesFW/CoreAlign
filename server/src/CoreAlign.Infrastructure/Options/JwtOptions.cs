using System.ComponentModel.DataAnnotations;

namespace CoreAlign.Infrastructure.Options;

public class JwtOptions : IValidatableObject
{
    public const string SectionName = "Jwt";

    private const string CompromisedSecret = "CoreAlign-Super-Secret-Key-That-Is-At-Least-256-Bits-Long-2026!";

    [Required, MinLength(64)]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(SecretKey, CompromisedSecret, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Jwt:SecretKey matches a known-compromised value. Rotate via user-secrets or environment variable.",
                new[] { nameof(SecretKey) });
        }
    }
}
