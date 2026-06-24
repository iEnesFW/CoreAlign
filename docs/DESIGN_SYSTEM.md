# CoreAlign — Design System

> Tek doğruluk kaynağı. Renk, token, primitif ve sayfa-pattern kararları burada tanımlıdır. Yeni UI yazarken bu dosya bağlayıcıdır (CLAUDE.md §2.2 ile birlikte).

## 1. Marka Kimliği

- **Ürün adı:** **CoreAlign** (her yüzeyde tutarlı). `shared/ui/Logo` tek marka işaretidir; ham metin/ikon ile marka çizme.
- **Birincil renk:** **Indigo `#6366f1`** (`primary-500`). Uygulamanın fiili görsel kimliği; aksiyon butonları `primary-600`, hover `primary-700`.
- **Aksan:** **Cyan `#22D3EE`** (`accent-400`).

Geçmişte üç çakışan "primary" vardı (`#3b82f6`, `#0EA5E9`, indigo). Tümü **indigo**'da uzlaştırıldı: `index.css`, PWA manifest (`theme_color`), `TenantThemeProvider` default.

## 2. Token Mimarisi (Tailwind v4 `@theme`)

`src/index.css` içindeki `@theme` bloğu **tek kaynaktır**. Her semantik renk 50→950 ramp'i olarak tanımlıdır ve Tailwind utility'si üretir:

| Rol     | Namespace   | Taban palet              | Anlam                                         |
| ------- | ----------- | ------------------------ | --------------------------------------------- |
| Primary | `primary-*` | indigo                   | Marka, birincil aksiyon, aktif durum, linkler |
| Accent  | `accent-*`  | cyan                     | İkincil vurgu, dekoratif gradient             |
| Success | `success-*` | emerald                  | Olumlu durum, onay, tamamlandı                |
| Warning | `warning-*` | amber                    | Uyarı, bekleyen, dikkat                       |
| Danger  | `danger-*`  | red                      | Hata, sil, reddet, negatif                    |
| Info    | `info-*`    | sky                      | Bilgi, nötr-pozitif vurgu                     |
| Neutral | `slate-*`   | slate (Tailwind default) | Metin, yüzey, kenarlık, arka plan             |

**Kullanım:** `bg-primary-600`, `text-success-600`, `border-danger-500`, `ring-primary-500/40` — hepsi gerçek Tailwind utility'leridir.

