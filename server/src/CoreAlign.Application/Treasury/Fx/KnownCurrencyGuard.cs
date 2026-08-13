using CoreAlign.Domain.Entities;
using FluentValidation;

namespace CoreAlign.Application.Treasury.Fx;

/// <summary>
/// Answers "may a document be denominated in this code?" from the currency catalogue.
/// </summary>
/// <remarks>
/// Shape-only validation (three letters) accepts a typo like TRL or a currency the tenant has no
/// rate for; the document is then created and the mistake only surfaces later as a failed FX
/// lookup or a silently un-converted amount.
/// </remarks>
public interface IKnownCurrencyGuard
{
    Task<bool> IsUsableAsync(string? code, CancellationToken cancellationToken = default);
}

public sealed class KnownCurrencyGuard : IKnownCurrencyGuard
{
    private readonly ICurrencyCatalog _catalog;
    private IReadOnlySet<string>? _usable;

    public KnownCurrencyGuard(ICurrencyCatalog catalog) => _catalog = catalog;

    public async Task<bool> IsUsableAsync(string? code, CancellationToken cancellationToken = default)
    {
        // An empty code is another rule's business (NotEmpty); this guard only judges what IS there.
        if (string.IsNullOrWhiteSpace(code)) return true;

        var set = await LoadAsync(cancellationToken).ConfigureAwait(false);

        // An empty catalogue means the feed has never run; refusing every document then would take
        // the whole ERP down over reference data, so an unpopulated catalogue accepts anything.
        if (set.Count == 0) return true;

        return set.Contains(Currency.Normalize(code));
    }

    private async Task<IReadOnlySet<string>> LoadAsync(CancellationToken cancellationToken)
    {
        if (_usable is not null) return _usable;

        var rows = await _catalog.ListAllAsync(cancellationToken).ConfigureAwait(false);
        _usable = rows
            .Where(c => c.IsActive)
            .Select(c => c.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _usable;
    }
}

public static class CurrencyRuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeAKnownCurrency<T>(
        this IRuleBuilder<T, string> rule,
        IKnownCurrencyGuard guard)
        => rule
            .MustAsync((code, ct) => guard.IsUsableAsync(code, ct))
            .WithMessage("Validation.CurrencyNotInCatalog");
}
