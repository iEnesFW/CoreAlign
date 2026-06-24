namespace CoreAlign.Application.Auth.Services;

/// <summary>
/// Server-side verification of a client-supplied CAPTCHA token (e.g. Google
/// reCAPTCHA v3). Anti-automation defence for unauthenticated, abuse-prone
/// endpoints (registration, password reset). Implementations MUST fail-open
/// (return true) when CAPTCHA is not configured so non-production environments
/// and tests are unaffected, and fail-closed (return false) on a present-but-
/// invalid token when configured.
/// </summary>
public interface ICaptchaVerifier
{
    /// <summary>
    /// Returns true when the token is valid (or CAPTCHA is disabled), false when
    /// CAPTCHA is enabled and the token is missing/invalid/below the score floor.
    /// </summary>
    Task<bool> VerifyAsync(string? token, string action, CancellationToken cancellationToken = default);
}
