using MediatR;

namespace CoreAlign.Application.Mrp.Distribution;

public record MrpTransferSuggestionDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid FromWarehouseId,
    string FromWarehouseCode,
    string FromWarehouseName,
    Guid ToWarehouseId,
    string ToWarehouseCode,
    string ToWarehouseName,
    decimal Quantity);

public record MrpWarehouseNetPositionDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    decimal Available,
    decimal Demand,
    decimal Net);

public record MrpExternalReplenishmentDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    decimal Quantity);

public record MrpTransferSuggestionsResultDto(
    int ProductsEvaluated,
    int TransferCount,
    int ExternalReplenishmentCount,
    IReadOnlyList<MrpTransferSuggestionDto> Transfers,
    IReadOnlyList<MrpWarehouseNetPositionDto> NetPositions,
    IReadOnlyList<MrpExternalReplenishmentDto> ExternalReplenishment);

public record GetMrpTransferSuggestionsQuery() : IRequest<MrpTransferSuggestionsResultDto>;
