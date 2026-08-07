using CoreAlign.Application.Billing.Expiry;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.API.HostedServices;

/// <summary>
/// Seeds the GLOBAL module-expiry notification template on every start.
/// </summary>
/// <remarks>
/// The dispatcher swallows a missing template per channel and creates no message at all, so a
/// reminder job whose template was never seeded looks perfectly healthy in the logs while sending
/// nothing. The system template seeder only runs behind the demo-data flag, which is off in
/// production — hence a dedicated always-run seeder scoped to just this key.
/// </remarks>
public sealed class ModuleExpiryNotificationTemplateSeeder : BackgroundService
{
    private static readonly string[] Locales = ["tr", "en", "ar", "de", "ru"];

    private static readonly NotificationChannel[] Channels =
    [
        NotificationChannel.Email,
        NotificationChannel.InApp,
        NotificationChannel.Push,
    ];

    /// <summary>Every key this seeder covers — the guard test compares it against what the job dispatches.</summary>
    public static IReadOnlyList<string> SeededKeys => [ModuleExpiryTemplateKeys.Expiring];

    private static readonly Dictionary<string, string> Subjects = new()
    {
        ["tr"] = "{{moduleName}} modülünüzün süresi doluyor",
        ["en"] = "Your {{moduleName}} module is expiring",
        ["ar"] = "وحدة {{moduleName}} على وشك الانتهاء",
        ["de"] = "Ihr Modul {{moduleName}} läuft ab",
        ["ru"] = "Срок действия модуля {{moduleName}} истекает",
    };

    // WHY: the payload carries the THRESHOLD band, not the exact days left — an exact count would
    // change daily and defeat the dispatcher's payload-hash dedup, so the wording says "less than".
    private static readonly Dictionary<string, string> Bodies = new()
    {
        ["tr"] = "{{moduleName}} modülünüzün kullanım süresi {{expiresOn}} tarihinde doluyor ({{thresholdDays}} günden az kaldı). Kesintisiz devam etmek için Abonelik > Modül Mağazası üzerinden uzatabilirsiniz.",
        ["en"] = "Your {{moduleName}} module expires on {{expiresOn}} (less than {{thresholdDays}} days left). Extend it from Subscription > Module Store to avoid interruption.",
        ["ar"] = "تنتهي صلاحية وحدة {{moduleName}} في {{expiresOn}} (بقي أقل من {{thresholdDays}} يومًا). يمكنك التمديد من الاشتراك > متجر الوحدات.",
        ["de"] = "Ihr Modul {{moduleName}} läuft am {{expiresOn}} ab (weniger als {{thresholdDays}} Tage verbleibend). Verlängern Sie es unter Abonnement > Modul-Store.",
        ["ru"] = "Модуль {{moduleName}} истекает {{expiresOn}} (осталось менее {{thresholdDays}} дн.). Продлите его в разделе Подписка > Магазин модулей.",
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ModuleExpiryNotificationTemplateSeeder> _logger;

    public ModuleExpiryNotificationTemplateSeeder(
        IServiceScopeFactory scopeFactory,
        ILogger<ModuleExpiryNotificationTemplateSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await SeedAsync(scope.ServiceProvider, stoppingToken);
            _logger.LogInformation("Module expiry notification template seed checked.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Module expiry notification template seeding failed; expiry reminders will send nothing.");
        }
    }

    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<INotificationTemplateRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var locale in Locales)
        {
            foreach (var channel in Channels)
            {
                var subject = Subjects.TryGetValue(locale, out var s) ? s : Subjects["en"];
                var body = Bodies.TryGetValue(locale, out var b) ? b : Bodies["en"];

                var existing = await repo.GetByKeyLocaleAsync(null, ModuleExpiryTemplateKeys.Expiring, channel, locale, ct);
                if (existing is null)
                {
                    await repo.AddAsync(
                        new NotificationTemplate(null, ModuleExpiryTemplateKeys.Expiring, channel, locale, subject, body),
                        ct);
                    anyChange = true;
                    continue;
                }

                // Insert-only seeding would freeze a wrong wording forever; this template is ours to own.
                if (existing.Subject == subject && existing.BodyTemplate == body) continue;
                existing.Update(subject, body);
                anyChange = true;
            }
        }

        if (anyChange) await uow.SaveChangesAsync(ct);
    }
}
