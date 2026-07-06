using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Inventory.Serials;

public class RegisterSerialUnitsCommandHandler : IRequestHandler<RegisterSerialUnitsCommand, int>
{
    private readonly IProductRepository _products;
    private readonly ISerialUnitRepository _serials;

    public RegisterSerialUnitsCommandHandler(IProductRepository products, ISerialUnitRepository serials)
    {
        _products = products;
        _serials = serials;
    }

    public async Task<int> Handle(RegisterSerialUnitsCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException();
        if (!product.IsSerialTracked)
        {
            throw new ProductNotSerialTrackedException(request.ProductId);
        }

        var numbers = request.SerialNumbers
            .Select(s => s?.Trim() ?? string.Empty)
            .Where(s => s.Length > 0)
            .Distinct()
            .ToList();

        var existing = await _serials.GetExistingSerialNumbersAsync(request.ProductId, numbers, cancellationToken);
        if (existing.Count > 0)
        {
            throw new DuplicateSerialUnitException(request.ProductId, existing);
        }

        var now = DateTime.UtcNow;
        var units = numbers.Select(sn => new SerialUnit(
            productId: request.ProductId,
            serialNumber: sn,
            receivedAtUtc: now,
            warehouseId: request.WarehouseId,
            lotId: request.LotId,
            unitCost: request.UnitCost,
            sourceReceiptMovementId: request.SourceReceiptMovementId,
            parentSerialUnitId: request.ParentSerialUnitId)).ToList();

        await _serials.AddRangeAsync(units, cancellationToken);
        return units.Count;
    }
}

public class ShipSerialUnitsCommandHandler : IRequestHandler<ShipSerialUnitsCommand, int>
{
    private readonly ISerialUnitRepository _serials;

    public ShipSerialUnitsCommandHandler(ISerialUnitRepository serials) => _serials = serials;

    public async Task<int> Handle(ShipSerialUnitsCommand request, CancellationToken cancellationToken)
    {
        var numbers = request.SerialNumbers.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
        var units = await _serials.GetBySerialNumbersAsync(request.ProductId, numbers, cancellationToken);
        var byNumber = units.ToDictionary(u => u.SerialNumber);

        var now = DateTime.UtcNow;
        var shipped = 0;
        foreach (var sn in numbers)
        {
            if (!byNumber.TryGetValue(sn, out var unit))
            {
                throw new SerialUnitNotFoundException(request.ProductId, sn);
            }
            unit.Ship(request.OrderId, request.ShipmentId, request.CustomerId, now);
            _serials.Update(unit);
            shipped++;
        }
        return shipped;
    }
}

public class GetSerialWhereUsedQueryHandler : IRequestHandler<GetSerialWhereUsedQuery, IReadOnlyList<SerialWhereUsedDto>>
{
    private readonly ISerialUnitRepository _serials;

    public GetSerialWhereUsedQueryHandler(ISerialUnitRepository serials) => _serials = serials;

    public async Task<IReadOnlyList<SerialWhereUsedDto>> Handle(GetSerialWhereUsedQuery request, CancellationToken cancellationToken)
    {
        var matches = await _serials.GetBySerialNumberAsync(request.SerialNumber.Trim(), cancellationToken);
        var result = new List<SerialWhereUsedDto>(matches.Count);
        foreach (var unit in matches)
        {
            var children = await _serials.GetChildrenAsync(unit.Id, cancellationToken);
            result.Add(new SerialWhereUsedDto(
                unit.Id,
                unit.ProductId,
                unit.SerialNumber,
                unit.Status.ToString(),
                unit.WarehouseId,
                unit.LotId,
                unit.UnitCost,
                unit.ReceivedAtUtc,
                unit.OrderId,
                unit.ShipmentId,
                unit.CurrentOwnerCustomerId,
                unit.ParentSerialUnitId,
                children.Select(c => new SerialComponentDto(c.Id, c.ProductId, c.SerialNumber, c.Status.ToString())).ToList()));
        }
        return result;
    }
}
