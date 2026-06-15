using System.Security.Claims;
using CoreAlign.API.Authorization;
using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.Handlers;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace CoreAlign.Application.Tests.Auth;

public class StepUpMfaTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITwoFactorService _twoFactorService = Substitute.For<ITwoFactorService>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IUserMembershipService _userMembershipService = Substitute.For<IUserMembershipService>();

    private static readonly Guid TenantId = Guid.NewGuid();

    private StepUpTwoFactorCommandHandler BuildSut() => new(
        _userRepository, _twoFactorService, _jwtTokenService, _userMembershipService);

    [Fact]
    public async Task StepUp_with_valid_code_returns_new_access_token_with_mfa_claim()
    {
        var user = BuildUser();
        user.IsTwoFactorEnabled = true;
        user.TwoFactorSecretKey = "SECRET";
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _twoFactorService.Verify("SECRET", "123456", 1).Returns(true);
        _userMembershipService.ResolvePersonaAsync(user.Id, user.TenantId, Arg.Any<CancellationToken>())
            .Returns(UserPersona.Tenant);
        _jwtTokenService.GenerateAccessToken(
            user.Id, user.TenantId, user.Email,
            Arg.Any<IEnumerable<string>>(),
            "tenant",
            Arg.Any<DateTime?>()).Returns("new-access-token");

        var sut = BuildSut();
        var result = await sut.Handle(new StepUpTwoFactorCommand(user.Id, "123456"), CancellationToken.None);

        result.AccessToken.Should().Be("new-access-token");
        result.MfaVerifiedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StepUp_with_wrong_code_throws()
    {
        var user = BuildUser();
        user.IsTwoFactorEnabled = true;
        user.TwoFactorSecretKey = "SECRET";
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _twoFactorService.Verify("SECRET", "000000", 1).Returns(false);

        var sut = BuildSut();
        var act = async () => await sut.Handle(new StepUpTwoFactorCommand(user.Id, "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTwoFactorCodeException>();
    }

    [Fact]
    public async Task StepUp_when_2fa_not_enabled_throws()
    {
        var user = BuildUser();
        user.IsTwoFactorEnabled = false;
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sut = BuildSut();
        var act = async () => await sut.Handle(new StepUpTwoFactorCommand(user.Id, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<TwoFactorNotEnabledException>();
    }

    private static User BuildUser()
    {
        return new User(TenantId, "tester", "tester@example.com", "hashed-pw")
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            IsEmailConfirmed = true,
        };
    }
}

public class RequireRecentMfaAttributeTests
{
    [Fact]
    public async Task Without_mfa_claim_returns_428()
    {
        var attr = new RequireRecentMfaAttribute { MaxAgeMinutes = 5 };
        var ctx = BuildContext(mfaClaim: null);

        await attr.OnAuthorizationAsync(ctx);

        var result = ctx.Result as ObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status428PreconditionRequired);
    }

    [Fact]
    public async Task With_stale_mfa_claim_returns_428()
    {
        var attr = new RequireRecentMfaAttribute { MaxAgeMinutes = 5 };
        var staleSeconds = DateTimeOffset.UtcNow.AddMinutes(-30).ToUnixTimeSeconds();
        var ctx = BuildContext(mfaClaim: staleSeconds.ToString());

        await attr.OnAuthorizationAsync(ctx);

        var result = ctx.Result as ObjectResult;
        result!.StatusCode.Should().Be(StatusCodes.Status428PreconditionRequired);
    }

    [Fact]
    public async Task With_fresh_mfa_claim_allows_request()
    {
        var attr = new RequireRecentMfaAttribute { MaxAgeMinutes = 5 };
        var freshSeconds = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds();
        var ctx = BuildContext(mfaClaim: freshSeconds.ToString());

        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
    }

    private static AuthorizationFilterContext BuildContext(string? mfaClaim)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };
        if (mfaClaim is not null)
        {
            claims.Add(new Claim(RequireRecentMfaAttribute.ClaimType, mfaClaim));
        }
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
