using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.API.HostedServices;

/// <summary>
/// The purchasable module catalog. This is SYSTEM reference data, not demo data — it must exist in
/// every environment, so it is seeded by an always-run hosted service rather than from
/// <see cref="DemoDataSeeder"/> (which is hard-off in Production and would leave the store empty).
/// </summary>
public static class ModuleCatalogSeed
{
    public readonly record struct ModuleSpec(
        string Code,
        string Name,
        string Description,
        string Category,
        string IconKey,
        int SortOrder,
        bool IsCore);

    public readonly record struct PlanSpec(string Code, string DisplayLabel, int DurationDays, decimal Price);

    // WHY these groupings: one sellable module per functional AREA, not per screen. "Sales" covers
    // quotes/orders/invoices/returns because a tenant that buys one of those needs all of them;
    // splitting them would produce a catalogue nobody can reason about. Core modules carry no plan.
    public static readonly ModuleSpec[] Modules =
    {
        new("Dashboard", "Panel", "Genel bakış, KPI kartları ve grafikler.", "Temel", "layout-dashboard", 0, true),
        new("Billing", "Abonelik & Faturalandırma", "Modül satın alma, siparişler ve abonelik yönetimi.", "Temel", "credit-card", 1, true),
        new("Settings", "Ayarlar", "Firma profili, kullanıcılar, yetkiler ve sistem ayarları.", "Temel", "settings", 2, true),

        new("Customers", "Müşteriler", "Müşteri kartları, cari hesap, kredi limiti ve ekstre.", "Satış & CRM", "users", 10, false),
        new("Sales", "Satış", "Teklif, sipariş, fatura, tahsilat ve iade süreçleri.", "Satış & CRM", "shopping-cart", 11, false),

        new("Projects", "Projeler", "Proje kartları, görevler ve proje bazlı takip.", "Projeler", "folder-kanban", 20, false),
        new("GlassEnclosure", "Cam Mekan", "3B cam tasarımcısı, otomatik BOM, kesim ve iş emri.", "Projeler", "square-stack", 21, false),

        new("Vendors", "Tedarikçiler", "Tedarikçi kartları, cari hesap ve satıcı ödemeleri.", "Satın Alma", "truck", 30, false),
        new("Purchasing", "Satın Alma", "Talep, sipariş, mal kabul, 3'lü eşleştirme ve borç yaşlandırma.", "Satın Alma", "package", 31, false),

        new("Products", "Ürünler", "Ürün kataloğu, varyant, fiyat listesi ve birim yönetimi.", "Envanter", "box", 40, false),
        new("Inventory", "Stok Yönetimi", "Depo, stok hareketleri, sayım, seri takibi ve maliyetleme.", "Envanter", "warehouse", 41, false),

        new("Mrp", "MRP & Planlama", "Malzeme ihtiyaç planlama, tahmin, kapasite ve dağıtım.", "Üretim", "factory", 50, false),
        new("Manufacturing", "Üretim", "Üretim rotaları, iş emirleri ve üretim kayıtları.", "Üretim", "cog", 51, false),

        new("Payroll", "Bordro & İK", "Personel kartları, Türk mevzuatına uygun bordro ve tahakkuk.", "İnsan Kaynakları", "badge-dollar-sign", 60, false),

        new("Warranty", "Garanti", "Garanti sözleşmeleri, kapsam ve servis talepleri.", "Satış Sonrası", "shield-check", 70, false),
        new("Installation", "Montaj & Keşif", "Saha keşfi, montaj planı, kabul tutanağı ve mobil uygulama.", "Satış Sonrası", "wrench", 71, false),

        new("Accounting", "Muhasebe", "TDHP hesap planı, yevmiye, mizan, bilanço ve dönem kapanışı.", "Finans", "calculator", 80, false),
        new("Treasury", "Kasa & Banka", "Banka hesapları, nakit pozisyonu ve döviz kuru yönetimi.", "Finans", "landmark", 81, false),

        new("Reports", "Raporlar & Analitik", "Hazır raporlar, mükerrer kayıt tespiti ve belge numarası boşlukları.", "Analitik", "bar-chart", 90, false),

        new("EInvoice", "e-Fatura & e-İrsaliye", "GİB uyumlu e-belge gönderimi, gelen fatura ve entegratör bağlantısı.", "Entegrasyon", "file-text", 100, false),
        new("AiHelper", "AI Asistan", "Uygulama içi yapay zekâ yardımcısı ve veri analizi.", "Entegrasyon", "sparkles", 101, false),

        new("B2BPortal", "Bayi Portalı", "Bayilerin kendi siparişlerini girdiği B2B yüzeyi.", "Portallar", "store", 110, false),
        new("CustomerPortal", "Müşteri Portalı", "Müşterilerin sipariş ve faturalarını gördüğü self-servis yüzey.", "Portallar", "user-round", 111, false),
    };

    public static readonly PlanSpec[] DefaultPlans =
    {
        new("Monthly", "Aylık", 30, 99m),
        new("Yearly", "Yıllık", 365, 999m),
    };

    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct)
    {
        var modules = sp.GetRequiredService<IModuleRepository>();
        var plans = sp.GetRequiredService<IModulePricePlanRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var changed = false;

        foreach (var spec in Modules)
        {
            var existing = await modules.GetByCodeAsync(spec.Code, ct);
            if (existing is null)
            {
                await modules.AddAsync(
                    new Module(spec.Code, spec.Name, spec.Description, spec.Category, spec.IconKey, spec.SortOrder, isActive: true, isCore: spec.IsCore),
                    ct);
                changed = true;
                continue;
            }
            // Presentation fields are catalogue copy, so a rename or a re-grouping should land on
            // the next boot. Code and Id stay put — grants reference them.
            if (existing.Name != spec.Name
                || existing.Description != spec.Description
                || existing.Category != spec.Category
                || existing.IconKey != spec.IconKey
                || existing.SortOrder != spec.SortOrder
                || existing.IsCore != spec.IsCore)
            {
                existing.Update(spec.Name, spec.Description, spec.Category, spec.IconKey, spec.SortOrder, isActive: true, isCore: spec.IsCore);
                modules.Update(existing);
                changed = true;
            }
        }
        if (changed) await uow.SaveChangesAsync(ct);

        var all = await modules.ListAsync(activeOnly: false, ct);
        changed = false;
        foreach (var module in all.Where(m => !m.IsCore))
        {
            var existingPlans = await plans.ListByModuleAsync(module.Id, activeOnly: false, ct);
            var byCode = existingPlans.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < DefaultPlans.Length; i++)
            {
                var spec = DefaultPlans[i];
                if (byCode.ContainsKey(spec.Code)) continue;
                await plans.AddAsync(
                    new ModulePricePlan(module.Id, spec.Code, spec.DisplayLabel, spec.DurationDays, spec.Price, "TRY", isActive: true, sortOrder: i),
                    ct);
                changed = true;
            }
        }
        if (changed) await uow.SaveChangesAsync(ct);
    }
}
