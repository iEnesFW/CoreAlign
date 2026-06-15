using CoreAlign.Application.Billing;
using CoreAlign.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreAlign.API.Common;

/// <summary>
/// Authorization filter that returns 403 Forbidden if the current tenant does
/// not have the named module currently active. Strict: there is no admin bypass
/// — tenant administrators must still purchase a module to use it.
///
/// <para>Usage:</para>
/// <code>
/// [RequireModule("Reports")]
/// [HttpGet("export")]
/// public Task&lt;IActionResult&gt; Export() ...
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireModuleAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string ModuleCode { get; }

    public RequireModuleAttribute(string moduleCode)
    {
        if (string.IsNullOrWhiteSpace(moduleCode)) throw new ArgumentException("ModuleCode is required.", nameof(moduleCode));
        ModuleCode = moduleCode.Trim();
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var modules = context.HttpContext.RequestServices.GetService(typeof(IActiveModulesService)) as IActiveModulesService;
        if (modules is null)
        {
            return;
        }

        if (!await modules.IsActiveAsync(ModuleCode, context.HttpContext.RequestAborted))
        {
            var response = ApiResponse<object>.Failure($"Module '{ModuleCode}' is not active for the current tenant.", StatusCodes.Status403Forbidden);
            context.Result = new ObjectResult(response) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
