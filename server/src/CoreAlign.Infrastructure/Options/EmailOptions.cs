using System.ComponentModel.DataAnnotations;

namespace CoreAlign.Infrastructure.Options;

public class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    [Required]
    public string Provider { get; set; } = "LogOnly";

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    public string? AppBaseUrl { get; set; }

    public EmailSmtpOptions Smtp { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Smtp.Host))
            {
                yield return new ValidationResult(
                    "Email:Smtp:Host is required when Email:Provider is 'Smtp'.",
                    new[] { nameof(Smtp.Host) });
            }
            if (Smtp.Port <= 0)
            {
                yield return new ValidationResult(
                    "Email:Smtp:Port must be a positive integer when Email:Provider is 'Smtp'.",
                    new[] { nameof(Smtp.Port) });
            }
            if (string.IsNullOrWhiteSpace(Smtp.FromAddress))
            {
                yield return new ValidationResult(
                    "Email:Smtp:FromAddress is required when Email:Provider is 'Smtp'.",
                    new[] { nameof(Smtp.FromAddress) });
            }
        }
    }
}

public class EmailSmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = true;
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
}
