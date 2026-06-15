using System.ComponentModel.DataAnnotations;

namespace CoreAlign.Infrastructure.Options;

public class EInvoiceOptions
{
    public const string SectionName = "EInvoice";

    [Required]
    public string Provider { get; set; } = "Stub";

    public string? BaseUrl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
