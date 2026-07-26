using CoreAlign.Application.Feedback.Notifications;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.API.HostedServices;

/// <summary>
/// Seeds the three GLOBAL feedback notification templates on every start.
/// </summary>
/// <remarks>
/// The dispatcher swallows a missing template per channel and creates no message at all, so a
/// notification whose template was never seeded fails silently and forever. The system template
/// seeder only runs behind the demo-data flag, which is off in production — hence a dedicated
/// always-run seeder scoped to just these keys, rather than switching on the other dormant ones.
/// </remarks>
public sealed class FeedbackNotificationTemplateSeeder : BackgroundService
{
    private static readonly string[] Locales = ["tr", "en", "ar", "de", "ru"];

    private static readonly NotificationChannel[] Channels =
    [
        NotificationChannel.Email,
        NotificationChannel.Sms,
        NotificationChannel.Push,
        NotificationChannel.InApp,
        NotificationChannel.WhatsApp,
    ];

    private static readonly FeedbackTemplateSpec[] Specs =
    [
        new(
            FeedbackTemplateKeys.Created,
            new Dictionary<string, string>
            {
                ["tr"] = "Yeni kayıt: {{title}}",
                ["en"] = "New report: {{title}}",
                ["ar"] = "بلاغ جديد: {{title}}",
                ["de"] = "Neue Meldung: {{title}}",
                ["ru"] = "Новое обращение: {{title}}",
            },
            new Dictionary<string, string>
            {
                ["tr"] = "{{type}} türünde yeni bir kayıt açıldı. Öncelik: {{priority}}. Modül: {{module}}.",
                ["en"] = "A new {{type}} report was opened. Priority: {{priority}}. Module: {{module}}.",
                ["ar"] = "تم فتح بلاغ جديد من نوع {{type}}. الأولوية: {{priority}}. الوحدة: {{module}}.",
                ["de"] = "Eine neue Meldung vom Typ {{type}} wurde erstellt. Priorität: {{priority}}. Modul: {{module}}.",
                ["ru"] = "Создано новое обращение типа {{type}}. Приоритет: {{priority}}. Модуль: {{module}}.",
            }),
        new(
            FeedbackTemplateKeys.StatusChanged,
            new Dictionary<string, string>
            {
                ["tr"] = "Kaydınızın durumu değişti: {{title}}",
                ["en"] = "Your report changed status: {{title}}",
                ["ar"] = "تغيرت حالة بلاغك: {{title}}",
                ["de"] = "Status Ihrer Meldung geändert: {{title}}",
                ["ru"] = "Статус вашего обращения изменён: {{title}}",
            },
            new Dictionary<string, string>
            {
                ["tr"] = "Kaydınızın yeni durumu: {{status}}.",
                ["en"] = "Your report is now: {{status}}.",
                ["ar"] = "الحالة الجديدة لبلاغك: {{status}}.",
                ["de"] = "Der neue Status Ihrer Meldung: {{status}}.",
                ["ru"] = "Новый статус вашего обращения: {{status}}.",
            }),
        new(
            FeedbackTemplateKeys.CommentAdded,
            new Dictionary<string, string>
            {
                ["tr"] = "Kaydınıza yeni yorum: {{title}}",
                ["en"] = "New comment on your report: {{title}}",
                ["ar"] = "تعليق جديد على بلاغك: {{title}}",
                ["de"] = "Neuer Kommentar zu Ihrer Meldung: {{title}}",
                ["ru"] = "Новый комментарий к вашему обращению: {{title}}",
            },
            new Dictionary<string, string>
            {
                ["tr"] = "{{authorName}} kaydınıza bir yorum yazdı.",
                ["en"] = "{{authorName}} commented on your report.",
                ["ar"] = "{{authorName}} علّق على بلاغك.",
                ["de"] = "{{authorName}} hat Ihre Meldung kommentiert.",
                ["ru"] = "{{authorName}} оставил комментарий к вашему обращению.",
            }),
    ];

    public static IReadOnlyList<string> SeededKeys { get; } = Specs.Select(s => s.Key).ToList();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeedbackNotificationTemplateSeeder> _logger;

    public FeedbackNotificationTemplateSeeder(
        IServiceScopeFactory scopeFactory,
        ILogger<FeedbackNotificationTemplateSeeder> logger)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Feedback notification template seeding failed.");
        }
    }

    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<INotificationTemplateRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var spec in Specs)
        {
            foreach (var locale in Locales)
            {
                foreach (var channel in Channels)
                {
                    if (await repo.ExistsAsync(null, spec.Key, channel, locale, ct)) continue;

                    var subject = spec.Subjects.TryGetValue(locale, out var s) ? s : spec.Subjects["en"];
                    var body = spec.Bodies.TryGetValue(locale, out var b) ? b : spec.Bodies["en"];
                    await repo.AddAsync(
                        new NotificationTemplate(null, spec.Key, channel, locale, subject, body),
                        ct);
                    anyChange = true;
                }
            }
        }

        if (anyChange) await uow.SaveChangesAsync(ct);
    }

    private sealed record FeedbackTemplateSpec(
        string Key,
        IReadOnlyDictionary<string, string> Subjects,
        IReadOnlyDictionary<string, string> Bodies);
}
