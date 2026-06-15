using System.Globalization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreAlign.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRecentMfaAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string ClaimType = "mfa_verified_at";
    public const string ChallengeUri = "/api/v1/auth/2fa/step-up";

    public int MaxAgeMinutes { get; set; } = 5;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Failure("Authentication required.", 401))
            {
                StatusCode = 401,
            };
            return Task.CompletedTask;
        }

        var claim = user.FindFirst(ClaimType);
        if (claim is null || !long.TryParse(claim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
        {
            context.Result = BuildMfaRequiredResult();
            return Task.CompletedTask;
        }

        var verifiedAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var age = DateTimeOffset.UtcNow - verifiedAt;
        if (age.TotalMinutes > MaxAgeMinutes)
        {
            context.Result = BuildMfaRequiredResult();
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private static ObjectResult BuildMfaRequiredResult()
    {
        var payload = new
        {
            error = new
            {
                code = "MFA_REQUIRED",
                challengeUri = ChallengeUri,
            },
        };
        return new ObjectResult(payload)
        {
            StatusCode = StatusCodes.Status428PreconditionRequired,
        };
    }
}
