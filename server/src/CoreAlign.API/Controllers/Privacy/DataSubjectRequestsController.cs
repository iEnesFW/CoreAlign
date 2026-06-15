using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Privacy;
using CoreAlign.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Privacy;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/privacy/requests")]
public class DataSubjectRequestsController : ControllerBase
{
    private readonly IDataSubjectRequestService _service;

    public DataSubjectRequestsController(IDataSubjectRequestService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitDataSubjectRequestBody body,
        CancellationToken ct)
    {
        var input = new SubmitDataSubjectRequestInput(
            body.Type,
            body.RequesterUserId,
            body.RequesterCustomerId,
            body.RequesterEmail,
            body.Notes);

        var result = await _service.SubmitAsync(input, ct);
        return result.ToCreated();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _service.GetAsync(id, ct)).ToOk();
}

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = PersonaPolicies.TenantAdminRole)]
[Route("api/v{version:apiVersion}/admin/privacy/requests")]
public class AdminDataSubjectRequestsController : ControllerBase
{
    private readonly IDataSubjectRequestService _service;

    public AdminDataSubjectRequestsController(IDataSubjectRequestService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DataSubjectRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        (await _service.ListAsync(status, page, pageSize, ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await _service.GetAsync(id, ct)).ToOk();

    [HttpPost("{id:guid}/process")]
    [Authorization.RequireRecentMfa]
    public async Task<IActionResult> Process(
        Guid id,
        [FromBody] ProcessDataSubjectRequestBody body,
        CancellationToken ct)
    {
        DataSubjectRequestDto result = body.Action switch
        {
            ProcessAction.Access => await _service.ProcessAccessRequestAsync(id, ct),
            ProcessAction.Erasure => await _service.ProcessErasureRequestAsync(id, body.KeepFinancialTrail, ct),
            ProcessAction.Portability => await _service.ProcessPortabilityRequestAsync(id, ct),
            ProcessAction.Rectification => await _service.ProcessRectificationRequestAsync(
                id,
                new RectificationCorrections(
                    body.Corrections?.FirstName,
                    body.Corrections?.LastName,
                    body.Corrections?.PhoneNumber,
                    body.Corrections?.Email),
                ct),
            ProcessAction.Reject => await _service.RejectAsync(id, body.RejectionReason ?? string.Empty, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(body), body.Action, "Unknown process action.")
        };
        return result.ToOk();
    }
}

public sealed record SubmitDataSubjectRequestBody(
    DataSubjectRequestType Type,
    Guid? RequesterUserId,
    Guid? RequesterCustomerId,
    string? RequesterEmail,
    string? Notes);

public enum ProcessAction
{
    Access = 0,
    Erasure = 1,
    Portability = 2,
    Rectification = 3,
    Reject = 4,
}

public sealed record ProcessDataSubjectRequestBody(
    ProcessAction Action,
    bool KeepFinancialTrail = true,
    string? RejectionReason = null,
    RectificationCorrectionsBody? Corrections = null);

public sealed record RectificationCorrectionsBody(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email);
