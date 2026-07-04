# CoreAlign — Proje Kuralları

> Bu dosya, CoreAlign üzerinde yapılan **her** değişiklikte (kod yazma, refaktör, dosyalama, DB, dokümantasyon) bağlayıcıdır. Aşağıdaki kurallar **non-negotiable** kabul edilir; istisna gerekiyorsa açıkça istemden geçer.
>
> **Bu dosya nasıl okunur (her task başında):** (1) bu çekirdeği oku; (2) aşağıdaki `@import` ile **otomatik yüklenen** `docs/INVARIANTS.md`'yi (bir-daha-aynı-hatayı-yapma defteri) uygula; (3) dokunacağın alanı **"Sistem Haritası & Modül Guardrail İndeksi"**nde (§0.1) bul, oradaki tuzakları ve "önce-oku" dokümanını aç. Bir işi yaparken başka bir modülü bozmamanın yolu bu indekstir.

@docs/INVARIANTS.md

## 0. Proje Kimliği

- **Tip:** Web ERP (multi-tenant, abonelik tabanlı, B2B + B2C portallı).
- **Stack:** React 19 + Vite 7 + TypeScript + Tailwind v4 (web) · Expo 52 + React Native + NativeWind (mobil) · .NET 10 + EF Core + PostgreSQL (backend) · JWT (access + refresh) auth.
- **Mimarî:** Frontend = Feature-Sliced Design (FSD). Backend = Clean Architecture + CQRS (MediatR pipeline) + Outbox.
- **Ölçek (2026-06, gerçek):** ~2200 `.cs`, ~400 Domain entity, ~108 controller, ~113 migration, ~55 backend Application modülü, 3 web SPA + 1 mobil uygulama. Yani "küçük proje" varsayımıyla davranma; her değişiklik mevcut büyük yüzeyle uyumlu olmalı.

### Dört frontend yüzeyi (KARIŞTIRMA — yanlış yüzeye kod = sessiz bozulma)

| Yüzey               | Kök                         | Kim için                                 | i18n yolu                                                   | Notlar                                                                                                                                     |
| ------------------- | --------------------------- | ---------------------------------------- | ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| **Admin SPA**       | `src/`                      | Tenant iç kullanıcıları (ERP'nin tamamı) | `src/app/i18n/locales/{tr,en}.json` (+ar,de,ru fallback=en) | Ana yüzey; FSD; design system burada (`src/index.css` `@theme`)                                                                            |
| **Customer Portal** | `apps/customer-portal/src/` | Tenant'ın son müşterileri (B2C)          | `apps/customer-portal/src/app/locales/{tr,en}.json`         | Kendi FSD ağacı + `shared/` + `widgets/` + `observability/`; kendi client-error reporter'ı                                                 |
| **B2B Portal**      | `apps/b2b/src/`             | Bayi/dealer (B2B)                        | `apps/b2b/src/app/locales/{tr,en}.json`                     | Kendi FSD ağacı; dealer order flow + portal comments                                                                                       |
| **Mobil (saha)**    | `mobile/`                   | Saha montaj/keşif ekibi                  | `mobile/src/shared/i18n/locales/{tr,en}.json`               | **Expo Router + NativeWind** (Tailwind v3, web v4 DEĞİL); offline `expo-sqlite`; `react-native-signature-canvas`; imza/foto/keşif akışları |

> Bir görev "portal", "bayi", "müşteri uygulaması", "mobil/saha" derse **önce hangi kök** olduğunu belirle. Her yüzeyin **kendi** `shared/`, `widgets/`, i18n ve error-reporter altyapısı vardır; admin `src/`'ten import etme, çapraz-yüzey kod paylaşımı yok.

### Backend alt-sistemler (VAR — yenisini icat etme, mevcut olanı kullan)

| Alt-sistem                 | Nerede                                            | Kural                                                                                                                             |
| -------------------------- | ------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| **MediatR pipeline**       | `Application/Common/Behaviors/`                   | Sıra sabit (§3.9). State-mutasyonu `ITransactionalRequest` ile atomik save+commit+audit+outbox.                                   |
| **Outbox**                 | `*Outbox*.cs` + `OutboxDrainBehavior`             | Domain event/yan-etki **outbox'a** yazılır, aynı transaction'da persist; ad-hoc `BackgroundService` ile event fırlatma. ADR-0004. |
| **Hangfire**               | `Infrastructure` (PostgreSQL storage + dashboard) | Zamanlanmış/tekrarlı iş **Hangfire job**'u; yeni `IHostedService` scheduler kurma. ADR-0007.                                      |
| **QuestPDF**               | `Application`/`Infrastructure` Documents          | PDF üretimi QuestPDF ile (fatura/credit-note/sipariş/packing-slip). Başka PDF lib ekleme. ADR-0006.                               |
| **iyzico / E-Fatura**      | `Application/EInvoice`, Payments providers        | TR ödeme = iyzico (ADR-0008); e-fatura entegrasyonu mevcut. Provider soyutlamasını kullan.                                        |
| **Sentry + OpenTelemetry** | `Program.cs`, `Observability`                     | Hata izleme + `/metrics`. `Add*/Use*` çağrılı mı doğrula (§3.4).                                                                  |
| **Error log (DB)**         | `error_logs` + `ExceptionHandlingMiddleware`      | Her 5xx + anlamlı 4xx kalıcı; correlation tek id (§3.4).                                                                          |
| **Auth/Identity**          | `Application/Auth`, `Identity`                    | Derin yatırım — koru (§5).                                                                                                        |

---

## 0.1 Sistem Haritası & Modül Guardrail İndeksi

> **Amaç:** "Bir işi yaparken diğerini bozma." Dokunacağın alanı bul; **Tuzaklar**'ı uygula, **Önce oku**'yu aç. Detay kural numaraları (§x.y) bu dosyadadır; `INV` = `docs/INVARIANTS.md` (otomatik yüklü).

| Alan / Modül                                                   | Kod yeri                                                                                                  | Bozmaması gereken (tuzaklar)                                                                                                                                                                                                           | Önce oku                                                                                               |
| -------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| **Para · Muhasebe · Fatura · Ödeme · Ledger · Tax · Treasury** | `Application/{Accounting,Invoices,Payments,Vendors,Tax,Treasury}`, Domain ledger entity'leri              | `decimal(18,4)` (asla float); **`IHasConcurrencyToken`** (§4.6 — xmin PG18'de devre dışı); GL `Σdebit=Σcredit`; `document_sequences` atomik tüketim; idempotency **durable natural key** (cache değil); FX tek scale (`decimal(18,6)`) | §4.3, §4.6, §16; INV satır 59-70; `docs/modules/error-handling.md`                                     |
| **Stok · Envanter · Ürün · Katalog**                           | `Application/{Inventory,Stock,Products,Catalog}`                                                          | Negatif stok reddi (handler+domain); `StockItem`/`ProductVariant` **`IHasConcurrencyToken`** (§4.6); batch-load (N+1 yok); `(ProductId,LotId)` composite lookup                                                                        | §4.6, §4.11; INV 63-68                                                                                 |
| **Sipariş · Teklif · Satış · Satınalma · İade · Sevkiyat**     | `Application/{Orders,Quotes,Sales,Purchasing,Returns,Shipments}`                                          | FSM guard'ları (her geçiş + reddedilen geçiş test); idempotency **FSM doğal** (§16); `ITransactionalRequest` ile tek transaction                                                                                                       | §3.9, §14; INV 23, 48, 62                                                                              |
| **Cam Mekan (Glass Enclosure)**                                | `Application/GlassEnclosure` (141 dosya), `src/features/glass-enclosure` (157), `src/shared/three-engine` | **Büyük modül**; 3D tasarımcı (three.js r128 sınırı); `IHasConcurrencyToken`; `glass_*_change_logs` partition adayı; `sky` domain rengi korunur                                                                                        | `docs/Cam_Mekan_Modul_Plan.md`; tasarım memory                                                         |
| **MRP**                                                        | `Application/Mrp`, `src/features/mrp`                                                                     | Tranche tasarımı; bilinen açık işler blocker'da                                                                                                                                                                                        | `docs/modules/mrp-analysis.md`, `mrp-tranche-design.md`, `docs/mrp-blockers.md`                        |
| **Bildirim · Provider · Outbox · E-Fatura**                    | `Application/{Notifications,Providers,EInvoice}`, `*Outbox*`                                              | Outbox pattern; tenant-aware SMTP (`TenantProviderConfig`); doküman-forward IDOR-safe; provider creds prod'da sandbox-only                                                                                                             | `docs/modules/notification-service.md`; `.claude/memory/project_corealign_notification_integration.md` |
| **Auth · Identity · Subscription**                             | `Application/{Auth,Identity}`, Domain auth entity'leri                                                    | Mevcut entity'leri silme/parçalama (§5); BCrypt; refresh rotation + reuse-detect; httpOnly cookie (§8.0); `LoginAuditLog`                                                                                                              | §5, §8.0                                                                                               |
| **Multi-tenant (çapraz-kesen)**                                | `Domain/Common/TenantEntity`, `CoreAlignDbContext`                                                        | Yeni veri-entity'si `TenantEntity` türer; her sorgu tenant-filter; `IgnoreQueryFilters` bilinçli + re-scope; cross-tenant izolasyon testi zorunlu                                                                                      | §4.7, §8.1; INV 49, 54, 56                                                                             |
| **Hata yönetimi · Gözlemlenebilirlik (çapraz)**                | `ExceptionHandlingMiddleware`, `error_logs`, `src/shared/errors`                                          | `ApiResponse<T>` zarfı; exception fırlat, status kodlama; 5xx detayı sızdırma; correlation tek id; `Add*/Use*` çağrılı                                                                                                                 | §3.4, §2.4; `docs/modules/error-handling.md`; error memory                                             |
| **Frontend (4 yüzey · design system · i18n)**                  | §0 tablosu                                                                                                | Doğru yüzey; FSD yön kuralı; `t()` + tr&en senkron; dark+responsive; `primary-*` token (raw `indigo-*` değil)                                                                                                                          | §2; `docs/DESIGN_SYSTEM.md`; design memory                                                             |
| **DB · Migration (çapraz)**                                    | `Infrastructure/Persistence`                                                                              | İleri-tarihli Phase ID; idempotent; aynı-tur uygula; tabula-rasa testi; snapshot drift yok; xmin sadece Npgsql                                                                                                                         | §4, §12, §17; INV 28, 59, 63                                                                           |
| **Zamanlanmış işler**                                          | Hangfire                                                                                                  | Yeni scheduler kurma → Hangfire job; ADR-0007                                                                                                                                                                                          | `docs/adr/0007-hangfire-for-scheduled-jobs.md`                                                         |

> Listede olmayan küçük modüller de **aynı disiplinle**: en yakın mevcut modülü örnek al (§13.1), `INVARIANTS.md`'yi tara, tenant + para + hata-yönetimi kurallarını uygula.

---

## 0.2 Geliştirme Ön-Gereksinimleri & Servisler (AI Helper)

> AI Helper (RAG tabanlı yardımcı bot) için gereken yerel/sunucu servisleri. Detay + kurulum: `docs/ai-helper-setup.md`. Mimari: `docs/modules/ai-helper.md`. Yeni PC/sunucuda bu adımlar atlanırsa AI Helper endpoint'i çalışmaz (uygulamanın geri kalanı degrade-güvenli kalmalı).

