using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.API.HostedServices;

public static class NotificationTemplateSeeder
{
    private static readonly string[] Locales = new[] { "tr", "en", "ar", "de", "ru" };

    private static readonly TemplateSpec[] Specs = new[]
    {
        new TemplateSpec("Warranty.Activated", "Warranty",
            new Dictionary<string, string>
            {
                ["tr"] = "Garantiniz aktif edildi",
                ["en"] = "Your warranty is active",
                ["ar"] = "تم تفعيل الضمان الخاص بك",
                ["de"] = "Ihre Garantie ist aktiv",
                ["ru"] = "Ваша гарантия активирована"
            },
            new Dictionary<string, string>
            {
                ["tr"] = "Sayın müşterimiz, {{warrantyNumber}} numaralı garantiniz {{startDate}} tarihinde başlatıldı. Bitiş: {{endDate}}.",
                ["en"] = "Dear customer, your warranty {{warrantyNumber}} started on {{startDate}}. End date: {{endDate}}.",
                ["ar"] = "عزيزي العميل، تم تفعيل ضمانك {{warrantyNumber}} في {{startDate}}. تاريخ الانتهاء: {{endDate}}.",
                ["de"] = "Sehr geehrter Kunde, Ihre Garantie {{warrantyNumber}} wurde am {{startDate}} aktiviert. Enddatum: {{endDate}}.",
                ["ru"] = "Уважаемый клиент, ваша гарантия {{warrantyNumber}} активирована {{startDate}}. Дата окончания: {{endDate}}."
            }),
        new TemplateSpec("Warranty.Expiring", "Warranty",
            new Dictionary<string, string>
            {
                ["tr"] = "Garantinizin bitimine az kaldı",
                ["en"] = "Your warranty is expiring soon",
                ["ar"] = "ضمانك على وشك الانتهاء",
                ["de"] = "Ihre Garantie läuft bald ab",
                ["ru"] = "Срок гарантии скоро истекает"
            },
            new Dictionary<string, string>
            {
                ["tr"] = "Garantinizin bitimine {{daysRemaining}} gün kaldı. Sona erme: {{endDate}}.",
                ["en"] = "{{daysRemaining}} days remaining until warranty expiry on {{endDate}}.",
                ["ar"] = "متبقي {{daysRemaining}} يومًا حتى انتهاء ضمانك في {{endDate}}.",
                ["de"] = "Noch {{daysRemaining}} Tage bis zum Ablauf Ihrer Garantie am {{endDate}}.",
                ["ru"] = "Осталось {{daysRemaining}} дней до истечения гарантии {{endDate}}."
            }),
        new TemplateSpec("Payment.Succeeded", "Payment",
            new Dictionary<string, string>
            {
                ["tr"] = "Ödemeniz alındı",
                ["en"] = "Payment received",
                ["ar"] = "تم استلام الدفعة",
                ["de"] = "Zahlung erhalten",
                ["ru"] = "Платёж получен"
            },
            new Dictionary<string, string>
            {
                ["tr"] = "{{paymentNumber}} numaralı {{amount}} {{currency}} tutarındaki ödemeniz alındı.",
                ["en"] = "Payment {{paymentNumber}} of {{amount}} {{currency}} received.",
                ["ar"] = "تم استلام دفعتك رقم {{paymentNumber}} بمبلغ {{amount}} {{currency}}.",
                ["de"] = "Zahlung {{paymentNumber}} über {{amount}} {{currency}} erhalten.",
                ["ru"] = "Платёж {{paymentNumber}} на сумму {{amount}} {{currency}} получен."
            }),
        new TemplateSpec("Payment.Failed", "Payment",
            new Dictionary<string, string>
            {
                ["tr"] = "Ödeme başarısız",
                ["en"] = "Payment failed",
                ["ar"] = "فشل الدفع",
                ["de"] = "Zahlung fehlgeschlagen",
                ["ru"] = "Платёж не выполнен"
            },
            new Dictionary<string, string>
            {
                ["tr"] = "{{paymentNumber}} numaralı ödemeniz alınamadı. Tutar: {{amount}}.",
                ["en"] = "Payment {{paymentNumber}} of {{amount}} could not be processed.",
                ["ar"] = "تعذر معالجة الدفعة {{paymentNumber}} بمبلغ {{amount}}.",
                ["de"] = "Zahlung {{paymentNumber}} über {{amount}} konnte nicht bearbeitet werden.",
                ["ru"] = "Платёж {{paymentNumber}} на сумму {{amount}} не выполнен."
            }),
        new TemplateSpec("Installation.Accepted", "Installation",
            new Dictionary<string, string>
            {
                ["tr"] = "Kurulum kabul edildi",
                ["en"] = "Installation accepted",
                ["ar"] = "تم قبول التركيب",
                ["de"] = "Installation akzeptiert",
                ["ru"] = "Установка принята"
            },
            new Dictionary<string, string>
            {
                ["tr"] = "Kurulumunuz {{acceptedAt}} tarihinde başarıyla kabul edildi.",
                ["en"] = "Your installation was accepted on {{acceptedAt}}.",
                ["ar"] = "تم قبول التركيب الخاص بك بتاريخ {{acceptedAt}}.",
                ["de"] = "Ihre Installation wurde am {{acceptedAt}} akzeptiert.",
                ["ru"] = "Ваша установка принята {{acceptedAt}}."
            }),
        new TemplateSpec("Mrp.SuggestionsCreated", "Mrp",
            new Dictionary<string, string>
            {
                ["tr"] = "Yeni MRP önerileri oluşturuldu",
                ["en"] = "New MRP suggestions",
                ["ar"] = "اقتراحات MRP جديدة",
                ["de"] = "Neue MRP-Vorschläge",
                ["ru"] = "Новые MRP-предложения"
            },
            new Dictionary<string, string>
            {
                ["tr"] = "{{asOfDate}} tarihi için {{lineCount}} yeni stok önerisi oluşturuldu.",
                ["en"] = "{{lineCount}} new stock suggestions created for {{asOfDate}}.",
                ["ar"] = "تم إنشاء {{lineCount}} اقتراح مخزون جديد لـ {{asOfDate}}.",
                ["de"] = "{{lineCount}} neue Lagervorschläge für {{asOfDate}} erstellt.",
                ["ru"] = "Создано {{lineCount}} новых предложений для {{asOfDate}}."
            }),
        new TemplateSpec("ServiceTicket.Resolved", "ServiceTicket",
            new Dictionary<string, string>
            {
                ["tr"] = "Talebiniz çözüldü",
                ["en"] = "Your ticket is resolved",
                ["ar"] = "تم حل التذكرة",
                ["de"] = "Ihr Ticket wurde gelöst",
                ["ru"] = "Ваш запрос решён"
            },
            new Dictionary<string, string>
            {
                ["tr"] = "Servis talebiniz çözüldü. Ücret: {{chargeableAmount}}.",
                ["en"] = "Your service ticket has been resolved. Charge: {{chargeableAmount}}.",
                ["ar"] = "تم حل تذكرة الخدمة. الرسوم: {{chargeableAmount}}.",
                ["de"] = "Ihr Service-Ticket wurde gelöst. Gebühr: {{chargeableAmount}}.",
                ["ru"] = "Ваш сервисный запрос решён. Сумма: {{chargeableAmount}}."
            }),
    };

    private static readonly NotificationChannel[] SeedChannels = new[]
    {
        NotificationChannel.Email,
        NotificationChannel.Sms,
        NotificationChannel.Push,
        NotificationChannel.InApp,
        NotificationChannel.WhatsApp
    };

    public static async Task SeedSystemTemplatesAsync(IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<INotificationTemplateRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var spec in Specs)
        {
            foreach (var locale in Locales)
            {
                foreach (var channel in SeedChannels)
                {
                    if (await repo.ExistsAsync(null, spec.Key, channel, locale, ct)) continue;

                    var subject = spec.Subjects.TryGetValue(locale, out var s) ? s : spec.Subjects["en"];
                    var body = spec.Bodies.TryGetValue(locale, out var b) ? b : spec.Bodies["en"];
                    var template = new NotificationTemplate(null, spec.Key, channel, locale, subject, body);
                    await repo.AddAsync(template, ct);
                    anyChange = true;
                }
            }
        }

        if (anyChange) await uow.SaveChangesAsync(ct);
    }

    private sealed record TemplateSpec(
        string Key,
        string CategoryKey,
        IReadOnlyDictionary<string, string> Subjects,
        IReadOnlyDictionary<string, string> Bodies);
}
