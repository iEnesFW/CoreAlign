using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Auth.Queries;
using CoreAlign.Application.Common;
using CoreAlign.Infrastructure.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "corealign_refresh_token";
    private const string CookiePath = "/api/v1/auth";

    private readonly IMediator _mediator;
    private readonly JwtOptions _jwtOptions;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IMediator mediator, IOptions<JwtOptions> jwtOptions, IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _jwtOptions = jwtOptions.Value;
        _environment = environment;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var enrichedCommand = command with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var result = await _mediator.Send(enrichedCommand, cancellationToken);
        AttachRefreshTokenCookie(result);
        return result.ToOk();
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenCommand? command, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? command?.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            return StatusCode(401, ApiResponse<object>.Failure("Refresh token missing.", 401));
        }

        var enrichedCommand = new RefreshTokenCommand(
            refreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await _mediator.Send(enrichedCommand, cancellationToken);
        AttachRefreshTokenCookie(result);
        return result.ToOk();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? string.Empty;
        var result = await _mediator.Send(new LogoutCommand(refreshToken), cancellationToken);
        ClearRefreshTokenCookie();
        return result.ToOk();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        var result = await _mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
        var result = await _mediator.Send(command, cancellationToken);
        if (result)
        {
            ClearRefreshTokenCookie();
        }
        return result.ToOk();
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        var command = new UpdateProfileCommand(userId, request.FirstName, request.LastName, request.PhoneNumber, request.AvatarUrl);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpPost("2fa/enroll")]
    [Authorize]
    public async Task<IActionResult> EnrollTwoFactorAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new EnrollTwoFactorCommand(CurrentUserId), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("2fa/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyTwoFactorAsync([FromBody] TwoFactorVerifyRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new VerifyTwoFactorEnrollmentCommand(CurrentUserId, request.Code), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactorAsync([FromBody] TwoFactorPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DisableTwoFactorCommand(CurrentUserId, request.Password), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("2fa/backup-codes/regenerate")]
    [Authorize]
    public async Task<IActionResult> RegenerateBackupCodesAsync([FromBody] TwoFactorPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegenerateBackupCodesCommand(CurrentUserId, request.Password), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("2fa/challenge")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> CompleteTwoFactorChallengeAsync([FromBody] TwoFactorChallengeRequest request, CancellationToken cancellationToken)
    {
        var command = new CompleteTwoFactorChallengeCommand(
            request.ChallengeToken,
            request.Code,
            request.BackupCode,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await _mediator.Send(command, cancellationToken);
        AttachRefreshTokenCookie(result);
        return result.ToOk();
    }

    [HttpPost("2fa/step-up")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> StepUpTwoFactorAsync([FromBody] TwoFactorStepUpRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StepUpTwoFactorCommand(CurrentUserId, request.Code), cancellationToken);
        return result.ToOk();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void AttachRefreshTokenCookie(AuthResponseDto result)
    {
        if (string.IsNullOrEmpty(result.RefreshToken))
        {
            return;
        }

        SetRefreshTokenCookie(result.RefreshToken);
        result.RefreshToken = string.Empty;
    }

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            Path = CookiePath
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = CookiePath });
    }
}