- **PostgreSQL (mevcut):** AI Helper bilgi tabanı tabloları (`ai_kb_documents`, `ai_kb_chunks`) ana `corealign` DB'sinde yaşar. **pgvector GEREKMEZ** — embedding'ler native `real[]` kolonda, cosine `IKnowledgeRetriever` arkasında hesaplanır (her Postgres'te çalışır, taşınabilir). pgvector-yetenekli sunucuda HNSW'li retriever config ile devreye alınabilir; varsayılan değildir.
- **Ollama (LLM + embedding runtime):** yerel/ücretsiz; sağlayıcı-bağımsız soyutlama (`IAiChatProvider`/`IAiEmbeddingProvider`) arkasında. Varsayılan modeller `qwen2.5:7b` (chat) + `bge-m3` (embedding). Dev: winget (`Ollama.Ollama`); sunucu/Docker: `docker-compose.full.yml` `ollama` servisi. Tek config noktası: `appsettings` `AiHelper` bloğu.
- **Sağlayıcı değiştirilebilir (zorunlu ilke):** LLM'i bulut sağlayıcıya/GPU sunucuya taşımak = `AiHelper:Provider` + Infrastructure DI'daki tek kayıt; iş mantığı/frontend DEĞİŞMEZ. Frontend asla LLM'e doğrudan gitmez, yalnız `/api/v1/ai-helper/ask`.
- **Güvenlik:** API key/secret yalnız sunucuda (user-secrets/env), commit edilmez; login-öncesi endpoint yalnız public KB + rate-limit; login-sonrası retrieval tenant+rol scope'lu (cross-tenant sızıntı yasak).

---

## 1. Sıfır Tolerans Kuralları (Non-Negotiable)

1. **Yorum yasağı.** Hiçbir dosyaya — `.cs`, `.ts`, `.tsx`, `.json`, `.css`, migration, config — yorum satırı yazılmaz. Eski yorumlar görüldüğünde temizlenir. `XML doc` (`///`) yalnız _public API_ sürümleme/refleksiyon gerekiyorsa eklenir; aksi halde eklenmez. Tek istisna: özel bir teknik tuzağı işaretleyen _tek satırlık_ `// WHY:` yorumu — gerekiyorsa, _neden_ açıklanır, _ne_ açıklanmaz.
2. **Lint hatası geçiştirme yasağı.** `// eslint-disable`, `@ts-ignore`, `@ts-expect-error`, `#pragma warning disable`, `SuppressMessage` kullanılmaz. Uyarı çıkarsa kök neden düzeltilir. Yapılamıyorsa istemde gerekçe sunulur, kullanıcıdan onay alınır.
3. **`console.*` yasağı (frontend).** Üretim kodunda `console.log/warn/error` bulunmaz. `src/shared/lib/logger.ts` kullanılır (yoksa kurulur). Debug için bile.
4. **Hardcoded metin yasağı (frontend).** Kullanıcıya görünen _her_ string `t("Namespace.Key")` sarmalında olur. Hata mesajı, toast, label, placeholder, aria-label, başlık, buton — istisnasız. Hem `tr.json` hem `en.json` aynı anda güncellenir, eksik anahtar bırakılmaz.
5. **Güvenlik.** SQLi/XSS/IDOR/CSRF/SSRF açığı oluşturacak hiçbir kalıp yazılmaz. Param binding zorunlu, raw SQL yasak. Auth/role kontrolü atlamak için "geçici" hiçbir çözüm yok.
6. **God class/component yasağı.** 300 satırı aşan component/service üretmeden önce parçalanır. Tek sorumluluk; mantığı view'dan, view'ı routing'ten ayır.

---

## 2. Frontend — Feature-Sliced Design

### 2.1 Klasör Anatomisi

> **Önce yüzeyi seç (§0).** Aşağıdaki anatomi **admin SPA (`src/`)** içindir; `apps/customer-portal`, `apps/b2b` aynı FSD ağacını kendi kökünde tekrarlar; `mobile/` Expo Router kullanır. Yanlış kökte dosya açmak sessiz bozulmadır.

Mevcut yapıya **uyulur**, ihlal edilmez:

```
src/
  app/          → Bootstrap (router, providers, i18n config, global styles)
  pages/        → Route-level component'lar (her route bir klasör)
  widgets/      → Layout parçaları (Navbar, Sidebar, Footer, Layout)
  features/     → Kullanıcı eylemleri (auth, dashboard, ...)
    <feature>/
      api/      → HTTP çağrıları (axios endpoint sarmalları)
      hooks/    → useQuery / useMutation / useStore hookları
      model/    → Zustand store, tipler, schema
      ui/       → Feature'a ait React component'lar
  shared/       → Cross-feature ortak kod
    api/        → apiClient (axios instance, interceptor)
    lib/        → Yardımcılar (i18n, logger, fingerprint, formatters)
    ui/         → Atomik component'lar (Button, Input, Card, Table, ...)
```

Kurallar:

- **Yönelim aşağı doğru:** `shared` → `features` → `widgets` → `pages` → `app`. Yukarı import yasak. Aynı katmanda feature → feature import yasak (paylaşılan şey varsa `shared`'a taşınır).
- **Page-specific componentlar** ilgili sayfa klasörü altında `components/` klasöründe tutulur. Genel olmayan kod `shared/`'a koyulmaz.
- **Tek doğru i18n yolu** (admin SPA): `src/app/i18n/` (`locales/{tr,en,ar,de,ru}.json`, fallback=en). Eski `src/shared/lib/i18n/` duplicate'i **silindi** — yeni kod yalnız `app/i18n` ile çalışır. Portallar/mobil kendi locale yollarını kullanır (§0).

### 2.2 Stil & Tasarım

- **Sadece Tailwind v4.** Inline `style` yalnız dinamik değerler için (örn. CSS var, hesaplanan boyut). Custom CSS dosyası açma — gerekirse `@layer` ile.
- **Responsive zorunlu:** `sm | md | lg | xl | 2xl` breakpoint'lerinin hepsi düşünülür (mobile → widescreen). Mobile-first yazılır.
- **Dark mode zorunlu:** `dark:` varyantı her renkli class ile eşleşir. Yeni component test edilmeden teslim edilmez.
- **Erişilebilirlik:** `aria-*`, semantik HTML (`button`, `nav`, `main`), klavye ile gezilebilirlik.

### 2.3 Veri Erişimi & State

- **API çağrısı doğrudan component'ta yapılmaz.** Her endpoint için:
  1. `features/<x>/api/<x>Api.ts` — axios çağrısı
  2. `features/<x>/hooks/use<X>Queries.ts` — `useQuery`/`useMutation` sarmalı
  3. Component bu hook'u kullanır.
- **Global state:** Zustand store'ları `features/<x>/model/<x>Store.ts`. Cross-feature shared state için `shared/lib/store/`.
- **Server state caching:** TanStack Query (mevcut). `staleTime` ve `queryKey` standardı feature içinde tutulur.

### 2.4 Hata Yönetimi (Frontend)

> Tam rehber + kod kalıpları: `docs/modules/error-handling.md`.

- **`try/catch` doğrudan kullanılmaz.** `shared/lib/safeRequest.ts` sarmalı kullanılır:
  - `safeRequest`: `[data, error]` tuple döner, sessiz yakalar.
  - `safeRequestWithNotify`: Toast gösterir (success/error).
  - `safeBatchRequest` / `safeBatchRequestSettled`: paralel istek sarmalı.
- Eğer bu sarmal henüz yoksa, **ilk ihtiyaçta** kurulur ve standartlaştırılır.
- API hata gövdesi backend **`ApiResponse<T>`** zarfına (madde 3.4: `isSuccess/errors/fieldErrors/statusCode/traceId`) göre parse edilir; `apiClient` interceptor'ı `isSuccess=false`'u normalize edip fırlatır, `safeRequest*` yakalar.
- **Correlation:** `apiClient` her isteğe `X-Correlation-Id` ekler, yanıttakini `getLastCorrelationId()` ile saklar — frontend hatası bu id ile backend isteğine bağlanır.
- **Beklenmeyen hata yakalama (zorunlu).** Her SPA önyüklemede global capture kurar ve kökü `<ErrorBoundary>` ile sarar; üçü de window `error` + `unhandledrejection` + render hatasını yakalayıp `POST /api/v1/client-errors`'a (throttle'lı, **asla throw etmeyen** `reportClientError`) gönderir → `error_logs` (`Source=Frontend`):
  - Root admin SPA (`src/`): `shared/errors/windowHandlers.ts` → `installWindowErrorHandlers()`.
  - Portallar (`apps/customer-portal`, `apps/b2b`): `shared/lib/clientErrorReporter.ts` → `installGlobalErrorReporting()`.
  - Yeni bir SPA eklenmedikçe bu altyapı kuruludur — yenisini kurma. Yeni SPA'da: `main.tsx`'te install + `<ErrorBoundary>` + `apiClient` correlation header'ı.

### 2.5 i18n Disiplini

- Key format: `[Sayfa/Modül].[İçerik]` — örn. `Login.Title`, `Common.Save`, `Validation.Required`.
- Eklenen her anahtar **tr.json** ve **en.json**'da aynı anda var olur, alfabetik gruplanır.
- Çoğul/cinsiyet/format için `t("Key", { count, name })` ile interpolation kullanılır.

### 2.6 Logging

- `src/shared/lib/logger.ts`: dev'de console, prod'da no-op (veya remote sink). `logger.info/warn/error`.
- `apiClient` interceptor'leri `logger` kullanır (console temizlendi). Yeni kodda da `console.*` değil `logger.*`.

### 2.7 TypeScript

- `strict: true` zorunlu. `any` kullanmak için somut gerekçe lazım — alternatif `unknown` + narrowing.
- DTO/Model tipleri **`src/shared/model/`** veya feature'a aitse `features/<x>/model/types.ts`.
- Backend ile şema uyumu — **NSwag/OpenAPI kurulu** (`nswag.json` + `openapi/`). Yeni endpoint eklenirken tip eşleşmesi kontrol edilir; üretilmiş client'ı elle ezme.

---

## 3. Backend — Clean Architecture + CQRS

### 3.1 Katmanlar

```
server/src/
  CoreAlign.API/           → HTTP sınırı (Controller, Middleware, DI host)
  CoreAlign.Application/   → Use-case'ler (Queries/Commands, Validators, DTOs)
  CoreAlign.Domain/        → Entity'ler, Value Object'lar, domain kuralları (saf, bağımlılıksız)
  CoreAlign.Infrastructure/→ DbContext, Migrations, Repository implementasyonu, dış servis adaptörleri
```

Bağımlılık yönü: `API → Application → Domain`, `Infrastructure → Application/Domain`. **Domain hiçbir şeye bağımlı değildir.**

### 3.2 CQRS / Use-Case Düzeni

- Her use-case için: `Application/<Modul>/Queries/<Name>Query.cs` veya `Commands/<Name>Command.cs`.
- MediatR (veya muadil dispatcher) — Handler tek sorumluluk. İçinde HTTP, EF veya cross-cutting yok; sadece use-case akışı.
- **DTO'lar:** Request/Response için ayrı sınıf. Entity dışarı sızdırılmaz.
- **Validation:** FluentValidation tercih edilir (`<Name>Validator.cs`). Behavior pipeline ile otomatik tetiklenir.

### 3.3 Controller Kuralları

- Controller **sadece** request bağlama + dispatcher çağrısı + response döndürme yapar. İş mantığı yasak.
- Endpoint başına ≤ 10 satır gövde. Aksi halde Application'a indirilir.
- `[Authorize]` / `[AllowAnonymous]` her endpoint'te **bilinçli** olarak işaretlenir; default güvenli (`[Authorize]`).

### 3.4 Global Hata Yönetimi & Gözlemlenebilirlik

> Tam rehber + kod kalıpları + "kullanıcı hata aldı" araştırma akışı: `docs/modules/error-handling.md`. Aşağısı bağlayıcı özet.

- **Tek nokta:** `CoreAlign.API/Middleware/ExceptionHandlingMiddleware.cs`. Tüm exception'lar buradan tek tip gövdeyle çıkar.
- **Yanıt zarfı `ApiResponse<T>`** (`CoreAlign.Application.Common`) — başarı **ve** hata aynı zarfta. Eski `{ "error": { code, message, details } }` şekli **kullanılmaz**. Gerçek şema:
  ```json
  {
    "isSuccess": false,
    "data": null,
    "errors": ["..."],
    "fieldErrors": { "Email": ["..."] },
    "statusCode": 409,
    "traceId": "..."
  }
  ```
  Başarı: `ApiResponse<T>.Success(data)`. `traceId` her yanıtta bulunur (madde 3.5/4 correlation).
- **Hatayı exception ile bildir, status'u KODLAMA.** Domain/Application'da `throw new <SpecificException>(...)`; middleware soyut tabana göre map eder:
  `NotFoundException`→404 · `ConflictException`→409 · `ForbiddenException`→403 · `AuthenticationException`→401 · `RateLimitExceededException`→429 · diğer `DomainException`→400 · FluentValidation `ValidationException`→400 (+`fieldErrors`) · `DbUpdateException` 23505/23503 & `DbUpdateConcurrencyException`→409 · eşleşmeyen→**500 (gövde generic)**.
- **Yeni hata tipi** `CoreAlign.Domain/Exceptions/` altında doğru **soyut tabandan** türetilir (`: NotFoundException` vb.) — `switch` koluna dokunma; mapping + DB capture otomatik gelir. Exception mesajı kullanıcı-görür + PII-siz olur (4xx'te client'a gider).
- **5xx detayı client'a SIZMAZ.** 500'de gövde her zaman generic ("An unexpected error occurred."); tam mesaj + stack yalnız sunucu logu + `error_logs`'a yazılır. `ex.Message`/`ex.ToString()` response'a **konmaz**. Controller'da `catch → return new { error = ex.Message }` yasak.
- **Try/catch sadece sınırlarda.** İş mantığında yutmak yasak; exception middleware'e akar.
- **DB error log (kalıcılık zorunlu):** her 5xx + anlamlı 4xx `error_logs` tablosuna yazılır (`IErrorLogWriter`; capture kararı `ShouldCapture` — Validation/Auth/NotFound/401/404 hariç). Tablo `BaseEntity`'dir (tenant-filter dışı, PlatformAdmin hepsini görür). Yazıcı kendi scope'unda, **asla throw etmez**, truncate + 5sn timeout. Kullanıcı "şurada hata aldım" dediğinde admin `error_logs`'tan `traceId`/sayfa/tarih ile bulur — yeni endpoint için ek iş yok.
- **Correlation tek id (madde 4):** `CorrelationIdMiddleware` `X-Correlation-Id` üretir/yansıtır; aynı id `error_logs.correlation_id` + yanıt `traceId` + Serilog + Sentry'de görünür. Yeni custom response `ApiResponse<T>` veya en az `ITraceableResponse` olmalı (zinciri kırma).
- **Admin görünürlük:** `GET /api/v1/admin/error-logs` (`PlatformAdmin` tümünü, `TenantAdmin` kendi tenant'ını). Frontend istemci hataları: `POST /api/v1/client-errors` (madde 2.4) aynı tabloya `Source=Frontend` düşer.
- **/metrics:** OTel sayaçları (`errorlog_persisted_total`, `errorlog_write_failed_total`) `Program.cs`'te bağlı (`AddCoreAlignOpenTelemetry` + `UseOpenTelemetryPrometheusScrapingEndpoint`, `OpenTelemetry:MetricsEnabled` guard'lı). `/metrics` auth'suzdur (Prometheus tasarımı) → **ingress/network seviyesinde kısıtla**.

### 3.5 Middleware & Yetkilendirme

- Pipeline sırası kritik: `ExceptionHandling → HTTPS → CORS → Authentication → Authorization → Controllers`. Bu sıra değişmez.
- Role/permission kontrolü policy tabanlı: `[Authorize(Policy = "Tenant.Admin")]`. String role karşılaştırması yasak.
- Tenant izolasyonu: Repository'de **her sorgu** tenant filter ile çalışır (EF global query filter zorunlu, multi-tenant kurulurken).

### 3.6 Repository / Service Düzeni

- Repository: pure data access (`IUserRepository` → `UserRepository`). LINQ-to-EF dışına çıkmaz.
- Service (Application layer): orchestrasyon + domain kuralı uygulama.
- **N+1 yasak.** `.Include()`, `AsSplitQuery()` veya projection kullanılır. Yeni query yazılırken EF generated SQL `dotnet ef migrations script` veya logger ile kontrol edilir.
- `IQueryable` Application/API'ye sızdırılmaz — Repository içinde materialize edilir veya spec pattern kullanılır.

### 3.7 Logging (Backend)

- `Microsoft.Extensions.Logging` + Serilog (kurulduğunda). `ILogger<T>` constructor injection.
- Log seviyeleri: `Information` (akış), `Warning` (anomali), `Error` (hata), `Critical` (servis bozulması). Verbose log üretimi yasak.
- PII (parola, token, e-posta vs.) loglara yazılmaz.

### 3.8 Validation & Sanitization

- Backend her input'u **kendi sınırında** valide eder; "frontend nasılsa zaten yaptı" varsayımı yasak.
- Email, GUID, length, range, format tek noktadan (FluentValidation).
- HTML/script gelmesi mümkün alanlar (örn. note, description) için sanitization veya output encoding.

### 3.9 MediatR Pipeline Sırası (SABİT — bozma)

Gerçek kayıt sırası (`ApplicationServiceRegistration`): **dıştan içe** wrap eder, yani ilk kayıt en dış, handler en iç.

```
Logging → Validation → ConcurrencyToken → Transaction → SaveChanges → Audit → Outbox → [Handler]
```

- **`ITransactionalRequest` opt-in'i kritik:** `SaveChangesBehavior` yalnız request `ITransactionalRequest` ise `SaveChangesAsync` çağırır; `TransactionBehavior` da yalnız o zaman transaction açar. **Para/stok/durum mutasyonu yapan her Command `ITransactionalRequest` olmalı** — yoksa save+commit+audit+outbox atomikliği yok, yarım iş kalır.
- **Audit + Outbox handler'dan SONRA ama SaveChanges'ten ÖNCE çalışır (stage eder):** domain değişikliği + audit log + outbox mesajı **tek `SaveChangesAsync`** ile, **tek transaction**'da persist edilir. Bu yüzden handler'da manuel `SaveChanges`/transaction yönetme (ITransactionalRequest isen); domain event'ini outbox'a yaz, behavior persist eder.
- **`ConcurrencyTokenBehavior`** `DbUpdateConcurrencyException`'ı **409 `DomainConcurrencyException`**'a çevirir (§4.6).
- **Cache-yazımı sıralaması (INV 69):** idempotency cache'i transaction commit'inden **önce** yazma — rollback olursa öksüz DTO replay edilir. Tercihen durable natural key (DB re-query); cache yalnız ikincil savunma.
- Sırayı/davranışları değiştirmek = sistemik finansal bütünlük riski. Yeni behavior eklemek gerekiyorsa yerini bilinçli seç ve gerekçeyi yaz.

---

## 4. Database — PostgreSQL Mühendislik Standardı

> CoreAlign **code-first**'tür: şema, Entity + EF Configuration kodundan `dotnet ef migrations add` ile üretilir. Bu yüzden **DB kalitesi koddan başlar** — bir kolonun tipi, bir index'in varlığı, bir FK'nin `OnDelete`'i, bir CHECK constraint hepsi Configuration'da/migration'da bilinçli yazılır. Aşağıdaki kurallar bağlayıcıdır ve "1 yıl sonra 10M–1B satır" (§11 Foresight) testinden geçecek şekilde tasarlanmıştır. §11 (Foresight), §12 (DB yaşam döngüsü), §16 (ERP doğruluk) bu bölümü tamamlar; çelişkide bu bölüm + §16 kazanır.

### 4.1 İsimlendirme

- **Tablo:** `snake_case`, **çoğul** (`users`, `customer_ledger_entries`).
- **Sütun:** `snake_case`. `*AtUtc` → `*_at_utc`. **PK:** `id` (Guid). **FK:** `<entity>_id`.
- **Timestamp:** her tabloda `created_at_utc`, `updated_at_utc` (`timestamptz`); soft-delete varsa `deleted_at_utc` veya `is_deleted`.
- **Index/constraint adı:** EF default convention global `snake_case` üretir; prefix örnekleri `ix`, `pk`, `fk`, `ux`, `ck`. Configuration'da elle isim verilmez (`ToTable`, `HasDatabaseName` çağrılmaz); `ApplySnakeCaseNaming()` halleder. Entity sınıfı PascalCase kalır.

### 4.2 Migrations & Governance

- Migration adı açıklayıcı (`AddSubscriptionStatusColumn`); EF `YYYYMMDDHHMMSS_` prefix'i verir. **Asıl tekillik = timestamp ID sırası** (ileri-tarihli Phase ID disiplini §4.12), label değil: `Phase##` insan-grubudur ve **kasıtlı gruplarda çoğullanabilir** (mevcut: `Phase52` ×3 customer-merge+concurrency, `Phase30/31/54/59/60/61/66` ×2 — bunlar drift değil, dokunma). Üretmeden önce yine de klasörü tara: yasak olan **aynı tablo+sütun için duplicate `AddColumn`/`CreateTable`/`CreateIndex`** (concurrent-merge çakışması, §17.1) — bunu canonical=en küçük ID ile temizle.
- **Production'a giden migration silinmez/değiştirilmez** (tarihçe immutable).
- **Idempotent yaz** (§12.7): `CREATE TABLE/INDEX IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, `ADD CONSTRAINT ... IF NOT EXISTS` (raw SQL ile), `DROP ... IF EXISTS`. EF auto-üretilen migration'lar bunu garanti etmez — yıkıcı/raw adımlarda guard'la.
- **Boş/scratch migration teslim edilmez.** `TempPendingProbe`, boş `Up/Down`, placeholder migration dev'de silinir; repoya bırakılmaz (Global Rule "No Scratch Files").
- **Veri migration'a gömülmez** (lookup seed hariç). Geri-alınamaz data backfill ayrı, post-deploy, idempotent bir command/job'a taşınır (§16.3 transaction sınırı).
- **Down migration gerçek olmalı** — finansal/audit tablolarda yıkıcı `Down` yazma; reversible değilse migration'ı forward-only işaretle ve nedenini not düş.
- **Apply-same-turn (§12.8):** migration yazıldıysa aynı turda `dotnet ef database update` ile uygulanır ve şema reconcile edilir. Uygulanamıyorsa idempotent yaz + follow-up'ı `docs/sprintN-blockers.md`'ye düş.
- **Snapshot drift yasağı:** Şemayı değiştiren her şey `CoreAlignDbContextModelSnapshot.cs`'e yansımalı. Bir index/constraint yalnızca `migrationBuilder.Sql()` ile yaratılıp model'de bildirilmezse (örn. GIN/BRIN/functional index), ya EF API'siyle modele bildir (§4.5 `HasMethod`) ya da `docs/RAW_SQL_INDEX_REGISTRY.md`'ye kaydet + INVARIANTS'a intentional-exception düş. **Modelde görünmeyen DB nesnesi = drift = sonraki `migrations add` onu görmez.**
- **Paralel ajan snapshot'ı tutuyorsa (§12.9):** snapshot mid-edit ise ona dokunma; el-yazımı idempotent migration ekle, hemen uygula, snapshot reconcile follow-up'ını blocker'a yaz.
- **Sıfırdan apply testi (§1.1):** yapısal değişiklikten sonra `DROP DATABASE → CREATE → ef database update` ile tüm zincir temiz uygulanmalı; `column already exists`/`does not exist` çıkarsa düzelt.

### 4.3 Tip & Precision Standardı (koddan pinlenir)

- **Para = `decimal(18,4)`** (`HasPrecision(18,4)`) **veya** minor-unit `bigint`. `float`/`double`/bare `numeric` (precision'sız) para için **yasak**. Tek istisna yok.
- **FX/exchange rate = tek proje-geneli scale.** Master `exchange_rates` ve tüm `ExchangeRate`/`FxRate*` kolonları **aynı** precision'ı kullanır (`decimal(18,6)` — `Money.RateScale` ile hizalı). Bir tabloda `18,6`, diğerinde `18,8` **yasak** (reconciliation drift).
- **Yüzde = `decimal(6,3)`** veya açık scale; `discount_percent`/`tax_rate` `[0,100]` CHECK ile (§4.4).
- **Miktar/quantity = `decimal(18,4)` veya `(12,3)`** (birim hassasiyetine göre, bilinçli). Fiziksel ölçü (glass mm) `(6,3)` gibi domain-uygun scale.
- **`float`/`double` yalnızca** gerçek bilimsel/yaklaşık ölçümlerde; para/oran/miktar/bakiyede asla.
- **Zaman = `timestamptz`, UTC saklanır** (§16.6). Naive `timestamp`, `DateTimeOffset` drift'i yasak. **Saf takvim tarihi** (vade, geçerlilik) `date`/`DateOnly` ile map edilir — özellikle unique key'e giren tarihlerde (`valid_on_date`). Aksi halde 00:00 UTC truncation belgelenir.
- **Enum saklama:** domain enum **int** olarak saklanır (kompakt) + geçerli değer kümesi DB'de CHECK ile zorlanır; ya da kasıtlı olarak `varchar` + CHECK. İkisinden biri seçilir, gerekçe net olur; serbest `text` status kolonu yasak.
- **String cap:** filtrelenen/indexlenen/iş-anlamlı her `string` `HasMaxLength(n)` ile sınırlanır (email/code/slug/phone/status). Sınırsız `text` yalnızca gerçek serbest-metin (note, description, markdown) için.
- **JSON = `jsonb`** (validity + GIN-ability). `varchar` içine JSON gömme yasak. Yalnızca write-once opaque blob ise gerekçesi not düşülür.
- **PK = UUIDv7** (time-ordered). `BaseEntity`/`TenantEntity` Id initializer `Guid.CreateVersion7()` kullanır (.NET 10). Random v4 (`Guid.NewGuid()`) yüksek-velocity tablolarda PK btree page-split + WAL amplification üretir — yeni entity'de v7. (int/long IDENTITY kullanan log tabloları zaten sequential.)

### 4.4 Bütünlük & Constraints

- **FK her zaman bildirilir** (`HasForeignKey`); soft Guid `*_id` referansı (FK'siz) bırakma — orphan + plansız seq-scan üretir. Bu, `tenant_id` dahil: her `TenantEntity` `tenant_id`'yi `HasOne<Tenant>().WithMany().HasForeignKey(...).OnDelete(Restrict).IsRequired()` ile gerçek FK yapar (convention loop ile toplu).
- **`OnDelete` bilinçli seçilir:**
  - **Restrict (default):** finansal/audit/ledger/stock geçmişi olan parent'lar (`customer_transactions→customers`, `vendor_ledger_entries→vendors`, `stock_transactions→products`, `journal_lines→gl_accounts`). Geçmiş varsa silme DB'de bloklanır.
  - **Cascade:** yalnızca gerçek parent-owns-child (`invoice_lines→invoices`, `journal_lines→journal_entries`, `payment_applications→payments`, junction'lar).
  - **SetNull:** opsiyonel attribution (`assigned_*_user_id`).
  - Finansal/audit child'ı parent silindiğinde **CASCADE ile silme** — bu para/iz kaybıdır.
- **CHECK constraint zorunlu (savunma derinliği):** app-katmanı enum/validation tek hat değildir; kötü migration/manuel SQL/domain-bypass DB'ye dayanır. Idempotent raw-SQL CHECK ile (Phase48 pattern): (1) `quantity`/`amount`/`on_hand` `>= 0`; (2) `discount_percent`/`tax_rate` `BETWEEN 0 AND 100`; (3) `debit >= 0 AND credit >= 0` + `NOT (debit > 0 AND credit > 0)`; (4) `journal_entries`: `total_debit = total_credit WHERE status='Posted'`; (5) status `IN (...)` (enum value-set'inden üret); (6) `start <= end` (dönem/geçerlilik). C# enum/sınırlardan türet, INVARIANTS'a kuralı düş.
- **Unique constraint tenant-scoped:** business key `(tenant_id, code/number/...)` ile unique — **global** `(code)` unique multi-tenant bug'ıdır. Junction tekilliği `(tenant_id, a_id, b_id)`. Reference/lookup tabloları (currencies, countries, modules) bilinçli **global**.
- **NOT NULL disiplini:** zorunlu FK, para, status, `tenant_id` `IsRequired`. `nullable` her zaman bir kararın sonucu, gerekçesi anlaşılır.

### 4.5 Index Disiplini

- **Her FK'ye index** (join + cascade + RI seek). Filtrelenen/sıralanan/join'lenen her kolon indexlenir; index'siz `>10k` satır beklenen sorgu yazılmaz (§11.2).
- **Composite sıra selectivity'ye göre, tenant-leading:** tenant-scoped hot tablolarda `tenant_id` **lider** (pruning + selectivity). Eşitlik-filtre kolonları range/sort kolonlarından önce.
- **Status index'lerine trailing sort kolonu:** düşük-kardinalite `(tenant_id, status)` work-queue sorgusunu karşılamaz; `(tenant_id, status, created_at_utc DESC)` (veya domain date) yaz, redundant bare `(tenant_id, status)`'ı düşür.
- **Partial index:** soft-delete tablolarında **her unique index** `HasFilter("is_deleted = false")`/`"deleted_at_utc IS NULL"` (yoksa silinen kayıt re-create'te 23505). Sıcak index'lere de `WHERE NOT is_deleted`. Aktif-altküme sorgularına partial (`outbox WHERE status IN ('Pending','Deferred')`).
- **Covering/INCLUDE** dar hot read path'lerde. **Redundant index** (daha uzun unique'in left-prefix'i olan non-unique) düşürülür — write amplification (§12.3).
- **GIN:** trigram/ILIKE arama (`USING gin (... gin_trgm_ops)`) ve content-filtrelenen `jsonb` (`jsonb_path_ops`) için. EF API ile bildir: `HasIndex(...).HasMethod("gin").HasOperators("gin_trgm_ops")` — raw SQL'e gömüp snapshot'tan saklama (§4.2 drift). Trigram index'leri mümkünse tenant-scoped (`btree_gin` ile `(tenant_id, lower(col))`) — cross-tenant candidate set'i küçült.
- **BRIN:** append-only, fiziksel-zaman-sıralı tablolarda zaman kolonuna (`USING brin`) — milyar-satırda btree'nin GB'larına karşı KB. Partition + BRIN birlikte (§4.9).
- **Keyset (seek) pagination zorunlu** büyüyen tablolarda (§11.1): `WHERE (tenant_id, created_at_utc, id) < @cursor ORDER BY ... DESC LIMIT n` + uygun composite index. `OFFSET` yalnızca küçük bounded admin listelerinde.
- **EXPLAIN ANALYZE gate (§12.4):** `>10k` satır beklenen yeni/değişen sorguda plan kontrol; beklenmeyen seq scan → index ekle, sonucu PR notuna yaz.

### 4.6 Concurrency & ERP Doğruluğu (§16 ile)

- **Optimistic concurrency = İKİ mekanizma, bilinçli seçilir (HYBRID — bunu bozma).** Çakışma her ikisinde de `DbUpdateConcurrencyException` → **409 `DomainConcurrencyException`** (`ConcurrencyTokenBehavior` ile bağlı), sessiz overwrite yok. Yeni yarışabilen entity'de **birini** seç (ikisini aynı entity'ye takma); mevcut seçimi gerekçesiz değiştirme:
  - **`IXminConcurrency`** → Postgres `xmin` sistem kolonu. **⚠️ PG18'DE DEVRE DIŞI (2026-07): `CoreAlignDbContext.ApplyXminConcurrencyTokens` bilinçli NO-OP** (`_ = modelBuilder;`) — PG18 `RETURNING xmin`'i reddediyor (SQLSTATE `0A000`) ve tüm finansal entity save'lerini kırıyordu. Dolayısıyla `IXminConcurrency` şu an **ÖLÜ MARKER**: onu HÂLÂ taşıyan `Invoice, Payment, Order, JournalEntry, CustomerLedgerEntry, VendorLedgerEntry, Employee, PayrollRun, Payslip` **şu an optimistic concurrency'siz** (lost-update/double-post riski — açık teknik borç; app-managed token'a taşınmalı). **Yeni yarışabilen finansal entity'de `IXminConcurrency` KULLANMA → `IHasConcurrencyToken`.**
  - **`IHasConcurrencyToken`** → app-managed `long ConcurrencyToken` + açık `BumpConcurrencyToken()` (SaveChangesAsync Modified'da otomatik bump'lar). **Hem Npgsql hem SQLite test provider'ında çalışır — PG18'de ÇALIŞAN tek mekanizma.** Kullananlar: `ProductVariant, StockItem (Phase71), VendorBill/VendorPayment/PurchaseOrder (Phase117), FxRate, DataSubjectRequest, GlassProject ailesi (FieldSurvey/GlassProject/GlassProjectPanel/GlassProjectRun/GlassWorkOrder)`.
  - **NEDEN ikisi (kritik tuzak):** `xmin` **yalnız Npgsql**'de bedava gelir; SQLite `EnsureCreated` test yolu onu **gerçek NOT NULL kolon** olarak materialize eder ve her insert patlar (commit `fc66c68`: xmin unconditional çalışınca **161 entegrasyon testi kırıldı**; `IsNpgsql()` guard'ı ile düzeldi). Bu yüzden: **`xmin`'i asla guard'sız/unconditional uygulama.** Concurrency'si **SQLite testinde kanıtlanması gereken** veya app-kontrollü bump isteyen entity → `IHasConcurrencyToken`. Mevcut `IHasConcurrencyToken` entity'sini "xmin'e taşı" diye değiştirme — test paritesini kırarsın.
  - Concurrency token'ın gerçekten çalıştığını **InMemory provider kanıtlamaz** (`IsConcurrencyToken` ignore); SQLite `:memory:` + iki context ile doğrula (INV 64).
- **Gapless döküman numarası atomik:** `document_sequences` tüketimi **tek atomik statement** — `UPDATE document_sequences SET next_number = next_number + 1 WHERE tenant_id=@t AND type=@ty RETURNING next_number - 1`. Read-modify-write (`NextNumber++`) **yasak** (lost-update + duplicate + 23505→500 cascade). Gerçek DB-gaplessness gerekmiyorsa per-(tenant,type) Postgres `SEQUENCE`.
- **Ledger append serialize:** `running_balance_after` hesaplayan ledger insert'i müşteri/satıcı başına `pg_advisory_xact_lock(hashtextextended(<party>_id))` ile serialize edilir (pattern: `QuoteRepository.AcquireConversionLockAsync`). Kilitsiz "son bakiyeyi oku" kalıcı bakiye bozulmasıdır.
- **Idempotency (§16.2):** para/stok mutasyonu idempotency key + unique constraint ile; retry çift kayıt üretmez. **Transaction sınırı (§16.3):** çok-tablolu tutarlılık (sipariş+stok+fatura+ledger) tek UnitOfWork.
- **23505 → 409 map:** `ExceptionHandlingMiddleware`'de `DbUpdateException`/`PostgresException` SQLSTATE `23505` (unique_violation) **409**'a, `23503` (FK) **409/422**'ye map edilir — unique yarışları 500 dönmez.
- **FILLFACTOR:** sürekli UPDATE edilen hot satırlarda (`stock_items`, `document_sequences`) `ALTER TABLE ... SET (fillfactor=85)` — HOT-update headroom.

### 4.7 Multi-Tenant DB Kuralları

- **`tenant_id` her index/unique'de lider** (tenant-scoped tablolarda) — pruning + tenant-scoped uniqueness.
- **`tenant_id` gerçek FK** (`→ tenants`, Restrict) — soft Guid bırakma (cross-tenant orphan tespit edilemez; global filtre orphan'ı saklar).
- **RLS = savunma derinliği (yüksek-değerli tablolarda).** App-katmanı global filtre tek hat değildir (`IgnoreQueryFilters`/Dapper bypass eder). Finansal/ledger/stock tablolarına önce: `ENABLE/FORCE ROW LEVEL SECURITY; CREATE POLICY tenant_isolation USING (tenant_id = current_setting('app.tenant_id')::uuid)`. GUC'yi `DbConnection` interceptor'da `TenantContextAccessor`'dan set et; app non-owner DB rolü kullansın (RLS-bypass etmesin); global/FX tabloları muaf.
- **`IgnoreQueryFilters()` disiplini:** her kullanım bilinçli; filtre düştüğünde `TenantId` manuel re-scope edilir veya yorumla gerekçelendirilir. Global-read için tek sanctioned yol seçilir (`IGlobalReadable` semantiği implement edilir ya da explicit `IgnoreQueryFilters` pattern'i — ikisi karışık değil).
- **Yeni tenant-data entity'si `TenantEntity` türetir** (otomatik filtre + auto-stamp + tenant FK). `ITenantOwned`'ı `BaseEntity` üstüne elle takma — auto-stamp loop `Entries<ITenantOwned>()` görmez.

### 4.8 Soft-Delete, Audit & Retention

- **Soft-delete politikası entity başına bilinçli:** `ISoftDeletable` (+ partial unique, §4.5) **veya** `status=Archived` + hard-delete bloklu. Master/finansal parent'lar hard-delete edilmez (ledger child'ları `Restrict`). Politika INVARIANTS'a yazılır.
- **Audit append-only + zincir bütünlüğü:** `EntityAuditLog` insert-only, eski→yeni, actor+tenant, per-tenant rolling-hash chain (§16.5). Retention zinciri kırmaz — purge'de signed checkpoint satırı yaz veya cold storage; finansal tenant'larda `KeepFinancialTrail`. Audit sequence atomik allocate edilir (advisory lock / DB sequence) — `(tenant_id, sequence)` unique altında eşzamanlı writer 23505 üretmesin.
- **Retention ölçeklenebilir:** satır-satır `ExecuteDeleteAsync`/in-memory `.ToListAsync()` yasak (OOM). Partition gelene kadar **batched/keyset** (10k chunk); partition sonrası retention = `DROP/DETACH PARTITION` (O(1), §4.9).

### 4.9 Ölçek & Partitioning

- **High-growth tablolar register'lanır** (`docs/INVARIANTS.md` `[PERF]`, §11.5): audit/log (`entity_audit_logs`, `login_audit_logs`, `activity_logs`, `glass_project_change_logs`), ledger (`customer/vendor/dealer_commission_ledger_entries`, `customer_transactions`), stock (`stock_movements`, `stock_transactions`), messaging (`outbox_messages`, `provider_webhook_inbox`, `processed_webhook_events`, `notification_messages`). Bu tablolara dokunan task partitioning adımını tetikler.
- **Strateji:** zaman kolonuyla **RANGE** partition (ledger çeyreklik, stock/audit aylık, outbox/webhook haftalık-aylık); çok büyük tek tenant'lar için **HASH by `tenant_id`** sub-partition. **EF `PARTITION BY` emit edemez** → `migrationBuilder.Sql()` + partition pre-create + rollover otomasyonu (**pg_partman** veya scheduled `HostedService`).
- **PG kuralı:** partition key her UNIQUE/PK'nin parçası olmalı → `id` PK'yi `(id, <ts>)` yap veya mevcut `(tenant_id, sequence)`/business-unique partition key'i absorbe etsin.
- **BRIN her partition içinde** zaman kolonuna; selective `tenant_id`-equality için btree korunur.
- **Retention = `DROP PARTITION`** (satır DELETE değil): O(1), WAL/vacuum yok. Finansal partition'lar silinmez → compressed/cold tablespace'e DETACH.
- **Partition GÜVENLİK kontrolü (zorunlu, battle-tested):** Bir tabloyu partition'lamadan ÖNCE `pg_indexes`'te `CREATE UNIQUE INDEX` ara — **sadece `pg_constraint`'e bakmak yetmez**; EF `IsUnique()` index'leri constraint değil, ayrı unique index'tir. Partition key'i içermeyen bir unique index varsa (örn. `notification_messages` idempotency, `entity_audit_logs` hash-chain `(tenant_id, sequence)`) o tablo partition'a **UYGUN DEĞİL** — partition'lamak o uniqueness'i sessizce düşürür (**correctness regression**, bu oturumda yaşandı). Böyle tabloları hariç tut. `LIKE INCLUDING INDEXES` PK'nin unique index'ini kopyalayıp partition'da hata verir → `INCLUDING DEFAULTS INCLUDING IDENTITY` kullan + non-unique index'leri `pg_indexes`'ten EF isimleriyle yeniden oluştur (snapshot drift'i azalır). `bigint IDENTITY` / `tenant_id`'siz tablolarda RLS + tenant-FK adımlarını koşullu atla.
- **Rollover ZORUNLU:** partition'lı tabloya scheduled rollover job'u (`corealign_ensure_future_partitions`) bağlanmadan partition migration teslim edilmez — yoksa pre-create penceresi bitince yeni satırlar tek `_pdefault` partition'ına düşer ve pruning bozulur (silent availability cliff).

### 4.10 PostgreSQL İşletme

- **Extension'lar guard'lı + belgeli:** `CREATE EXTENSION IF NOT EXISTS pg_trgm` (ve gerekirse `btree_gin`, `pg_partman`) migration'da; fresh-DB provisioning için `docs/`'ta listeli.
- **Bağlantı dayanıklılığı:** Npgsql `EnableRetryOnFailure` (transient) + bilinçli `CommandTimeout`; connection pooling (PgBouncer-uyumlu — prepared statement ayarına dikkat).
- **Read disiplini:** read query'lerde `AsNoTracking`; koleksiyon include'larında `AsSplitQuery` veya projection (N+1 yok, §3.6/§11.3).
- **Test parity uyarısı:** Sqlite test yolu (`EnsureCreated`) şemayı **modelden** kurar — raw-SQL-only index'ler (GIN/BRIN/partition) orada yok; bu nesnelere bağlı davranış Postgres integration test ile doğrulanır (§4.2 drift ile birleşir).

### 4.11 Ölçek Sorgu Disiplini (her liste / report / yeni endpoint — empirik doğrulandı)

- **Liste sayfaları index-backed:** sıralanan her liste sorgusu için `(tenant_id, <sort_col> DESC, id DESC)` composite index — trailing `id` tiebreaker olmadan keyset deterministik değil. (Validated: sığ sayfa Index Scan ile **sub-ms**.)
- **Append-only / infinite-scroll = KEYSET (seek):** ledger, transaction history, audit, stream/export gibi büyüyen tablolarda `OFFSET`/`Skip()` **YASAK**. Keyset: `WHERE tenant_id=@t AND (sort_col < @cur OR (sort_col=@cur AND id < @curId)) ORDER BY sort_col DESC, id DESC LIMIT n`. Sebep: deep `OFFSET` N+M satır tarayıp atar (page 4000 ≈ **225ms**, lineer büyür) ve partition pruning'i yener; `Skip`-loop'lu stream **O(n²)**. (Validated: keyset **0.47ms sabit** vs OFFSET 225ms; audit export O(n²)→O(n).) Page-numaralı liste keyset'e geçince API/UI cursor'a döner — bilinçli yap.
- **Report/aggregate SERVER-SIDE:** `SUM`/`GROUP BY`/`COUNT` **her zaman SQL'de**. Tüm tabloyu `ToListAsync()` edip C#'ta `GroupBy/Sum` **YASAK** (OOM + GC + lineer transfer). EF `Any`-subquery + GroupBy translate olmuyorsa **join + GROUP BY**'a çevir. **Top-N report'ta**: önce `GroupBy(key).Select(Sum...).OrderByDescending(...).Take(n)` (server-side, sadece N satır döner) — `Take`'i aggregate'ten ÖNCE koyup tüm tabloyu indirme. (Validated: TrialBalance 2M satır → 50 satır **132ms**; TopProducts iki-pass.)
- **`COUNT(DISTINCT x)` per-group — raw SQL'e gitme, decompose et:** EF Core 10 `GroupBy` aggregate'i İÇİNDE nested `g.Select(x=>x.Y).Distinct().Count()` translate **etmez** (client-eval → throw). Çözüm raw SQL DEĞİL: `query.Select(l => new {l.Key, l.Y}).Distinct().GroupBy(x=>x.Key).Select(g=>new{g.Key, g.Count()})` → Npgsql `SELECT key, count(*) FROM (SELECT DISTINCT key, y ...) GROUP BY key` üretir. Bu yol **EF global query filter'ı (tenant) korur**; raw SQL filter'ı bypass eder (RLS yoksa cross-tenant sızıntı). Top-N'de bu ikinci pass'i **sadece kazanan N product'a** scope'la. (Validated: `ToQueryString` + execute, gerçek şema.)
- **Aggregation rewrite'ı `ToQueryString()` ile DOĞRULA:** her yeni/değişen GroupBy/Distinct/join sorgusunda, güvenmeden önce `IQueryable.ToQueryString()` çıktısını Npgsql'e karşı incele (translate olmuyorsa runtime'da `InvalidOperationException` atar, build yakalamaz). SQLite test fixture'ı Npgsql translation'ı **kanıtlamaz** — provider-specific; gerçek Postgres'e karşı doğrula. **Bu oturumda kanıtlanan iki sınır:** (a) grouped conditional `SUM` İÇİNDE `COALESCE(a,b)` (`x.A ?? x.B`) translate **olmaz** — null-expansion/pre-projection de EF tarafından inline edilip aynı hataya düşer; (b) bazı entity'lerde (ör. `VendorBill`) server-side `GroupBy` hiç translate olmuyor (düz `g.Sum(x=>x.Total)` bile patlıyor) — aynı şekil `Invoice`'ta çalışıyor, yani model-spesifik. Translation gerçekten imkânsızsa in-memory bucketing **sadece set kanıtlanabilir şekilde bounded ise** kabul edilir (ör. açık vendor-bill seti); unbounded sette (customer invoice, ledger) ASLA — orada COALESCE'i kaldır veya kolonu non-null'a taşı.
- **N+1 = batch-load:** loop içinde tek-tek `GetByIdAsync` **YASAK**; `GetByIdsAsync(IEnumerable<Guid>)` → tek `WHERE Id IN (...)` + in-memory dictionary. Composite-key lookup (örn. stock item `(ProductId, LotId)`) için repo'ya `Dictionary<(K1,K2),V>` döndüren batch metod ekle — loop'ta `GetAsync(a,b,c)` çağırma. Koleksiyon listesinde `.Include(child)` yerine slim projection (`...SearchRow`) + `Lines.Count` scalar subquery; full include sadece detail'de.
- **>10k beklenen yeni/değişen hot sorguda EXPLAIN ANALYZE** (gerçek veya sentetik milyon-satır veriyle): seq scan / disk-spill sort / kaçırılan partition pruning varsa düzelt; planı PR notuna yaz.

### 4.12 Migration & EF Tuzakları (battle-tested — bir daha yaşama)

- **Migration ID ordering:** proje Phase## tarihlerini **ileri-tarihli** kullanıyor; `dotnet ef migrations add` wall-clock ID verir (son Phase'den ÖNCE sıralanır → apply order bozulur). Yeni migration üretince ID'yi **son Phase'den sonraya rename et** (hem dosya adı hem `.Designer.cs`'teki `[Migration("...")]`).
- **`migrations add --no-build` tuzağı:** config/entity edit sonrası `--no-build` stale assembly kullanır → BOŞ/yanlış migration üretir. Config değişiminden sonra build'li `migrations add`.
- **Pending model değişikliği bloke eder:** başka ajan model'e entity ekleyip migrate etmediyse `has-pending-model-changes`=yes → `database update` `PendingModelChangesWarning` ile patlar ve `migrations add` onların değişikliğini seninkine bundle'lar. Önce kontrol et; kirliyse ya onların catch-up'ını üret ya da §12.9 el-yazımı idempotent raw-SQL + snapshot'a dokunma.
- **`IGlobalReadable` tenant-FK istisnası:** `IGlobalReadable` entity'ler `tenant_id = Guid.Empty` (global, gerçek tenant değil) satır tutar → tenant-FK convention (`ApplyTenantForeignKeys`) bunları **HARİÇ tutmalı**, yoksa startup `MigrateAsync` `23503` ile patlar (bu oturumda yaşandı). Yeni `IGlobalReadable` entity = exclusion'ı doğrula.
- **RLS yeni-tablo kapsamı:** RLS policy migration'ından SONRA eklenen tenant tablosu policy almaz → RLS application'ı re-runnable yap veya yeni tablo migration'ında policy'yi de uygula.
- **Doğrula, varsayma:** her yapısal değişiklik (partition, FK, index, RLS, aggregation rewrite) ACTUAL DB'ye karşı **psql/EXPLAIN ile doğrulanır**, varsayılmaz. Bu oturumda partition'ın bir unique index'i sessizce düşürdüğü ancak adversarial doğrulamayla yakalandı.

---

## 5. Auth — Mevcut Kapsamlı İşi Koruma

Login akışı zaten **derin yatırım yapılmış** bir bölüm. Üzerinde çalışırken:

- Mevcut entity'ler (`User`, `Role`, `UserRole`, `RefreshToken`, `UserSession`, `EmailVerificationToken`, `PasswordResetToken`, `LoginAuditLog`, `Subscription`, `SubscriptionPlan`) **kaldırılmaz veya gereksizce parçalanmaz** — gerekçe sunmadan dokunulmaz.
- Parola: BCrypt/Argon2. Düz hash yok. Reset/email-verification token tek kullanımlık, TTL'li.
- Refresh token rotation + reuse detection açık. Logout = refresh token revoke.
- LoginAuditLog her başarılı/başarısız girişte yazılır (IP, fingerprint, sonuç, sebep).
- DeviceFingerprint (`shared/lib/deviceFingerprint.ts`) mevcut — yeni cihazda audit event + (gerekiyorsa) email bildirimi.

---

## 6. Yeni Özellik İş Akışı

Sırayla, atlanmaz:

0. **Yönel (önce).** Hangi **frontend yüzeyi** (§0) ve hangi **modül** (§0.1 indeks)? O satırın tuzaklarını + "önce oku" dokümanını ve `INVARIANTS.md`'yi oku. En yakın mevcut modülü örnek al (§13.1).
1. **Planla** — etkilenen modüller, DB, API, UI listele. Tasarım kararını kullanıcıya 2-3 cümle ile özetle.
2. **Domain & DB** — Entity ekle/değiştir, EF Configuration güncelle, migration yarat.
3. **Application** — Query/Command + Validator + DTO + Handler.
4. **Infrastructure** — Repository extension'ları, dış servis adaptörü.
5. **API** — Controller endpoint'i (slim).
6. **Frontend API katmanı** — `features/<x>/api/`.
7. **Hooks** — `useQuery`/`useMutation`.
8. **UI** — Component + Page + i18n anahtarları (tr+en).
9. **Doğrula** — Build (front+back), lint (sıfır warning), DB migrate test, akışı tarayıcıda dene.

---

## 7. Kullanıcıya Karşı Sorumluluk

- **Düzeltme yetkisi:** Talimat eksik veya hatalıysa düzeltilir, gerekçe açıklanır. "Söylendi" diye yanlış uygulanmaz.
- **Risk eylemleri:** DB silme, branch silme, force-push, geri alınamayan migration → istemde önce onay.
- **Belirsizlikte sus, sor.** Tahmin ile feature/dosya/API üretme.
- **Bitti raporu doğru olur.** "Tamam" denmeden önce: build geçer, lint sıfır, akış tarayıcıda test edildi. Edilemiyorsa açıkça yazılır.

---

## 8. Kod İncelemesi — Her Değişiklik Sonrası

Hızlı kontrol listesi (mental):

- [ ] Yorum kalmadı mı?
- [ ] `console.*` veya `@ts-ignore` var mı?
- [ ] Görünen metin `t()` içinde mi? `tr+en` dosyaları senkron mu?
- [ ] N+1 / sınırsız query var mı?
- [ ] Try/catch sınırlarda mı, yutuluyor mu?
- [ ] Dark mode + responsive çalışıyor mu?
- [ ] Endpoint `[Authorize]` mi? Tenant filter çalışıyor mu?
- [ ] Migration adı ve içeriği temiz mi?
- [ ] DTO ↔ Entity sızıntısı var mı?
- [ ] Lint/build/test geçiyor mu?

---

## 8.0 Refresh Token Strategy — httpOnly Cookie

- **Access token:** JWT, Bearer header'da, sadece bellekte (`useAuthStore.accessToken`). Page refresh'te kaybolur → ilk istek 401 → interceptor refresh → yeni access token.
- **Refresh token:** Backend `AuthController` tarafından `corealign_refresh_token` adlı **httpOnly + SameSite=Lax** cookie'ye yazılır. `Path = /api/v1/auth`. JS erişemez (XSS koruması).
- **Login / Refresh** sonrası: backend cookie set eder, response body'sinden `RefreshToken` string'ini siler (frontend asla görmez).
- **Refresh endpoint:** body opsiyonel; cookie varsa kullanır, yoksa 401.
- **Logout:** cookie'yi okur (revoke için), sonra siler.
- **Dev cross-origin:** Vite proxy `/api → http://localhost:5178`. Frontend `apiClient.baseURL = '/api/v1'` (relative; `VITE_API_URL=` boş). Browser cookie'yi `localhost:5273` origin'ine bağlar; proxy üzerinden 5178'e geçer — same-site, Lax çalışır.
- **Prod cross-origin:** Eğer frontend ve backend farklı domain'deyse cookie için `SameSite=None; Secure=true` gerekir (HTTPS şart). `Request.IsHttps` ile dinamik set ediliyor.
- **CSRF:** State-changing endpoint'ler Bearer header ister; refresh tek başına bir saldırı vektörü değil (response cross-origin okunamaz).

---

## 8.2 Test Standartları

**İki backend test projesi (ikisi de yeşil kalmalı):**

1. **`server/tests/CoreAlign.Application.Tests/`** — Unit/handler. xUnit + NSubstitute + FluentAssertions. Repo/UoW mock'lı.
2. **`server/tests/CoreAlign.Integration.Tests/`** — Gerçek-DB (SQLite/Npgsql). **Her yeni endpoint için:** happy + auth-reddi + **cross-tenant izolasyon** (`CrossTenantIsolationTests`/`PortalScopeIsolationTests` append-only, `AcceptableDeny`={404,403}) + **N+1 round-trip bütçesi** (`DbCommandRoundTripInterceptor`, tight bütçe 3-4). Concurrency'yi SQLite `:memory:` çift-context ile kanıtla (INV 64). xmin guard'ı burada test parity'yi etkiler (§4.6).

**Stack notu:** `TreatWarningsAsErrors` + global using'ler (Xunit, NSubstitute, FluentAssertions) csproj'de bind.

**Kapsam:**

- **Unit testleri** Application/handler katmanına odaklanır. Repository ve UnitOfWork `Substitute.For<>()` ile mock'lanır.
- Test dosya yolu = mirror: `Application/Orders/Handlers/X.cs` → `Application.Tests/Orders/XTests.cs`
- Test isimleri: `Verb_condition_expected_result` (snake) — `Confirming_draft_order_with_sufficient_stock_decrements_product_stock`.
- Her test arrange/act/assert ayrı; helper'lar dosya altında private static.
- **State machine** ve **business rule** branch'lerinin her biri en az bir test → kritik regression sigortası.

**Yazılması gereken testler:** yeni handler eklediğinde, en az happy path + 1 failure path. State machine bulunan handler'larda her geçiş ayrı test.

**Komutlar:**

```bash
dotnet test server/tests/CoreAlign.Application.Tests
dotnet test server/tests/CoreAlign.Integration.Tests
```

### 8.2.1 Frontend Tests — Vitest

**Konum:** `src/**/*.test.ts(x)` (kaynak ile yan yana — colocation)
**Stack:** Vitest + jsdom + `vi.mock('sonner')` setup'ı (`src/test/setup.ts`)

**Kapsam (mevcut):**

- `src/features/*/model/*Schema.test.ts` — zod schema'ların happy/failure path'leri (5 dosya, schema başına 4-6 test)
- `src/shared/lib/safeRequest.test.ts` — başarılı/başarısız ApiResponse + axios throw senaryoları
- `src/shared/lib/mutationToast.test.ts` — AxiosError ve düz Error normalize'i

**Yazılması gereken:** Yeni zod schema veya saf utility eklediğinde, en az happy path + 2 failure path. React component testleri (RTL) sonraki adım — şu an pure logic ile başlıyoruz.

**Komutlar:**

```bash
npm run test           # Tek sefer
npm run test:watch     # Watch mode
npm run test:ui        # UI mode (browser)
npm run test:coverage  # v8 coverage report
```

### 8.2.2 E2E — Playwright

- **Konum:** `e2e/` (config `e2e/playwright.config.ts`); projeler: `admin`, `customer-portal`, `b2b` (üç SPA için ayrı).
- Kritik akışlar (login, ana modül smoke, glass-enclosure) e2e ile korunur. Yeni kritik kullanıcı-akışı eklerken ilgili proje altına e2e ekle.
- **Komutlar:** `npm run e2e` (hepsi) · `npm run e2e:admin` / `e2e:customer` / `e2e:b2b` · `npm run e2e:ui` · `npm run e2e:report`. İlk kurulum: `npm run e2e:install`.

### 8.2.3 Diğer kalite kapıları

- **Bundle bütçesi (3 SPA):** `npm run check:bundle:all` — per-chunk limit (INV 43/52/58, `scripts/check-bundle-size.mjs`, exemption tablosu `docs/performance-budget.md`).
- **Mutation (Stryker):** yeni Application kodu eşiğin altına çekmez (§14.3).

---

## 8.1 Multi-Tenant Mimari

CoreAlign **multi-tenant ERP**'dir. Her register başına bir `Tenant` (organizasyon) yaratılır; User → Tenant N:1.

**Anahtarlar:**

- `Domain/Common/ITenantOwned` — interface marker (`Guid TenantId`)
- `Domain/Common/TenantEntity` — abstract base: Id, TenantId, CreatedAtUtc, UpdatedAtUtc; ITenantOwned implementer
- `Domain/Entities/Tenant` — kök (Id, Name, Slug unique, IsActive, timestamps)
- `Domain/Interfaces/ITenantContext` — `Guid? CurrentTenantId`, `bool HasTenant`
- `Infrastructure/Services/TenantContextAccessor` — JWT `tenant_id` claim'inden okur, `IHttpContextAccessor` ile
- `JwtTokenService.GenerateAccessToken(userId, tenantId, email, roles)` — `tenant_id` claim yayar

**DbContext davranışı:**

- `ApplyTenantQueryFilters` — `ITenantOwned`'i implement eden tüm entity'lere otomatik global query filter (`WHERE tenant_id = current_tenant_id`).
- `SaveChangesAsync` — yeni eklenen ITenantOwned entity'de `TenantId == Guid.Empty` ise context'ten otomatik atar; `UpdatedAtUtc` property'si olan Modified entity'lere otomatik tarih basar.
- **User filtre dışı** — User'a TenantId eklendi ama `ITenantOwned` implement etmez (auth flow tenant context'ten önce çalışır). Auth lookup'larında `IgnoreQueryFilters()` kullanılır. Future entity'ler (`Customer`, `Project`, ...) `TenantEntity` türetip otomatik filtreden faydalanır.

