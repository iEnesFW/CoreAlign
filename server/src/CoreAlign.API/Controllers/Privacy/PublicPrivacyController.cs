using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Privacy;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Privacy;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/public/privacy")]
public class PublicPrivacyController : ControllerBase
{
    private readonly IDataSubjectRequestService _service;
    private readonly ITenantContext _tenant;

    public PublicPrivacyController(IDataSubjectRequestService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpPost("request")]
    public async Task<IActionResult> SubmitAnonymously(
        [FromBody] PublicPrivacyRequestBody body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.RequesterEmail))
        {
            return BadRequest(new { error = "Privacy.EmailRequired" });
        }

        if (body.TenantId == Guid.Empty)
        {
            return BadRequest(new { error = "Privacy.TenantRequired" });
        }

        using var scope = _tenant.PushScope(body.TenantId);

        var input = new SubmitDataSubjectRequestInput(
            body.Type,
            null,
            null,
            body.RequesterEmail,
            body.Notes);

        var result = await _service.SubmitAsync(input, ct);

        return new PublicPrivacyRequestAccepted(result.Id, "Privacy.VerificationEmailSent").ToAccepted();
    }
}

public sealed record PublicPrivacyRequestBody(
    Guid TenantId,
    DataSubjectRequestType Type,
    string RequesterEmail,
    string? Notes);

public sealed record PublicPrivacyRequestAccepted(Guid RequestId, string NoticeKey);
