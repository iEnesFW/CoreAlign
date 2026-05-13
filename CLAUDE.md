# CoreAlign — Proje Kuralları

> Bu dosya, CoreAlign üzerinde yapılan **her** değişiklikte (kod yazma, refaktör, dosyalama, DB, dokümantasyon) bağlayıcıdır. Aşağıdaki kurallar **non-negotiable** kabul edilir; istisna gerekiyorsa açıkça istemden geçer.

## 0. Proje Kimliği

- **Tip:** Web ERP (multi-tenant, abonelik tabanlı).
- **Stack:** React 19 + Vite 7 + TypeScript + Tailwind v4 (frontend) · .NET 10 + EF Core + PostgreSQL (backend) · JWT (access + refresh) auth.
- **Mimarî:** Frontend = Feature-Sliced Design (FSD). Backend = Clean Architecture + CQRS (Application katmanı).

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
- **Tek doğru i18n yolu** vardır: `src/app/i18n/`. `src/shared/lib/i18n/` duplicate — refaktör fırsatında silinir, _yeni kod_ sadece `app/i18n` ile çalışır.

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

- **`try/catch` doğrudan kullanılmaz.** `shared/lib/safeRequest.ts` sarmalı kullanılır:
  - `safeRequest`: `[data, error]` tuple döner, sessiz yakalar.
  - `safeRequestWithNotify`: Toast gösterir (success/error).
  - `safeBatchRequest`: Promise.all sarmalı.
- Eğer bu sarmal henüz yoksa, **ilk ihtiyaçta** kurulur ve standartlaştırılır.
- API hata gövdesi backend `ErrorResponse` şemasına (madde 3.4) göre parse edilir.

### 2.5 i18n Disiplini

- Key format: `[Sayfa/Modül].[İçerik]` — örn. `Login.Title`, `Common.Save`, `Validation.Required`.
- Eklenen her anahtar **tr.json** ve **en.json**'da aynı anda var olur, alfabetik gruplanır.
- Çoğul/cinsiyet/format için `t("Key", { count, name })` ile interpolation kullanılır.

### 2.6 Logging

- `src/shared/lib/logger.ts`: dev'de console, prod'da no-op (veya remote sink). `logger.info/warn/error`.
- `apiClient` interceptor'lerindeki mevcut `console.log/error` çağrıları **logger ile değiştirilir** (mevcut borç).

### 2.7 TypeScript

- `strict: true` zorunlu. `any` kullanmak için somut gerekçe lazım — alternatif `unknown` + narrowing.
- DTO/Model tipleri **`src/shared/model/`** veya feature'a aitse `features/<x>/model/types.ts`.
- Backend ile şema uyumu manuel tutulmaz — uzun vadede OpenAPI tabanlı tip üretimine geçilir (NSwag/orval). Yeni endpoint eklenirken tip eşleşmesi kontrol edilir.

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

### 3.4 Global Hata Yönetimi

- `CoreAlign.API/Middleware/ExceptionHandlingMiddleware.cs` tek noktadır. Tüm exception'lar buradan çıkar.
- Standart hata gövdesi:
  ```json
  { "error": { "code": "VALIDATION_FAILED", "message": "...", "details": [...], "traceId": "..." } }
  ```
- Domain/Application'da `throw new <SpecificException>(message)` — middleware HTTP status'a map eder (Validation→400, NotFound→404, Forbidden→403, Conflict→409, Unauthorized→401, internal→500).
- **Try/catch sadece sınırlarda.** İş mantığında yutmak yasak.

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

---

## 4. Database

### 4.1 İsimlendirme (basit ve anlaşılır)

- **Tablo adı:** `snake_case`, **çoğul**. Örn: `users`, `user_roles`, `subscription_plans`, `login_audit_logs`.
- **Sütun adı:** `snake_case`. Entity property suffix'i `*AtUtc` ise sütun `*_at_utc` olur. Örn: `created_at_utc`, `last_login_at_utc`, `user_id`.
- **Primary key:** `id` (Guid).
- **Foreign key:** `<entity>_id`. Örn: `user_id`, `role_id`.
- **Timestamp standardı:** Her tablo `created_at_utc`, `updated_at_utc` (PG `timestamp with time zone`). Soft-delete kullanılıyorsa `deleted_at_utc`.
- **Indeks adı:** EF default + global snake_case dönüşümü. Örn: `ix_users_normalized_email`, `pk_users`, `fk_user_roles_users_user_id`.

C# Entity sınıfı **PascalCase** (`User.CreatedAtUtc`), DbContext içinde `ApplySnakeCaseNaming()` çağrısı tüm tablo / sütun / FK / indeks adlarını otomatik snake*case'e dönüştürür. EF Configuration'larda artık `ToTable("Users")` veya `HasDatabaseName("IX*...")` yazılmaz — convention yapar.

### 4.2 Migrations

- Migration adı: `YYYYMMDDHHMMSS_<purpose>.cs` — EF zaten verir, dosya açıklayıcı isim taşır (`AddSubscriptionStatusColumn`).
- **Production'a giden migration silinmez** (tarihçe). Geliştirme aşamasında silinmesi serbest; mevcut işsiz migration temizlenir, yenisi `InitialSchema` adıyla kurulur.
- Migration **veri** içermez (lookup seed hariç). Veri taşıması ayrı bir command.
- Migration EF Core convention'a uygun **idempotent** olmalı; her ortamda yeniden çalıştırılabilir.

