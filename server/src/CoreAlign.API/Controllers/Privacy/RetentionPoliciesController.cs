using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Privacy;
using CoreAlign.Domain.Entities.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Privacy;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = PersonaPolicies.TenantAdminRole)]
[Route("api/v{version:apiVersion}/admin/privacy/retention-policies")]
public class RetentionPoliciesController : ControllerBase
{
    private readonly IRetentionPolicyService _service;

    public RetentionPoliciesController(IRetentionPolicyService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        (await _service.ListAsync(ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertRetentionPolicyBody body,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(ToInput(body), ct);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertRetentionPolicyBody body,
        CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, ToInput(body), ct);
        return result.ToOk();
    }

    private static UpsertRetentionPolicyInput ToInput(UpsertRetentionPolicyBody body) =>
        new(body.EntityType, body.RetentionDays, body.ActionOnExpiry, body.KeepFinancialTrail, body.IsEnabled);
}

public sealed record UpsertRetentionPolicyBody(
    string EntityType,
    int RetentionDays,
    RetentionActionOnExpiry ActionOnExpiry,
    bool KeepFinancialTrail = true,
    bool IsEnabled = true);
