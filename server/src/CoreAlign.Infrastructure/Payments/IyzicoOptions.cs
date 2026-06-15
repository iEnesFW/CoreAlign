using System.ComponentModel.DataAnnotations;

namespace CoreAlign.Infrastructure.Payments;

/// <summary>
/// Configuration block for the Iyzico provider (<c>Billing:Iyzico</c>). Keys
/// must be supplied via secret store / environment in production; leave empty
/// in dev to skip Iyzico registration entirely (the registry will still expose
/// the mock gateway).
/// </summary>
public sealed class IyzicoOptions
{
    public const string SectionName = "Billing:Iyzico";

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";

    public string CallbackBaseUrl { get; set; } = string.Empty;

    public string WebhookBaseUrl { get; set; } = string.Empty;

    public string DefaultLocale { get; set; } = "tr";

    public bool AllowInstallments { get; set; }

    [Range(1, 600)]
    public int HttpTimeoutSeconds { get; set; } = 30;

    public override string ToString() =>
        $"IyzicoOptions(BaseUrl={BaseUrl}, CallbackBaseUrl={CallbackBaseUrl}, Locale={DefaultLocale}, Installments={AllowInstallments})";
}

/// <summary>
/// Maps our <see cref="IyzicoOptions"/> onto the SDK's
/// <see cref="Iyzipay.Options"/>. Kept as a tiny extension so we don't drag
/// SDK types into our option-binding surface.
/// </summary>
public static class IyzicoOptionsExtensions
{
    public static Iyzipay.Options ToIyzicoSdkOptions(this IyzicoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new Iyzipay.Options
        {
            ApiKey = options.ApiKey,
            SecretKey = options.SecretKey,
            BaseUrl = options.BaseUrl,
        };
    }
}
