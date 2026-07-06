using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Ingestion;
using CoreAlign.Application.B2B;
using CoreAlign.Domain.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai-helper")]
public sealed class AiHelperController : ControllerBase
{
    private const int MaxQuestionLength = 4000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiHelperService _service;
    private readonly IKnowledgeIngestionService _ingestion;
    private readonly IAiHelperFeedbackWriter _feedback;
    private readonly AiHelperOptions _options;
    private readonly ITenantContext _tenantContext;
    private readonly IPortalScopeService _portalScope;
    private readonly ILogger<AiHelperController> _logger;

    public AiHelperController(
        IAiHelperService service,
        IKnowledgeIngestionService ingestion,
        IAiHelperFeedbackWriter feedback,
        IOptions<AiHelperOptions> options,
        ITenantContext tenantContext,
        IPortalScopeService portalScope,
        ILogger<AiHelperController> logger)
    {
        _service = service;
        _ingestion = ingestion;
        _feedback = feedback;
        _options = options.Value;
        _tenantContext = tenantContext;
        _portalScope = portalScope;
        _logger = logger;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType<AiHelperStatusResponse>(StatusCodes.Status200OK)]
    public IActionResult Status() => Ok(new AiHelperStatusResponse(_options.Enabled));

    [HttpPost("ask")]
    [EnableRateLimiting("ai-helper")]
    public async Task AskAsync([FromBody] AskAiHelperRequest request, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        var tenantId = _tenantContext.CurrentTenantId;
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale.Trim();
        var question = (request.Question ?? string.Empty).Trim();
        if (question.Length == 0)
        {
            throw new ValidationException("AI Helper question is required.");
        }
        if (question.Length > MaxQuestionLength)
        {
            question = question[..MaxQuestionLength];
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        var conversationId = request.ConversationId is { } cid && cid != Guid.Empty ? cid : Guid.CreateVersion7();

        Guid? customerId = null;
        if (!roles.Contains("TenantAdmin"))
        {
            try
            {
                customerId = await _portalScope.GetCurrentCustomerIdAsync(ct);
            }
            catch (Exception)
            {
                customerId = null;
            }
        }

        var query = new AiHelperQuery(
            question, locale, request.RoutePath, tenantId, !tenantId.HasValue, roles, conversationId, userId,
            request.PageEntityType, request.PageEntityId, customerId);

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var ev in _service.AskAsync(query, ct))
            {
                switch (ev)
                {
                    case AiHelperSourcesEvent s:
                        await WriteEventAsync("sources", JsonSerializer.Serialize(new { sources = s.Sources }, JsonOptions), ct);
                        break;
                    case AiHelperTokenEvent t:
                        await WriteEventAsync("token", JsonSerializer.Serialize(new { text = t.Text }, JsonOptions), ct);
                        break;
                    case AiHelperDoneEvent d:
                        await WriteEventAsync("done", JsonSerializer.Serialize(new { answerId = d.AnswerId }, JsonOptions), ct);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Helper stream failed");
            await WriteEventAsync("error", "{\"message\":\"stream_failed\"}", CancellationToken.None);
        }
    }

    [HttpPost("admin/reindex")]
    [Authorize(Roles = "TenantAdmin")]
    [EnableRateLimiting("ai-helper")]
    public async Task<IActionResult> ReindexAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var result = await _ingestion.ReindexAsync(ct);
        return Ok(result);
    }

    [HttpPost("feedback")]
    [EnableRateLimiting("ai-helper")]
    public async Task<IActionResult> FeedbackAsync([FromBody] AiHelperFeedbackRequest request, CancellationToken ct)
    {
        if (request is null || request.AnswerId == Guid.Empty)
        {
            throw new ValidationException("AI Helper feedback requires a valid answerId.");
        }

        await _feedback.SubmitAsync(request.AnswerId, request.IsHelpful, request.Reason, _tenantContext.CurrentTenantId, ct);
        return Accepted();
    }

    private async Task WriteEventAsync(string eventName, string data, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {eventName}\n", ct);
        await Response.WriteAsync($"data: {data}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}

public sealed record AskAiHelperRequest(
    string? Question,
    string? Locale,
    string? RoutePath,
    Guid? ConversationId,
    string? PageEntityType = null,
    Guid? PageEntityId = null);

public sealed record AiHelperFeedbackRequest(Guid AnswerId, bool IsHelpful, string? Reason);

public sealed record AiHelperStatusResponse(bool Enabled);