### 4.3 Bütünlük

- FK her zaman bildirilir; `OnDelete` davranışı **bilinçli** seçilir (default Restrict, junction tablolarında Cascade).
- `nullable` sütunlar nedeni anlaşılır olmalı; her opsiyonellik bir kararın sonucu.
- Para birimi `decimal(18,4)` veya minor unit `bigint`.

### 4.4 Performans

- Sorgu indeksini öngör: where/join/order alanlarına indeks. Indeks olmadan tabloya `>10k` satır beklenen sorgu yazılmaz.
- Paging zorunlu. `Take(int.MaxValue)` ve sınırsız `ToList()` yok.

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

**Konum:** `server/tests/CoreAlign.Application.Tests/`
**Stack:** xUnit + NSubstitute + FluentAssertions. `TreatWarningsAsErrors` + global using'ler (Xunit, NSubstitute, FluentAssertions) csproj'de bind.

**Kapsam:**

- **Unit testleri** Application/handler katmanına odaklanır. Repository ve UnitOfWork `Substitute.For<>()` ile mock'lanır.
- Test dosya yolu = mirror: `Application/Orders/Handlers/X.cs` → `Application.Tests/Orders/XTests.cs`
- Test isimleri: `Verb_condition_expected_result` (snake) — `Confirming_draft_order_with_sufficient_stock_decrements_product_stock`.
- Her test arrange/act/assert ayrı; helper'lar dosya altında private static.
- **State machine** ve **business rule** branch'lerinin her biri en az bir test → kritik regression sigortası.

**Yazılması gereken testler:** yeni handler eklediğinde, en az happy path + 1 failure path. State machine bulunan handler'larda her geçiş ayrı test.

**Komut:** `dotnet test server/tests/CoreAlign.Application.Tests`

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

## 9. Foundation Durumu (2026-05-12)

**Tamamlanmış altyapı:**

- Backend: Clean Architecture + CQRS (MediatR), FluentValidation pipeline, `ExceptionHandlingMiddleware` (traceId + standart envelope), `JwtOptions` + DataAnnotations validation (`ValidateOnStart`), `CorsOptions` validated, snake_case naming convention (`ApplySnakeCaseNaming()` extension).
- Serilog — console + günlük rolling file (`logs/corealign-YYYYMMDD.log`).
- API versioning — `/api/v1/...` (URL segment reader).
- Swagger UI Bearer auth desteği (Authorize butonu).
- Rate limiter — IP-bazlı fixed window: `auth` policy (10/dk, login/register/forgot/reset için) + `global` policy (200/dk).
- Health check — `/health` (PostgreSQL kontrolü dahil).
- CORS — `WithOrigins` whitelist; Authorization+ContentType+Accept header ile sınırlı.
- AutoMapper kaldırıldı (CVE'li sürüm + kullanılmıyordu).
- Tek `InitialSchema` migration — snake_case tablolar (`users`, `user_roles`, `refresh_tokens`, ...).
- Frontend: `shared/types/api.ts` (ApiResponse/ApiError tipler), `shared/lib/{logger,env,safeRequest,mutationToast}`, `apiClient` (`/api/v1` base, console.log temizlendi, logger entegre), tek `app/i18n` (duplicate silindi), `ErrorBoundary`, `AppToaster` (sonner), `LoginForm` (react-hook-form + zod + i18n validation key'leri).
- Tooling: Prettier + ESLint (prettier rule), Husky pre-commit + lint-staged, `npm run typecheck/lint/format`.
- Docker Compose — `docker compose up -d` ile PostgreSQL 17.

**TreatWarningsAsErrors** her backend csproj'de açık, lint `--max-warnings=0` — sıfır toleranslı CI.

**Multi-tenant + İlk ERP modülü:** `Customer` entity (`TenantEntity` türevli) — pattern'in canlı kanıtı. Backend: Repository + CQRS handlers + Controller + Validator. Frontend: `features/customers/{api,hooks,model,ui}` + `pages/customers/CustomersPage` + RHF+zod form + sonner toast + Tailwind responsive table.

**Ertelenmiş işler (server gerekli):**

- NSwag / openapi-typescript — backend swagger.json'dan frontend TS client üretimi. Server `dotnet run` çalışıyorken kurulabilir.
- `docker compose up -d` ile Postgres yerine yerel PG 18 kullanılabilir; `appsettings.json` connection string'i revize edilebilir.

---

## 10. Komutlar / Hızlı Referans

```bash
# PostgreSQL (ilk açılış)
docker compose up -d

# Backend
dotnet build server/src/CoreAlign.API/CoreAlign.API.csproj
dotnet ef migrations add <Name> -p server/src/CoreAlign.Infrastructure -s server/src/CoreAlign.API -o Persistence/Migrations
dotnet ef database update -p server/src/CoreAlign.Infrastructure -s server/src/CoreAlign.API

# Tests (xUnit + NSubstitute + FluentAssertions)
dotnet test server/tests/CoreAlign.Application.Tests

# Frontend
npm run dev          # http://localhost:5273
npm run build
npm run typecheck    # tsc -b --noEmit
npm run lint         # 0 warning zorunlu
npm run lint:fix
npm run format       # prettier write
npm run format:check
npm run test         # Vitest (frontend unit tests)
npm run test:watch
npm run test:coverage

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