**Neutral neden slate?** Slate zaten tutarlı (codebase'de ~11k kullanım). Yeniden isimlendirmenin değeri yok; slate = nötr ölçek olarak korunur.

## 3. White-Label (Runtime Tema)

`@theme` değerleri `:root`'a CSS değişkeni olarak düşer; bu yüzden runtime'da override edilebilir. `TenantThemeProvider`:

- Tenant'ın `primaryColor`'ı **özel** olduğunda (`brandName` set), `--color-primary-{400,500,600,700}` tonlarını `color-mix()` ile türetip override eder → `bg-primary-600` kullanan **her** component otomatik tenant rengine boyanır.
- Default tenant'ta override kaldırılır; `@theme` indigo ramp'i geçerli olur.

**Sonuç:** White-label'ın çalışması için componentler `primary-*` (ham `indigo-*`/`blue-*` değil) kullanmalıdır. Migrasyon haritası §6'da.

## 4. Dark Mode

- Strateji: **class-based** (`.dark` `documentElement`'te). `ThemeProvider` toggle + localStorage + `prefers-color-scheme`.
- Kural: **renkli her Tailwind class'ının `dark:` karşılığı olur.** İstisna bilinçli ve nadir.
- Eşleme: nötr 50↔900 kabul; semantik tonlarda 2–3 shade delta (örn. `text-primary-600 dark:text-primary-300`).

## 5. Çekirdek Primitifler (`shared/ui`)

| Primitif                                                                | Durum                                                                                                                                    |
| ----------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `Button`                                                                | ✅ Token-güdümlü Tailwind. `variant: primary\|secondary\|outline\|ghost\|danger`, `size: sm\|md\|lg`, `isLoading`. `focus-visible` ring. |
| `Input`                                                                 | ✅ Token-güdümlü Tailwind. `label/error/leftIcon`, dark placeholder, `aria-invalid`, `focus-visible`.                                    |
| `Logo`                                                                  | ✅ Gradient `primary-*` token'ından; tek marka işareti.                                                                                  |
| `Select / Textarea / Checkbox / Tooltip`                                | ⏳ Eksik — Faz 2'de eklenecek (şu an ham HTML kullanılıyor).                                                                             |
| Kart ailesi (`StatCard / StatStrip / InlineDetailCard / DataTableCard`) | ⏳ 4 örtüşen tip; 2 kanonik primitife indirgenecek (Faz 2).                                                                              |

**Kural:** API tutarlılığı — etkileşimli her component `size: 'sm'|'md'|'lg'` açar; tablo-benzeri için `density`.

## 6. Renk Migrasyon Haritası (kademeli)

Ham palet → semantik token. Codemod ile uygulanır; bileşik class'lar (`hover:`, `dark:`) elle gözden geçirilir.

| Ham (eski)                                         | Semantik (yeni) |
| -------------------------------------------------- | --------------- |
| `*-indigo-*` (primary anlamında)                   | `*-primary-*`   |
| `bg-blue-600`, `text-blue-600` (primary kirliliği) | `*-primary-*`   |
| `*-emerald-*` (durum)                              | `*-success-*`   |
| `*-amber-*` / `*-yellow-*` (durum)                 | `*-warning-*`   |
| `*-red-*` **ve** `*-rose-*` (danger — birleştir)   | `*-danger-*`    |
| `*-sky-*` (bilgi; cam-kabin domain'i hariç)        | `*-info-*`      |
| `*-slate-*`                                        | değişmez (nötr) |

**Durum rengi kuralı (tek anlam):** success=emerald, warning=amber, **danger=red** (rose terk edilir), info=sky. Aynı anlama iki aile yasak.

## 7. Sayfa Pattern Standardı

- **Liste sayfası:** `ListPageTemplate` (dış padding `p-4 sm:p-6` + dikey ritim) → `header={<PageHeader … actions={<Button/>} />}` → `toolbar` (Input/Select) → `children` (tablo) → `pagination`. Ham `p-4` + ad-hoc `<h1>` yasak.
- **Detay sayfası:** `DetailPageTemplate` → `header={<PageHeader breadcrumb/>}` → içerik. Inline `<table>` yerine `DataTable`.
- **Form kontrolleri:** ham `<input>/<select>/<textarea>/<input type=checkbox>` yerine `Input`/`Select`/`Textarea`/`Checkbox`/`Label`; aksiyonlar `Button`; diyaloglar `Modal`.
- Durum rozetleri için `Badge` + `shared/lib/statusStyles` (`statusToneClass[tone]`). Domain mapper feature katmanında.
- **Exemplar'lar (kopyala-uyarla):** `src/pages/settings/SettingsPage.tsx` (config/tab), `src/pages/vendors/VendorsPage.tsx` (liste + toolbar + Modal form).
- God component yasağı: sayfa > ~300 satır ise alt-componentlere böl.

### 7.1 Kalan migrasyon kapsamı (admin dashboard, ~30 sayfa)

Migrate edilecek (ad-hoc header → `ListPageTemplate`/`DetailPageTemplate` + `PageHeader`): `accounting/*` (6), `inventory/*`, `mrp/*` (3), `reports/*` (4), `purchasing/*` (PurchaseOrders/VendorBills/GoodsReceipts/ThreeWayMatch/PayablesAging), `warranty/*` (3), `notifications`, `activity`, `admin/*` (4), `Platform/Tenants/*` (2), `orderTemplates/*`, `settings/{DiscountRules,ExchangeRates,Imports,PriceLists,TaxRules}`, `customers/CustomerDetailPage`, `vendors/VendorDetailPage`, `feedback`.

**Meşru istisna (PageHeader kullanmaz):** auth (login/register/forgot/reset/verify), `legal/*`, `public/*` (landing kendi tasarımı — Faz 4), `customer-portal/*` (ayrı shell — kendi başlık deseni), `glass-enclosure/GlassProjectDesignerPage` (tam-ekran 3D), print view'lar, `*Section`/`*Form` parçaları.

## 8. Erişilebilirlik (a11y)

- İkon-only buton → `aria-label` zorunlu.
- Renk-tek durum sinyali yasak → ikon/metin ile destekle.
- `focus-visible:ring-2 focus-visible:ring-primary-500` etkileşimli her öğede.
- Modal/drawer: `role="dialog"`, focus-trap, Esc.

## 9. Yol Haritası

- **Faz 1 (TAMAM):** `@theme` token sistemi; marka uzlaşması (indigo); `Button`/`Input` → token Tailwind; `Logo`/Sidebar/Footer marka düzeltmesi; ölü CSS temizliği; bu doküman.
- **Faz 2a (TAMAM):** Kanonik primitifler eklendi — `Label`, `Select`, `Textarea`, `Checkbox`, `Card`(+Header/Title/Body/Footer); `Badge` token-güdümlü; paylaşılan `fieldClasses`; `Input` bunları kullanacak şekilde refactor.
- **Faz 2b (TAMAM):** `shared/lib/statusStyles.ts` — generic `StatusTone` + `statusToneClass` (domain mapper'ları feature katmanında, FSD'ye uygun).
- **Faz 3a (TAMAM):** Ham renk → semantik token codemod'u **uygulandı**: 317 dosya, 4403 değişim (indigo/blue→primary, emerald/green→success, amber/yellow/orange→warning, rose/red→danger, sky→info). Kayıpsız; rose→red ve blue→indigo kasıtlı. glass-enclosure `sky` domain rengi korundu. Build + 206 test yeşil.
- **Faz 2c (kısmen TAMAM):** Kanonik sayfa şablonları (`ListPageTemplate`/`DetailPageTemplate`) + `Tooltip` primitifi eklendi; shell yüzey rengi `#0B0F19` → `@theme` `shell` tokenı (`dark:bg-shell` vb., 13 kullanım). **Kalan:** Sidebar IA gruplaması + section başlığı i18n; CommandPalette gruplama.
- **Faz 2d:** theme `react-refresh` eslint-disable temizliği (ThemeProvider/TenantThemeProvider hook çıkarma).
- **Faz 3b:** `ListPageTemplate` + `DetailPageTemplate`; non-kanonik sayfaları (Vendors/Inventory/Settings/accounting) PageHeader/DataToolbar/DataTable'a geçir; feature status mapper'ları.
- **Faz 3c:** Ham HTML form kontrollerini (`<select>`/`<textarea>`/`<input type=checkbox>`) yeni primitiflere geçir.
- **Faz 4:** landing/marketing (social proof, pricing, hero), login cilası.
- **Guardrail:** `eslint-plugin-jsx-a11y` + ham-renk-class uyarısı (lint); pre-existing react-hooks/any borçları.
