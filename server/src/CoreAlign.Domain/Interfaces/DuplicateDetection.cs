namespace CoreAlign.Domain.Interfaces;

public enum DuplicateKeyKind
{
    Email,
    TaxNumber,
    NationalId,
}

public record DuplicateMemberRow(Guid Id, string Name);

public record DuplicateGroupRow(string KeyValue, int Count, IReadOnlyList<DuplicateMemberRow> Members);
