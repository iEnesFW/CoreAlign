namespace CoreAlign.Application.Stock.Substitute;

public interface IProductSubstituteResolver
{
    Task<IReadOnlyList<SubstituteSuggestion>> ResolveAsync(
        Guid productId,
        decimal requiredQuantity,
        int maxDepth = 3,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<SubstituteSuggestion>>> ResolveBatchAsync(
        IEnumerable<Guid> productIds,
        int maxDepth = 3,
        CancellationToken cancellationToken = default);
}

public sealed record SubstituteSuggestion(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal ConversionRate,
    int Priority,
    int Depth,
    string? Notes);