**Yeni entity rehberi:**

```csharp
public class Project : TenantEntity   // ← TenantId, Id, timestamps gelir
{
    public string Name { get; set; }
    // ...
}
```

DbContext'e `DbSet<Project> Projects => Set<Project>();` ekle, Configuration yaz. Migration üret. TenantId otomatik dolar, query'ler otomatik filtrelenir.

**Register akışı:** `RegisterCommand` artık `OrganizationName` alır → `Tenant.GenerateSlug()` → benzersiz slug → yeni Tenant + ilk User (`TenantAdmin` rolü) + Free Trial Subscription.

---

## 9. Mevcut Durum (Snapshot — güncel)

> Bu bölüm "ne var" der; "nereye dokunursan neyi bozma" için **§0.1 Modül Guardrail İndeksi**. Bilinen açık işler/teknik borç: **`docs/sprint{8..13}-blockers.md`, `docs/mrp-blockers.md`, `docs/DB_RECONCILE_FOLLOWUP.md`** — task başında ilgili olanı tara.

**Olgun altyapı (kurulu — yeniden kurma):** Clean Architecture + CQRS (MediatR, 7-behavior pipeline §3.9), FluentValidation, `ExceptionHandlingMiddleware` + `ApiResponse<T>` + correlation + `error_logs`, Serilog (rolling file), API versioning `/api/v1`, Swagger Bearer, rate limiter (auth 10/dk + global 200/dk), `/health`, CORS whitelist, snake_case convention, Sentry + OpenTelemetry `/metrics`, **Hangfire** (job), **Outbox**, **QuestPDF**, multi-tenant (`TenantEntity` + global filter + RLS), **NSwag/OpenAPI** üretimi (`nswag.json` + `openapi/`).

