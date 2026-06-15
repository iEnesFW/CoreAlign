namespace CoreAlign.Application.Customers.Statements;

public sealed record CustomerStatementLineDto(
    DateTime OccurredAtUtc,
    string EntryKind,
    string DocumentNumber,
    string? Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string Currency);

public sealed class CustomerStatementDto
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerCode { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public decimal TotalDebit { get; init; }
    public decimal TotalCredit { get; init; }
    public IReadOnlyList<CustomerStatementLineDto> Lines { get; init; } = Array.Empty<CustomerStatementLineDto>();
}
