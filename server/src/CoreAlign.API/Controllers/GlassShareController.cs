using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[EnableRateLimiting("global")]
[Route("api/v{version:apiVersion}/share/glass")]
public class GlassShareController : ControllerBase
{
    private readonly IMediator _mediator;
    public GlassShareController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{token}")]
    public async Task<IActionResult> GetSharedProject(string token, CancellationToken ct)
    {
        var ipHash = HashIp(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anon");
        var result = await _mediator.Send(new GetShareViewerProjectQuery(token, ipHash), ct);
        if (result is null) return NotFound();
        return result.ToOk();
    }

    [HttpPost("{token}/action")]
    public async Task<IActionResult> RecordAction(string token, [FromBody] ShareViewerActionDto data, CancellationToken ct) =>
        (await _mediator.Send(new RecordShareViewerActionCommand(token, data), ct)).ToOk();

    private static string HashIp(string ip)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(bytes);
    }
}