**Canlı ERP modülleri (~55 Application modülü, kısmi liste):** Accounting (TDHP, trial balance, balance sheet, income statement, year-end close), Invoices, Payments, Customers, Vendors, Orders, Quotes, Purchasing (PO/PR/GoodsReceipt/3-way-match), Inventory/Stock, Products/Catalog, Pricing, Returns, Shipments, Tax, Treasury, Fx, Payroll, MRP, **GlassEnclosure (Cam Mekan — büyük)**, B2B (dealer flow), CustomerPortal, Notifications/Providers, EInvoice, Compliance/Privacy/Consents (KVKK), Imports, BI/Reports, Warranty, Installation, Whitelabel, Sso, Tags, MasterData, Lookups.

**Frontend:** 3 web SPA (admin `src/` + `apps/{customer-portal,b2b}`) + 1 Expo mobil (`mobile/`). Design system Phase-3 olgun (`docs/DESIGN_SYSTEM.md` + design memory). 5-dil locale (fallback=en).

**CI/kalite:** `TreatWarningsAsErrors` her backend csproj'de; lint `--max-warnings=0`; Husky pre-commit + lint-staged; Vitest + Playwright e2e + bundle bütçesi + Stryker. PostgreSQL Docker Compose (`docker compose up -d`).

---

