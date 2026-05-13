using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/master-data")]
public class MasterDataController : ControllerBase
{
    private readonly IMediator _mediator;
    public MasterDataController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    // ---------- Brands ----------
    [HttpGet("brands")]
    public async Task<IActionResult> ListBrands([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListBrandsQuery(isActive), ct)).ToOk();

    [HttpGet("brands/{id:guid}")]
    public async Task<IActionResult> GetBrand(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetBrandByIdQuery(id), ct)).ToOk();

    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("brands/{id:guid}")]
    public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] UpdateBrandCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("brands/{id:guid}")]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteBrandCommand(id), ct)).ToOk();

    // ---------- Categories ----------
    [HttpGet("categories")]
    public async Task<IActionResult> ListCategories([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListProductCategoriesQuery(isActive), ct)).ToOk();

    [HttpGet("categories/{id:guid}")]
    public async Task<IActionResult> GetCategory(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetProductCategoryByIdQuery(id), ct)).ToOk();

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateProductCategoryCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateProductCategoryCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteProductCategoryCommand(id), ct)).ToOk();

    // ---------- Customer Groups ----------
    [HttpGet("customer-groups")]
    public async Task<IActionResult> ListCustomerGroups([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListCustomerGroupsQuery(isActive), ct)).ToOk();

    [HttpGet("customer-groups/{id:guid}")]
    public async Task<IActionResult> GetCustomerGroup(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetCustomerGroupByIdQuery(id), ct)).ToOk();

    [HttpPost("customer-groups")]
    public async Task<IActionResult> CreateCustomerGroup([FromBody] CreateCustomerGroupCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("customer-groups/{id:guid}")]
    public async Task<IActionResult> UpdateCustomerGroup(Guid id, [FromBody] UpdateCustomerGroupCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("customer-groups/{id:guid}")]
    public async Task<IActionResult> DeleteCustomerGroup(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteCustomerGroupCommand(id), ct)).ToOk();

    // ---------- UoM ----------
    [HttpGet("units-of-measure")]
    public async Task<IActionResult> ListUoms([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListUnitsOfMeasureQuery(isActive), ct)).ToOk();

    [HttpGet("units-of-measure/{id:guid}")]
    public async Task<IActionResult> GetUom(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetUnitOfMeasureByIdQuery(id), ct)).ToOk();

    [HttpPost("units-of-measure")]
    public async Task<IActionResult> CreateUom([FromBody] CreateUnitOfMeasureCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("units-of-measure/{id:guid}")]
    public async Task<IActionResult> UpdateUom(Guid id, [FromBody] UpdateUnitOfMeasureCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("units-of-measure/{id:guid}")]
    public async Task<IActionResult> DeleteUom(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteUnitOfMeasureCommand(id), ct)).ToOk();

    // ---------- Tax Rates ----------
    [HttpGet("tax-rates")]
    public async Task<IActionResult> ListTaxRates([FromQuery] bool? isActive, [FromQuery] bool? isWithholding, CancellationToken ct)
        => (await _mediator.Send(new ListTaxRatesQuery(isActive, isWithholding), ct)).ToOk();

    [HttpGet("tax-rates/{id:guid}")]
    public async Task<IActionResult> GetTaxRate(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetTaxRateByIdQuery(id), ct)).ToOk();

    [HttpPost("tax-rates")]
    public async Task<IActionResult> CreateTaxRate([FromBody] CreateTaxRateCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("tax-rates/{id:guid}")]
    public async Task<IActionResult> UpdateTaxRate(Guid id, [FromBody] UpdateTaxRateCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("tax-rates/{id:guid}")]
    public async Task<IActionResult> DeleteTaxRate(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteTaxRateCommand(id), ct)).ToOk();

    // ---------- Payment Terms ----------
    [HttpGet("payment-terms")]
    public async Task<IActionResult> ListPaymentTerms([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListPaymentTermsQuery(isActive), ct)).ToOk();

    [HttpGet("payment-terms/{id:guid}")]
    public async Task<IActionResult> GetPaymentTerm(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetPaymentTermByIdQuery(id), ct)).ToOk();

    [HttpPost("payment-terms")]
    public async Task<IActionResult> CreatePaymentTerm([FromBody] CreatePaymentTermCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("payment-terms/{id:guid}")]
    public async Task<IActionResult> UpdatePaymentTerm(Guid id, [FromBody] UpdatePaymentTermCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("payment-terms/{id:guid}")]
    public async Task<IActionResult> DeletePaymentTerm(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeletePaymentTermCommand(id), ct)).ToOk();

    // ---------- Price Lists ----------
    [HttpGet("price-lists")]
    public async Task<IActionResult> ListPriceLists([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListPriceListsQuery(isActive), ct)).ToOk();

    [HttpGet("price-lists/{id:guid}")]
    public async Task<IActionResult> GetPriceList(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetPriceListByIdQuery(id), ct)).ToOk();

    [HttpPost("price-lists")]
    public async Task<IActionResult> CreatePriceList([FromBody] CreatePriceListCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("price-lists/{id:guid}")]
    public async Task<IActionResult> UpdatePriceList(Guid id, [FromBody] UpdatePriceListCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("price-lists/{id:guid}")]
    public async Task<IActionResult> DeletePriceList(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeletePriceListCommand(id), ct)).ToOk();

    // ---------- Warehouses ----------
    [HttpGet("warehouses")]
    public async Task<IActionResult> ListWarehouses([FromQuery] bool? isActive, CancellationToken ct)
        => (await _mediator.Send(new ListWarehousesQuery(isActive), ct)).ToOk();

    [HttpGet("warehouses/{id:guid}")]
    public async Task<IActionResult> GetWarehouse(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetWarehouseByIdQuery(id), ct)).ToOk();

    [HttpPost("warehouses")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("warehouses/{id:guid}")]
    public async Task<IActionResult> UpdateWarehouse(Guid id, [FromBody] UpdateWarehouseCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("warehouses/{id:guid}")]
    public async Task<IActionResult> DeleteWarehouse(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteWarehouseCommand(id), ct)).ToOk();
}
