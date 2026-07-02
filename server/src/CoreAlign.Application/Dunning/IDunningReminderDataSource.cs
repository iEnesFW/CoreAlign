using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Dunning;

public sealed record DunningSettingSnapshot(
    Guid TenantId,
    DunningType Type,
    bool SendInApp,
    bool SendEmail,
    IReadOnlyList<Guid> RecipientUserIds);

public sealed record DueInvoiceReminder(
    Guid InvoiceId,
    string InvoiceNumber,
    DateTime DueDate,
    decimal AmountDue,
    string Currency);

public sealed record ExpiringQuoteReminder(
    Guid QuoteId,
    string QuoteNumber,
    DateTime ValidUntilUtc,
    string Currency);

public sealed record CriticalStockReminder(
    Guid ProductId,
    string Sku,
    string Name,
    decimal StockQuantity,
    decimal ReorderPoint);

public interface IDunningReminderDataSource
{
    Task<IReadOnlyList<DunningSettingSnapshot>> GetEnabledSettingsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<DueInvoiceReminder>> GetDueInvoicesAsync(
        Guid tenantId,
        DateTime dueOnOrBeforeUtc,
        int max,
        CancellationToken ct = default);

    Task<IReadOnlyList<ExpiringQuoteReminder>> GetExpiringQuotesAsync(
        Guid tenantId,
        DateTime nowUtc,
        DateTime expiringOnOrBeforeUtc,
        int max,
        CancellationToken ct = default);

    Task<IReadOnlyList<CriticalStockReminder>> GetCriticalStockAsync(
        Guid tenantId,
        int max,
        CancellationToken ct = default);
}