## 10. Komutlar / Hızlı Referans

```bash
# PostgreSQL (ilk açılış)
docker compose up -d

# Backend
dotnet build server/src/CoreAlign.API/CoreAlign.API.csproj
dotnet ef migrations add <Name> -p server/src/CoreAlign.Infrastructure -s server/src/CoreAlign.API -o Persistence/Migrations
dotnet ef database update -p server/src/CoreAlign.Infrastructure -s server/src/CoreAlign.API

# Tests (backend — ikisi de)
dotnet test server/tests/CoreAlign.Application.Tests
dotnet test server/tests/CoreAlign.Integration.Tests

# Frontend (admin SPA)
npm run dev          # http://localhost:5273
npm run build        # tsc -b && vite build && prerender-seo (SEO shell üretir)
npm run typecheck    # tsc -b --noEmit
npm run lint         # 0 warning zorunlu
npm run lint:fix
npm run format       # prettier write
npm run test         # Vitest (frontend unit tests)
npm run test:coverage

# E2E (3 SPA) + bundle bütçesi
npm run e2e          # admin + customer-portal + b2b   (ilk: npm run e2e:install)
npm run e2e:admin    # / e2e:customer / e2e:b2b
npm run check:bundle:all

# Portallar (root workspaces YOK → --prefix kullan, INV 50)
npm --prefix apps/customer-portal run build
npm --prefix apps/b2b run build

# Mobil (Expo)
cd mobile && npm install && npx expo start

# Full-stack (VS Code)
F5 → "Full Stack (Backend + Frontend)" compound
```

