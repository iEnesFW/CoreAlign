using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.BankReconciliation;

namespace CoreAlign.Infrastructure.Providers.BankReconciliation;

public sealed class MockBankReconciliationProvider : IBankReconciliationProvider
{
    private static readonly DateTime ParsedAtUtcFixed = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly BookingDateFixed = new(2026, 1, 1);

    public string Name => "mock";

    public string DisplayName => "Mock Bank Reconciliation Provider";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.None,
        new Dictionary<string, string> { ["env"] = "dev" });

    public BankStatementFormat Format => BankStatementFormat.GenericCsv;

    public Task<BankStatementParseResult> ParseAsync(Stream file, BankCredentials? creds, CancellationToken ct) =>
        Task.FromResult(BuildResult());

    public Task<BankStatementParseResult> PullAsync(DateOnly from, DateOnly to, BankCredentials creds, CancellationToken ct) =>
        Task.FromResult(BuildResult());

    private static BankStatementParseResult BuildResult()
    {
        var transactions = new BankTransactionParsed[]
        {
            new(
                TransactionId: "mock-tx-0001",
                BookingDate: BookingDateFixed,
                Amount: 1000.00m,
                Currency: "TRY",
                Description: "Mock incoming transfer",
                CounterpartyName: "Mock Counterparty A",
                Reference: "MOCK-REF-0001"),
            new(
                TransactionId: "mock-tx-0002",
                BookingDate: BookingDateFixed,
                Amount: -250.50m,
                Currency: "TRY",
                Description: "Mock outgoing payment",
                CounterpartyName: "Mock Counterparty B",
                Reference: "MOCK-REF-0002")
        };

        return new BankStatementParseResult(
            Transactions: transactions,
            ParsedAtUtc: ParsedAtUtcFixed,
            WarningKeys: Array.Empty<string>());
    }
}
