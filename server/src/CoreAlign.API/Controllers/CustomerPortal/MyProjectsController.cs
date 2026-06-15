using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.CustomerPortal;

[ApiController]
[Authorize(Policy = CustomerPortalPolicies.SelfService)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-portal/glass-projects")]
public class MyProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentCustomerAccessor _currentCustomer;

    public MyProjectsController(IMediator mediator, ICurrentCustomerAccessor currentCustomer)
    {
        _mediator = mediator;
        _currentCustomer = currentCustomer;
    }

    [HttpGet]
    public async Task<IActionResult> ListMy(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var query = new GetGlassProjectsQuery(search, null, customerId, null, null, page, pageSize);
        return (await _mediator.Send(query, ct)).ToOk();
    }

    [HttpGet("{id:guid}/installation-status")]
    public async Task<IActionResult> GetInstallationStatus(Guid id, CancellationToken ct)
    {
        var customerId = await _currentCustomer.GetCustomerIdOrThrowAsync(ct);
        var project = await _mediator.Send(new GetGlassProjectByIdQuery(id), ct);
        if (project is null || project.CustomerId != customerId)
        {
            return NotFound(ApiResponse<object>.Failure("Project not found.", 404));
        }

        var status = new MyProjectInstallationStatusDto(
            project.Id,
            project.Code,
            project.ProjectName,
            project.Status,
            project.SiteCity,
            project.SiteDistrict,
            project.ValidUntilDate,
            project.UpdatedAtUtc);
        return status.ToOk();
    }
}

public record MyProjectInstallationStatusDto(
    Guid Id,
    string Code,
    string ProjectName,
    Domain.Enums.GlassProjectStatus Status,
    string? SiteCity,
    string? SiteDistrict,
    DateTime? ValidUntilDate,
    DateTime UpdatedAtUtc);