**Portlar (emcm-web ile çakışmaz):**

- Backend HTTP: **5178** · HTTPS: **7184**
- Frontend Vite: **5273** (strictPort)
- PostgreSQL: **5432**

**Endpoint'ler:**

- API base: `http://localhost:5178/api/v1`
- Swagger UI: `http://localhost:5178/swagger`
- Health: `http://localhost:5178/health`

---

## 11. Öngörülü Davranış (Foresight) — Anahtar İlke

Her tablo, sorgu, endpoint ve sayfa **"1 yıl sonra burada 10M satır / 100× trafik olursa ne olur?"** testinden geçer. Tasarımı bugünün veri hacmi değil, makul en kötü gelecek hacmi belirler.

1. **Sınırsız sorgu yok.** Liste dönen her query'de zorunlu pagination. Büyüyen/büyük tablolarda `OFFSET` yerine **keyset (seek) pagination** (`WHERE id > @cursor ORDER BY id LIMIT n`). `ToList()` / `Take(int.MaxValue)` ile sınırsız çekim yasak (madde 4.4 pekiştirir).
2. **Filtre/sıralama server-side ve indexli.** "Hepsini çek, frontend'de filtrele" yasak. Filtrelenen/sıralanan her kolonun index'i olur.
3. **N+1 öngörüsü.** Koleksiyon erişiminde baştan projection ya da `Include` + `AsSplitQuery`. "Sonra optimize ederim" yok — ilk yazımda doğru.
4. **Frontend ölçek.** Uzun listelerde virtualization (ör. `@tanstack/react-virtual`). Sonsuz büyüyen client state yok; sayfalı/sanal.
5. **Hot-path öngörüsü.** Çok yazılan/okunan tablolar (audit log, stock ledger, activity) için baştan doğru index; gerekiyorsa partition/archival kararını `docs/INVARIANTS.md`'ye not düş.
6. **Öngörü ≠ over-engineering.** YAGNI ile denge kuralı: _ucuzsa ve geri dönüşü pahalıysa_ baştan yap (index, pagination, doğru tip, tenant filter); _pahalıysa ve belirsizse_ genişleme dikişi (seam) bırak ve nedenini not et, ama spekülatif soyutlama üretme.

