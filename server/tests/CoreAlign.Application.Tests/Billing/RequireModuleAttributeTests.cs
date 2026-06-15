using System.Security.Claims;
using CoreAlign.API.Common;
using CoreAlign.Application.Billing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Tests.Billing;

public class RequireModuleAttributeTests
{
    private static AuthorizationFilterContext BuildContext(IActiveModulesService modules, bool authenticated)
    {
        var services = new ServiceCollection();
        services.AddSingleton(modules);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        if (authenticated)
        {
            var identity = new ClaimsIdentity(authenticationType: "Test");
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
            httpContext.User = new ClaimsPrincipal(identity);
        }
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task Allows_when_module_active()
    {
        var modules = Substitute.For<IActiveModulesService>();
        modules.IsActiveAsync("Reports", Arg.Any<CancellationToken>()).Returns(true);
        var ctx = BuildContext(modules, authenticated: true);

        var attr = new RequireModuleAttribute("Reports");
        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task Denies_when_module_inactive()
    {
        var modules = Substitute.For<IActiveModulesService>();
        modules.IsActiveAsync("Reports", Arg.Any<CancellationToken>()).Returns(false);
        var ctx = BuildContext(modules, authenticated: true);

        var attr = new RequireModuleAttribute("Reports");
        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeOfType<ObjectResult>();
        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Skips_for_unauthenticated_request()
    {
        var modules = Substitute.For<IActiveModulesService>();
        modules.IsActiveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var ctx = BuildContext(modules, authenticated: false);

        var attr = new RequireModuleAttribute("Reports");
        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
        await modules.DidNotReceive().IsActiveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
