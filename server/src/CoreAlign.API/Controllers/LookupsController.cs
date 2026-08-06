using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Lookups;
using CoreAlign.Application.Treasury.Fx;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupQueryService _lookups;
    private readonly IMediator _mediator;

    public LookupsController(ILookupQueryService lookups, IMediator mediator)
    {
        _lookups = lookups;
        _mediator = mediator;
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> Currencies([FromQuery] bool? isActive, CancellationToken ct)
        => (await _lookups.GetCurrenciesAsync(isActive, ct)).ToOk();

    // The catalogue derives from the TCMB feed automatically; these two are the manual half.
    // PlatformAdmin-only because `currencies` is a GLOBAL table with no tenant column — one
    // tenant's admin editing it would change the pickable list for every other tenant.
    [HttpPut("currencies")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> UpsertCurrency([FromBody] UpsertCurrencyCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("currencies/{code}")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> DeactivateCurrency(string code, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateCurrencyCommand(code), ct);
        return NoContent();
    }

    [HttpGet("countries")]
    public async Task<IActionResult> Countries([FromQuery] bool? isActive, CancellationToken ct)
        => (await _lookups.GetCountriesAsync(isActive, ct)).ToOk();

    [HttpGet("provinces")]
    public async Task<IActionResult> Provinces([FromQuery] string? countryCode, CancellationToken ct)
        => (await _lookups.GetProvincesAsync(countryCode, ct)).ToOk();

    [HttpGet("districts")]
    public async Task<IActionResult> Districts([FromQuery] int? provinceId, CancellationToken ct)
        => (await _lookups.GetDistrictsAsync(provinceId, ct)).ToOk();
}
