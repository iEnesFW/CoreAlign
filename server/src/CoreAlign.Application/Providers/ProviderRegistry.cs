using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Providers;

public class ProviderRegistry<TProvider> : IProviderRegistry<TProvider> where TProvider : IExternalProvider
{
    private readonly IReadOnlyDictionary<string, TProvider> _byName;
    private readonly IReadOnlyList<TProvider> _all;
    private readonly ITenantProviderConfigResolver _configResolver;

    public ProviderRegistry(IEnumerable<TProvider> providers, ITenantProviderConfigResolver configResolver)
    {
        _all = providers.ToArray();
        _byName = _all.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _configResolver = configResolver;
    }

    public TProvider? Find(string name) =>
        _byName.TryGetValue(name, out var provider) ? provider : default;

    public TProvider Require(string name) =>
        Find(name) ?? throw new ProviderNotFoundException(typeof(TProvider).Name, name);

    public IReadOnlyList<string> Names => _byName.Keys.ToArray();
    public IReadOnlyList<TProvider> All => _all;

    public async Task<TProvider> ResolveForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var category = ResolveCategory();
        var providerName = await _configResolver.GetDefaultProviderNameAsync(tenantId, category, cancellationToken);
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ProviderNotConfiguredException(category, tenantId);
        }
        return Require(providerName);
    }

    public async Task<TProvider?> TryResolveForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var category = ResolveCategory();
        var providerName = await _configResolver.GetDefaultProviderNameAsync(tenantId, category, cancellationToken);
        if (string.IsNullOrWhiteSpace(providerName)) return default;
        return Find(providerName);
    }

    private static CoreAlign.Domain.Enums.ProviderCategory ResolveCategory()
    {
        var typeName = typeof(TProvider).Name;
        return typeName switch
        {
            "IEFaturaProvider" => CoreAlign.Domain.Enums.ProviderCategory.EFatura,
            "IPaymentProvider" => CoreAlign.Domain.Enums.ProviderCategory.Payment,
            "ILaserMeterAdapter" => CoreAlign.Domain.Enums.ProviderCategory.LaserMeter,
            "ILabelPrinter" => CoreAlign.Domain.Enums.ProviderCategory.LabelPrinter,
            "ICncExporter" => CoreAlign.Domain.Enums.ProviderCategory.CncExport,
            "ICadImporter" => CoreAlign.Domain.Enums.ProviderCategory.CadImport,
            "IFreightTrackingProvider" => CoreAlign.Domain.Enums.ProviderCategory.Freight,
            "IBankReconciliationProvider" => CoreAlign.Domain.Enums.ProviderCategory.BankReconciliation,
            "ICalendarProvider" => CoreAlign.Domain.Enums.ProviderCategory.Calendar,
            "IEmailProvider" => CoreAlign.Domain.Enums.ProviderCategory.Email,
            "ISmsProvider" => CoreAlign.Domain.Enums.ProviderCategory.Sms,
            "IPushNotificationProvider" => CoreAlign.Domain.Enums.ProviderCategory.Push,
            "IWhatsAppProvider" => CoreAlign.Domain.Enums.ProviderCategory.WhatsApp,
            _ => throw new InvalidOperationException($"Unknown provider category for {typeName}")
        };
    }
}
