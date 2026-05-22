using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Lookups;
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

    public LookupsController(ILookupQueryService lookups) => _lookups = lookups;

    [HttpGet("currencies")]
    public async Task<IActionResult> Currencies([FromQuery] bool? isActive, CancellationToken ct)
        => (await _lookups.GetCurrenciesAsync(isActive, ct)).ToOk();

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
