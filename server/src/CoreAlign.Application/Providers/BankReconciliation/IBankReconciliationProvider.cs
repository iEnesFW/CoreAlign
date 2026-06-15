namespace CoreAlign.Application.Providers.BankReconciliation;

public enum BankStatementFormat
{
    Mt940,
    GarantiFast,
    IsBankCsv,
    Camt053,
    GenericCsv
}

public sealed record BankCredentials(
    string? CustomerNumber,
    string? ApiKey,
    string? AccountNumber);

public sealed record BankTransactionParsed(
    string TransactionId,
    DateOnly BookingDate,
    decimal Amount,
    string Currency,
    string Description,
    string CounterpartyName,
    string Reference);

public sealed record BankStatementParseResult(
    IReadOnlyList<BankTransactionParsed> Transactions,
    DateTime ParsedAtUtc,
    string[] WarningKeys);

public interface IBankReconciliationProvider : IExternalProvider
{
    BankStatementFormat Format { get; }

    Task<BankStatementParseResult> ParseAsync(Stream file, BankCredentials? creds, CancellationToken ct);

    Task<BankStatementParseResult> PullAsync(DateOnly from, DateOnly to, BankCredentials creds, CancellationToken ct);
}