---

## 12. DB Yaşam Döngüsü ve Evrim — Tablolar Çürümez

Bir tablo bir istek için doğar; aylar sonra başka yöne evrilebilir. **Evrilen tablo ilk hâlinde bırakılmaz.**

1. **Doğuştan doğru.** Yeni tablo: tüm FK'lere index, sık `WHERE/JOIN/ORDER BY` kolonlarına index, doğru unique constraint'ler, doğru tipler (`decimal(18,4)` para ya da `bigint` minor-unit, `timestamptz`, GUID `id`), bilinçli nullability. Madde 4 standartları **aynı migration'da** karşılanır.
2. **Her şema değişiminde index revizyonu.** Bir tabloyu değiştiren task, o tablonun mevcut index/constraint'lerini **gözden geçirir**; sorgu paterni değiştiyse eski index'i düzeltir veya yenisini ekler. Kural: **"dokunduğun tabloyu bulduğundan daha iyi bırak."**
3. **Index disiplini.** Composite index kolon sırası selectivity'ye göre. Çok okunan dar sorgular için covering index. Soft-delete varsa kısmi index (`WHERE deleted_at_utc IS NULL`). JSONB filtreleniyorsa GIN. Gereksiz/çoğullanan index temizlenir (yazma maliyeti).
4. **Query plan farkındalığı.** >10k satır beklenen yeni/değişen sorguda `EXPLAIN ANALYZE` ile plan kontrol edilir; beklenmeyen seq scan varsa index eklenir. Sonuç PR/özet notuna yazılır.
5. **Zero-downtime migration.** Büyük/canlı tabloda yıkıcı değişiklik adımlanır: önce nullable kolon ekle → backfill (ayrı veri komutu) → constraint/NOT NULL. Tek migration içinde uzun kilit tutan işlem yok. Yıkıcı/geri alınamaz migration → madde 7 onayı.
6. **Standart tutarlılığı.** Her tabloda `created_at_utc`, `updated_at_utc`; soft-delete kullanılıyorsa `deleted_at_utc`. `snake_case` + çoğul tablo (madde 4.1).
7. **Korunaklı (idempotent) migration.** Her migration tekrar-uygulanabilir/çakışmaya dayanıklı yazılır: kolon/tablo/index eklerken `ADD COLUMN IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`; düşürürken `DROP ... IF EXISTS`. Şüpheli durumda `migrationBuilder.Sql("... IF NOT EXISTS ...")` ile guard'la. Böylece snapshot drift, duplicate `AddColumn`, ya da iki ajanın migration'ı birbirini ezse bile uygulama güvenlidir.
8. **O an bitir + uygula — biriktirme.** Bir migration yazıldıysa **aynı turda uygulanır** (`dotnet ef database update` ya da migration'ı lokal/dev DB'ye çalıştır) ve şema reconcile edilir. Uygulanmamış migration `.cs` dosyaları **birikmeye bırakılmaz** — birikmiş/uygulanmamış SQL'ler snapshot ile drift eder ve birbirini ezer (bu projede tekrar tekrar yaşandı). Yarım kalan/uygulanmayan migration teslim edilmez. Apply edilemiyorsa (DB kilitli/erişilemez) → idempotent yaz + nedeni ve "snapshot reconcile" follow-up'ı `docs/sprintN-blockers.md`'ye düş.
9. **Paralel ajan snapshot'ı tutuyorsa.** `CoreAlignDbContextModelSnapshot.cs`/`.csproj` başka ajan tarafından aktif düzenleniyorsa: snapshot'a **dokunma**; el-yazımı **idempotent** migration ekle (madde 12.7), hemen uygula (madde 12.8), ve snapshot'a property/tablo ekleme follow-up'ını blocker'a yaz (yoksa sonraki `migrations add` duplicate üretir).

