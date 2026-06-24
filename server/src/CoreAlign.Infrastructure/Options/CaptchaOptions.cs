namespace CoreAlign.Infrastructure.Options;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    /// <summary>When false (default) verification is skipped (fail-open) so dev/test are unaffected.</summary>
    public bool Enabled { get; set; }

    /// <summary>Google reCAPTCHA v3 secret key. Provided via env/user-secrets, never committed.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Minimum v3 score (0.0–1.0) to accept. Default 0.5.</summary>
    public double MinScore { get; set; } = 0.5;

    public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
}
