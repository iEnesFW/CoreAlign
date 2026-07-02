namespace CoreAlign.Domain.Interfaces;

public record DocumentNumberGapRow(
    string DocumentType,
    string Prefix,
    int Year,
    long Expected,
    long UsedCount,
    long MaxUsed,
    long GapCount,
    IReadOnlyList<long> MissingNumbers);

public interface IDocumentNumberGapReader
{
    Task<IReadOnlyList<DocumentNumberGapRow>> GetGapsAsync(
        Guid tenantId,
        int? year,
        CancellationToken cancellationToken = default);
}