---

## 13. İlk Seferde Doğru (First-Pass Quality) — Tekrar Döngüsünü Önle

Amaç: kodu baştan mimariye/performansa/kurala uygun yazmak; kullanıcıyı tekrar tekrar test/düzeltme döngüsüne sokmamak.

1. **Önce tara, sonra yaz.** Yeni iş öncesi benzer mevcut feature okunur ve **aynı pattern** uygulanır. Yeni pattern icat etme; tutarlılık > yaratıcılık.
2. **Mimariyi yazarken uygula.** Dependency yönü (FSD/Clean), layer sorumluluğu, DTO/`IQueryable` sızıntısı yok, slim controller — sonradan düzeltilecek borç olarak değil, ilk yazımda doğru.
3. **Performansı baştan koy.** Doğru async/await (sync-over-async yok), read'lerde `AsNoTracking`, projection, N+1 yok, gereksiz materialization yok.
4. **Hata yönetimini baştan koy.** `safeRequest`/middleware sarmalı, exception yutma yok, iş mantığında try/catch yok (madde 2.4 / 3.4).
5. **Bitmeden kendin doğrula.** "Bitti" demeden önce **madde 8 checklist'ini sen koştur** ve gate'leri lokalde çalıştır: `dotnet build` (0 warning), arch testler, `npm run lint` / `typecheck` / `test`. **Yeşil görmeden teslim etme.**
6. **Testi sen çalıştır.** Yazdığın test ve gate'leri sen koştur, yeşil sonucu kullanıcıya sun. Kullanıcı senin yerine manuel test eden kişi değildir — onu döngüye sokma.

---

## 14. Genişletilmiş Test Direktifleri

Madde 8.2 / 8.2.1 geçerli; ek olarak:

1. **Her yeni endpoint → integration test** (gerçek DB): happy path + auth reddi + **tenant izolasyonu** (başka tenant'ın verisi sızmıyor) doğrulanır.
2. **N+1 regression guard.** Kritik liste/detay query'lerinde SQL round-trip sayan test (DbCommand interceptor ile). Round-trip sayısı beklenenden artarsa test kırılır.
3. **Mutation skoru düşmez.** Yeni Application kodu Stryker eşiğinin altına çekemez.
4. **State machine** her geçiş + her reddedilen geçiş için ayrı test (sipariş/fatura/ödeme FSM'leri).
5. **Para/stok mutasyonu** testi: yuvarlama, negatif stok reddi, idempotency (madde 16), eşzamanlı güncelleme çakışması.
6. **Frontend:** yeni zod schema/util → happy + ≥2 failure; kritik etkileşimli component → React Testing Library.

---

## 15. Invariants Log — Kendi Kendini Güncelleyen Kurallar

Prompt'ta unutulsa bile atlanmaması gerekenler kalıcı hafızada tutulur: **`docs/INVARIANTS.md`**. Bu, projenin "bir daha aynı hatayı yapma" defteridir.

1. **Oku (task başında).** Her göreve başlarken `docs/INVARIANTS.md` okunur; ilgili invariant'lar uygulanır.
2. **Ekle (task sonunda).** Tekrar eden bir hata, bir "şunu unutma" durumu veya bir tuzak ortaya çıktığında agent bunu **kendi** ekler. Tek satır: neden + kural.
3. **Format:** `- [ALAN] Durum → Kural`.
   - `- [DB] customers.email aramada full-scan yapıyordu → normalized_email + ix index zorunlu`
   - `- [API] liste endpoint'i pagination'sız döndü → tüm liste endpoint'leri zorunlu sayfalı`
   - `- [TENANT] yeni repo sorgusu global filter'ı bypass etti → her sorgu tenant filter ile doğrulanır`
4. **Kapanış kontrolü.** Görev sonunda agent açıkça sorar: **"Yeni bir invariant öğrendim mi?"** Evetse ekler. Tekrarlayan ihlal kök-neden olarak buraya yazılır — böylece bir daha atlanmaz.
5. **Alan etiketleri:** `[ARCH] [DB] [PERF] [SECURITY] [API] [FRONTEND] [TEST] [I18N] [TENANT] [BUILD]`.
6. **Çatışma çözümü.** Bir invariant CLAUDE.md ile çelişirse CLAUDE.md kazanır; invariant güncellenir veya silinir.

---

## 16. ERP Doğruluk Kuralları (Öngörünün ERP'ye Uygulanışı)

Para ve stok hatası kabul edilemez; eşzamanlılık ve retry senaryoları **baştan** düşünülür.

1. **Optimistic concurrency.** Yarışabilen kayıtlarda (stok, hesap bakiyesi, fatura/ödeme durumu) concurrency token kullanılır — **hangi mekanizma (`IXminConcurrency` / `IHasConcurrencyToken`) ve neden ikisi: §4.6**. Çakışmada **409**, sessiz overwrite yok.
2. **Idempotency.** Para/stok hareketi yaratan komutlar (ödeme uygula, sipariş onayla, stok düş) idempotency key ile çalışır; retry çift kayıt üretmez.
3. **Transaction sınırı.** Çok-tablolu tutarlılık (sipariş + stock ledger + fatura) tek transaction / UnitOfWork içinde; yarım kalmış durum bırakılmaz.
4. **Cache disiplini.** Cache key **tenant-scoped**; cross-tenant okuma imkânsız. Yazma sonrası ilgili key invalidate edilir; stale para/stok gösterilmez. TTL bilinçli seçilir.
5. **Audit.** Para/stok/yetki değişiklikleri audit'lenir (kim, ne zaman, eski → yeni).
6. **Decimal & TZ.** Para her zaman `decimal(18,4)` ya da minor-unit `bigint`; `float`/`double` yasak. Zaman `timestamptz`, UTC saklanır, yalnız sınırda dönüştürülür.

---

## 17. Post-Sprint Validation Routine (Long-Sprint Drift Önleme)

Uzun sprint sonrası (40+ saatlik aralıklı çalışma veya 3+ migration ekleme sonrası), **bir sonraki çalışmaya başlamadan** veya **branch merge öncesi** şu sıralı kontrolü yap. §1.1 migration sanity, §4/§12 DB ve §13.5 gate'leri bu rutini tamamlar.

1. **Migration sanity sweep:** Migration klasöründe duplicate `AddColumn`/`CreateTable`/`CreateIndex` (aynı tablo+sütun / aynı isim) ara — concurrent merge çakışması en sık burada birikir. Bul + canonical = en küçük migration ID kuralıyla temizle (§4.12).
2. **DB tabula rasa testi:** `DROP DATABASE → CREATE → dotnet ef database update` ile tüm zinciri sıfırdan apply et. Build hatası veya `column already/does not exist` çıkarsa hemen düzelt — production deploy'unu önler (§4.2 sıfırdan apply).
3. **DI ValidateOnBuild/Start:** Dev ortamda açık olmalı. API'yi bir kez start et — eksik registration, lifetime mismatch, concrete-not-registered hatalarını tek seferde gör. Hatasız startup şart.
4. **Smoke test ana sayfalar:** Login + Dashboard + Customers + Products + Orders + Reports + Accounting/TrialBalance + GlassEnclosure list'ini UI'dan tıkla. `InvalidOperationException` (LINQ untranslatable), `Document sequence not seeded`, `null violation` görür görmez ticket aç.
5. **Cleanup commit:** `git status`'ta `?? dummy_*`, `?? temp_*` gibi izleme dışı dosya görürsen sil veya `.gitignore`'a ekle (§1 No Scratch Files).
6. **Seed extensibility:** Yeni `DocumentSequenceType` enum değeri eklendiyse `DemoDataSeeder`'a da eklendi mi kontrol et (Customer/Product/Order/Invoice/GlassProject/PurchaseOrder/VendorPayment/Journal/PurchaseRequisition sequence'leri eksiksiz olmalı).

Bu rutin olmadan F5 sonrası "Demo sales flow X failed", "column does not exist", "Document sequence not seeded" gibi rastgele hataları sırayla debug etmek zorunda kalınır — saatler kaybedilir. **Rutini atlamak = teknik borcu ertelemek.**

---

## 18. Claude Code Çalışma Düzeni (bu repo)

CoreAlign'a özel Claude Code varlıkları `.claude/` altında, **repoyla taşınır** (detay: `.claude/README.md`).

### 18.1 Otomatik yüklenen bağlam

- **`CLAUDE.md`** (bu dosya) + **`@docs/INVARIANTS.md`** (üstteki import) her oturumda otomatik yüklenir. Başka hiçbir doküman otomatik yüklenmez — §0.1 indeksindeki "önce oku"ları **sen açarsın**.
- `.claude/memory/` Claude Code tarafından instruction olarak **otomatik yüklenmez**; canlı memory store'un git-aynasıdır (senkron: `.claude/README.md`). Kalıcı kural buraya değil, CLAUDE.md/INVARIANTS'a yazılır.

### 18.2 Slash komutları (`.claude/commands/`)

- **`/pre-ship`** — "bitti" demeden önce ZORUNLU: build (0 warning) + lint + typecheck + ilgili testler + (dokunulduysa) tabula-rasa migration testi. §13.5/§8.
- **`/new-endpoint`** — yeni endpoint checklist'i (slim controller → Application → validator → tenant filter → cross-tenant + N+1 testi → i18n). §3/§6/§14.
- **`/new-module`** — FSD + CQRS iskeleti (doğru yüzey + TenantEntity + repo + handler + DTO + UI + tr/en).
- **`/db-migration`** — migration üretme protokolü (ileri-tarihli ID, idempotent, aynı-tur uygula, snapshot drift, tabula-rasa). §4.2/§4.12/§17.

### 18.3 Reviewer subagent (`.claude/agents/`)

- **`corealign-reviewer`** — değişiklik sonrası §8 checklist'ini bağımsız uygular (yorum/console/`@ts-ignore`, t() + tr/en senkron, N+1/sınırsız query, tenant filter + `[Authorize]`, para tipi/concurrency, DTO sızıntısı, dark+responsive). "Bitti" demeden önce çalıştır; bulduklarını düzelt.

### 18.4 Güvenlik izinleri (`.claude/settings.json`)

- Yıkıcı komutlar (**`DROP DATABASE`**, `git push --force`, `rm -rf`, `ef database update/drop` prod'a) **onay (`ask`) ister** — §7 risk-eylem kuralını mekanik olarak zorlar. Onaysız çalıştırma; kullanıcıya neden gerektiğini söyle.

### 18.5 Hooks

- Düzenleme sonrası ilgili dosyada prettier/eslint çalışır (lint debt birikmesini önler). Hook'un raporladığı hatayı **geçiştirme** (§1.2) — kök neden düzelt.

### 18.6 Paralel/büyük iş

- Çok-dosyalı tekrarlı dönüşümlerde (codemod, toplu sayfa migration'ı) izole subagent/worktree kullan; her batch'i ayrı doğrula. Snapshot/migration tutan başka ajan varsa §12.9 (el-yazımı idempotent + dokunma) uygula.
