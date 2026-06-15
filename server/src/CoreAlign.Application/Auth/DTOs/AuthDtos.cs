namespace CoreAlign.Application.Auth.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto? User { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorChallengeToken { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? Persona { get; set; }
    public string? PreferredLocale { get; set; }
}

public class SubscriptionDto
{
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? TrialEndAtUtc { get; set; }
    public bool IsTrialExpired { get; set; }
}
