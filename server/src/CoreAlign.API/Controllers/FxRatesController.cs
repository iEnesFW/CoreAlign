using Asp.Versioning;
using CoreAlign.Application.Fx;
using CoreAlign.Application.Treasury.Fx;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fx-rates")]
public class FxRatesController : ControllerBase
{
    private readonly IFxRateProvider _fxProvider;
    private readonly IMediator _mediator;

    public FxRatesController(IFxRateProvider fxProvider, IMediator mediator)
    {
        _fxProvider = fxProvider;
        _mediator = mediator;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        var rates = await _fxProvider.GetLatestAsync(ct);
        return Ok(rates);
    }

    [HttpGet("{currencyCode}")]
    public async Task<IActionResult> GetRate(string currencyCode, [FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var rate = await _fxProvider.GetRateAsync(currencyCode, asOfDate ?? DateTime.UtcNow, ct);
        return rate is null ? NotFound() : Ok(rate);
    }

    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] FxConvertRequest request, CancellationToken ct)
    {
        var converted = await _fxProvider.ConvertAsync(
            request.Amount,
            request.FromCurrency,
            request.ToCurrency,
            request.AsOfDate ?? DateTime.UtcNow,
            ct);
        return Ok(new FxConvertResponse(
            FromCurrency: request.FromCurrency.ToUpperInvariant(),
            ToCurrency: request.ToCurrency.ToUpperInvariant(),
            OriginalAmount: request.Amount,
            ConvertedAmount: converted,
            AsOfDate: request.AsOfDate ?? DateTime.UtcNow));
    }

    [HttpPost("sync")]
    [Authorize(Policy = FxRatesPolicies.AdminFxSync)]
    public IActionResult ManualSync()
    {
        return StatusCode(StatusCodes.Status410Gone, new
        {
            error = "deprecated",
            message = "POST /api/v1/fx-rates/sync is retired. Use POST /api/v1/fx-rates/poll (the Phase 40 TcmbFxIngestJob pipeline).",
        });
    }

    // TriggerTcmbFxPollCommand had a handler but no route, so the only way to refresh rates
    // out-of-band was the Hangfire dashboard. The pickable currency catalogue now derives from this
    // feed, which makes an operator-triggerable refresh part of the contract rather than a nicety.
    [HttpPost("poll")]
    [Authorize(Policy = FxRatesPolicies.AdminFxSync)]
    public async Task<IActionResult> Poll(CancellationToken ct)
    {
        var upserted = await _mediator.Send(new TriggerTcmbFxPollCommand(), ct);
        return Ok(new FxPollResponse(upserted));
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetFxPreferencesQuery(), ct);
        return Ok(dto);
    }

    [HttpPut("preferences")]
    [Authorize(Policy = FxRatesPolicies.AdminFxSync)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateFxPreferencesCommand command, CancellationToken ct)
    {
        var dto = await _mediator.Send(command, ct);
        return Ok(dto);
    }

    [HttpGet("resolve/{currencyCode}")]
    public async Task<IActionResult> Resolve(string currencyCode, [FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var dto = await _mediator.Send(new ResolveFxRateQuery(currencyCode, asOfDate), ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record FxConvertRequest(decimal Amount, string FromCurrency, string ToCurrency, DateTime? AsOfDate);

public sealed record FxConvertResponse(string FromCurrency, string ToCurrency, decimal OriginalAmount, decimal ConvertedAmount, DateTime AsOfDate);

public static class FxRatesPolicies
{
    public const string AdminFxSync = "Admin.FxSync";
}

public sealed record FxPollResponse(int UpsertedRateCount);
