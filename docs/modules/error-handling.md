# Error Handling & Observability — Geliştirici Rehberi

> Bağlayıcı kural özeti `CLAUDE.md` §2.4 + §3.4'tedir. Bu doküman o kuralların **nasıl** uygulandığını, kod kalıplarını ve "kullanıcı hata aldı" araştırma akışını anlatır. Yeni API/feature yazarken bu kalıpların dışına çıkma.

CoreAlign'da hata yönetimi tek bir sözleşme etrafında kurulur: **her yanıt `ApiResponse<T>` zarfıdır, her hata bir exception'dır, her exception tek bir middleware'den geçer, her kullanıcı-görür hata `error_logs` tablosuna düşer ve hepsi tek bir correlation id ile birbirine bağlanır.** Aşağıdaki kuralları izlersen yeni endpoint için ekstra hata-yönetimi kodu yazmana gerek kalmaz — mekanizma seni kapsar.

---

## 1. Yanıt sözleşmesi — `ApiResponse<T>`

`CoreAlign.Application.Common.ApiResponse<T>` her HTTP yanıtının zarfıdır (başarı **ve** hata):

```json
{
  "isSuccess": false,
  "data": null,
  "errors": ["A record with the same unique value already exists."],
  "fieldErrors": { "Email": ["Email is required."] },
  "statusCode": 409,
  "traceId": "3f9a1c2e8b7d4f10a2c5e6b7d8f90123"
}
```

| Alan          | Anlamı                                                               |
| ------------- | -------------------------------------------------------------------- |
| `isSuccess`   | İşlem başarılı mı                                                    |
| `data`        | Başarı yükü (`T`); hata varken `null` (serileştirmede atlanır)       |
| `errors`      | İnsan-okur hata mesaj(lar)ı                                          |
| `fieldErrors` | Alan-bazlı doğrulama hataları (yalnız `ValidationException`'da dolu) |
| `statusCode`  | HTTP durum kodu (gövdede de tekrarlanır)                             |
| `traceId`     | İstek correlation id'si — **her** yanıtta bulunur (§4)               |

- `ApiResponse<T>` `ITraceableResponse` implement eder; `traceId` başarı yolunda `CorrelationResultFilter` (`CoreAlign.API/Common/CorrelationResultFilter.cs`), hata yolunda `ExceptionHandlingMiddleware` tarafından doldurulur.
- **Eski `{ "error": { code, message, details } }` şekli artık yoktur** — yeni kodda kullanılmaz, frontend ona göre parse etmez.
- Başarılı yanıt fabrikası: `ApiResponse<T>.Success(data)` / `ApiResponse<T>.Success(data, 201)`. Manuel hata zarfı (controller sınırında, ör. erken model-bind reddi): `ApiResponse<object>.Failure("mesaj", 400)`.

---

## 2. Backend — hatayı nasıl bildirirsin

### 2.1 Altın kural: status kodu değil, exception fırlat

İş mantığında hatayı **exception** ile bildir; HTTP status'unu **kodlamazsın**. `ExceptionHandlingMiddleware` (`CoreAlign.API/Middleware/`) tek çıkış noktasıdır ve exception'ı status + zarfa map eder.

```csharp
public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken ct)
{
    var customer = await _customers.GetByIdAsync(request.Id, ct);
    if (customer is null)
        throw new CustomerNotFoundException(request.Id);

    return _mapper.ToDto(customer);
}
```

### 2.2 Exception → HTTP eşlemesi

Soyut tabandan türeyen her domain exception'ı middleware otomatik map eder (`CoreAlign.Domain/Exceptions/`):

| Taban sınıf                                     | HTTP    | Notlar                                    |
| ----------------------------------------------- | ------- | ----------------------------------------- |
| `NotFoundException`                             | **404** | Capture edilmez (yüksek hacim, beklenen)  |
| `ConflictException`                             | **409** |                                           |
| `ForbiddenException`                            | **403** |                                           |
| `AuthenticationException`                       | **401** | Capture edilmez                           |
| `RateLimitExceededException`                    | **429** | `Exceptions/ForwardExceptions.cs`         |
| `DomainException` (diğer her şey)               | **400** |                                           |
| FluentValidation `ValidationException`          | **400** | `fieldErrors` doldurulur; capture edilmez |
| `UnauthorizedAccessException`                   | **401** |                                           |
| `DbUpdateException` 23505 (unique) / 23503 (FK) | **409** | Generic güvenli mesaj                     |
| `DbUpdateConcurrencyException`                  | **409** | "Reload and retry"                        |
| (eşleşmeyen tüm exception'lar)                  | **500** | Gövde **generic**, detay sızmaz           |

### 2.3 Yeni exception tipi ekleme

Yeni bir hata durumu = uygun **soyut tabandan türet**. `switch` koluna dokunma; mapping + capture otomatik gelir.

```csharp
namespace CoreAlign.Domain.Exceptions;

public sealed class CustomerNotFoundException : NotFoundException
{
    public CustomerNotFoundException(Guid id)
        : base($"Customer {id} was not found.") { }
}
```

- Mesajı **kullanıcıya gösterilebilir** ve **PII-içermez** yaz (mesaj client'a 4xx'te gider). Gizli/iç detayı mesaja koyma.
- Parametre alan exception'larda (id, durum) mesajı sabit şablonla kur; serbest kullanıcı girdisini ham gömme.

### 2.4 Yasaklar

- **İş mantığında `try/catch` ile yutma yok.** Exception'ı middleware'e bırak. (`catch (Exception) { return null; }` = yasak.)
- **Controller'da `catch → return new { error = ex.Message }` yok.** Bu hem sözleşmeyi bozar hem 500 detayını sızdırır. Controller slim kalır (CLAUDE.md §3.3), exception yukarı akar.
- **`ex.Message` / `ex.ToString()` response gövdesine konmaz.** 500'de client her zaman generic mesaj görür; tam detay yalnız log + `error_logs`'a gider. (Tek bilinçli istisna: admin-only + sandbox-only diagnostik endpoint'ler — `ProviderHealthController`/`ProviderTestRunnerController`. Yeni kodda taklit etme.)
- **Manuel `StatusCode(500, ...)` / `BadRequest(ex)` yok.** Status'u exception tipi belirler.

---

## 3. DB error log — kalıcı kayıt (`error_logs`)

Kullanıcı "şu sayfada hata aldım" dediğinde admin'in DB'den analiz edebilmesi için **her 5xx + anlamlı 4xx** `error_logs` tablosuna yazılır. Bu mekanizmanın merkezidir.

- **Entity:** `CoreAlign.Domain/Entities/Observability/ErrorLogEntry` (`: BaseEntity` — **tenant query-filter'a tabi değildir**, ki PlatformAdmin tüm tenant'ların hatasını görebilsin).
- **Yazıcı:** `IErrorLogWriter` → `ErrorLogWriter` (singleton). Kendi DI scope'unu açar, **asla throw etmez**, alanları truncate eder, 5 sn linked-CTS timeout uygular, OTel sayaçlarını artırır. Hata-yazma yolu isteği asla bozmaz.
- **Ne yazılır:** correlationId, traceId, occurredAtUtc, source (Backend/Frontend), severity (Error/Warning/Info), statusCode, httpMethod, path, exceptionType, message (tam — iç), stackTrace (`ex.ToString()` — iç), tenantId?, userId?, userName, **clientPage**, **clientComponent**, userAgent, contextJson, isResolved/resolutionNotes.
- **Ne capture edilir** (`ExceptionHandlingMiddleware.ShouldCapture`): 5xx **her zaman** (Severity=Error); 4xx **Warning** olarak — ancak `ValidationException`, `AuthenticationException`, `NotFoundException` ve 401/404 **hariç** (yüksek hacimli, beklenen gürültü). Yeni endpoint için ayar gerekmez; middleware'den geçmesi yeterli.

### Kullanıcı hata bildirdiğinde — araştırma akışı

1. Kullanıcıdan **zamanı + sayfayı** (mümkünse ekran görüntüsündeki `traceId`'yi) al.
2. Admin UI: **Error Logs** sayfası (`pages/admin/ErrorLogsPage.tsx`) → tarih + sayfa + severity ile filtrele; ya da `GET /api/v1/admin/error-logs`.
3. `traceId` biliniyorsa doğrudan onunla ara — aynı id loglarda, Sentry'de ve yanıt gövdesinde de geçer (§4).
4. Detay modalinde `exceptionType`, `message`, `stackTrace`, `path`, `clientPage`, `contextJson`, `userName` görünür.
5. Çözülünce `Resolve` ile `isResolved=true` + not yaz (`ResolveErrorLogCommand`). Çözülen + 90 günü geçen kayıtlar retention job ile silinir (§6).

> Doğrudan DB sorgusu (acil durum): `select occurred_at_utc, status_code, path, exception_type, message, correlation_id from error_logs where correlation_id = '<id>' order by occurred_at_utc desc;`

---

## 4. Correlation — tek id, uçtan uca

Bir isteğin tüm izleri **tek** correlation id ile bağlanır. `CorrelationIdMiddleware` (`CoreAlign.API/Middleware/`):

- Gelen `X-Correlation-Id` header'ını kullanır; yoksa/geçersizse yeni Guid (`"N"` format) üretir.
- Yanıta `X-Correlation-Id` header'ını yazar.
- `Activity.Current` tag + baggage (`correlation_id` / `correlation.id`), Sentry scope tag ve Serilog `LogContext`'e basar.
- `HttpContext.Items[CorrelationIdMiddleware.ItemsKey]`'e koyar; `ExceptionHandlingMiddleware` ve `CorrelationResultFilter` buradan okuyup `traceId`'yi gövdeye yazar.

Sonuç: **aynı id** → yanıt gövdesi `traceId` = `X-Correlation-Id` header = `error_logs.correlation_id` = Serilog `CorrelationId` = Sentry `correlation_id` tag.

**Bu zinciri kırma:** yeni bir custom response tipi dönüyorsan `ApiResponse<T>` kullan (veya en azından `ITraceableResponse` implement et) ki başarı yolunda da `traceId` dolsun. Pipeline sırası sabittir: `CorrelationId → ExceptionHandling → ...` (CLAUDE.md §3.5) — ExceptionHandling correlation'dan **sonra** gelir ki traceId hazır olsun.

---

## 5. Frontend — hata yakalama ve raporlama

### 5.1 API çağrıları — `safeRequest` ailesi

Component'te ham `try/catch` yok. `src/shared/lib/safeRequest.ts`:

| Fonksiyon                                                 | Davranış                         |
| --------------------------------------------------------- | -------------------------------- |
| `safeRequest(promise)`                                    | `[data, error]` tuple, sessiz    |
| `safeRequestWithNotify(promise, { successMessage, ... })` | Toast (success/error) gösterir   |
| `safeBatchRequest([...])`                                 | Paralel istekler, tek tuple      |
| `safeBatchRequestSettled([...])`                          | `{ results, allOk, firstError }` |

`apiClient` interceptor'ı `ApiResponse` zarfını çözer: `isSuccess === false` ise normalize edilmiş hata (mesaj + status + traceId) fırlatır; `safeRequest*` bunu yakalar. Yeni endpoint cache'lenecekse `httpCache.ts` `TTL_RULES`'a regex ekle (CLAUDE.md caching kuralı).

### 5.2 Correlation (her uygulama)

`apiClient` her isteğe `X-Correlation-Id` ekler ve yanıttaki id'yi saklar (`getLastCorrelationId()`). Hata raporları bu id'yi taşır → frontend hatası backend isteğine bağlanır.

### 5.3 Beklenmeyen hata yakalama (zorunlu, 3 uygulama)

Her SPA önyüklemede global capture kurar + kökü `<ErrorBoundary>` ile sarar:

- **Root admin SPA** (`src/`): `shared/errors/windowHandlers.ts` → `installWindowErrorHandlers()` (dedupe + parse + toast + `reportClientError`). `main.tsx`'te çağrılır.
- **Portallar** (`apps/customer-portal`, `apps/b2b`): `shared/lib/clientErrorReporter.ts` → `installGlobalErrorReporting()`. `main.tsx`'te çağrılır + `<ErrorBoundary>` sarması.

Üçü de `window.onerror` + `unhandledrejection` + React render hatasını (`componentDidCatch`) yakalar ve `reportClientError(...)` ile **`POST /api/v1/client-errors`**'a yollar. `reportClientError` **asla throw etmez**, throttle'lıdır (≤1/sn) ve `correlationId`'yi otomatik ekler.

> Not: root SPA `windowHandlers` (zengin pipeline) kullanır; portallarda o pipeline yok, bu yüzden kendi `installGlobalErrorReporting`'lerini korurlar. Root'ta `installGlobalErrorReporting` **kullanılmaz** (silindi) — yeni global handler eklemen gerekmez.

### 5.4 İstemci hata sözleşmesi (`POST /api/v1/client-errors`)

`[AllowAnonymous]` + `[EnableRateLimiting("client-errors")]` (20/dk). Gövde (`ClientErrorReportBody`):

```jsonc
{
  "message": "TypeError: x is undefined",
  "severity": "Error", // Error | Warning | Info
  "page": "/customers/123",
  "component": "ErrorBoundary",
  "stackTrace": "...",
  "correlationId": "....", // getLastCorrelationId()
  "contextJson": "{...}",
}
```

Backend `ReportClientErrorCommand` ile aynı `error_logs` tablosuna `Source=Frontend` olarak yazar; tenant/user bağlamı varsa context'ten okunur.

---

## 6. Gözlemlenebilirlik — metrics, retention, rate limit

- **OTel sayaçları** (`CoreAlign.Infrastructure/Observability/ErrorLogMetrics`): `errorlog_persisted_total{severity,source}` ve `errorlog_write_failed_total`. `ErrorLogWriter` başarı + hata yolunda artırır.
- **Pipeline `Program.cs`'te bağlı:** `AddCoreAlignOpenTelemetry(builder)` (kayıt) + `app.UseOpenTelemetryPrometheusScrapingEndpoint()` (`/metrics`, `OpenTelemetry:MetricsEnabled` guard'lı, auth'tan önce). Yapılandırma `OpenTelemetryConfig.cs`.
  - **Güvenlik:** `/metrics` auth'suzdur (Prometheus pull tasarımı) → **ingress/network ACL ile kısıtla**. Public bırakma.
- **Retention:** `ErrorLogRetentionJob` (Hangfire, günlük 04:00) çözülmüş + 90 günü geçen kayıtları siler (`DeleteOlderThanAsync`).
- **Sentry:** `SentryStartupExtensions.AddCoreAlignSentry` — DSN config'ten (boş = kapalı), `BeforeSend` PII scrubber.

---

## 7. Çek-listeler

### Yeni backend endpoint

- [ ] Hata durumları **exception** ile (uygun soyut tabandan türeyen domain exception) bildiriliyor; manuel status yok.
- [ ] Controller slim; `try/catch` yok; `ex.Message` response'a sızmıyor.
- [ ] Beklenen 4xx mesajları kullanıcı-görür + PII-siz.
- [ ] Başarı yanıtı `ApiResponse<T>.Success(...)` (veya `ITraceableResponse`) → `traceId` dolu.
- [ ] Yeni exception gerekiyorsa `CoreAlign.Domain/Exceptions/` altında, doğru tabandan; `switch`'e dokunulmadı.
- [ ] (Doğrulama) FluentValidation validator'ı var (INVARIANTS [TEST]).

### Yeni frontend feature

- [ ] API erişimi `features/<x>/api` + `useQuery/useMutation`; component'te `try/catch` yok; `safeRequest*` kullanıldı.
- [ ] Hata mesajları `t("NS.Key")` (tr+en); `console.*` yok → `logger`.
- [ ] Yeni app/route eklenmediği sürece global capture + ErrorBoundary zaten kurulu (yenisini kurma).
- [ ] Yeni bir SPA eklediysen: `main.tsx`'te global capture install + `<ErrorBoundary>` sarma + `apiClient` correlation header'ı kuruldu.

---

## 8. Dosya haritası

| Katman         | Dosya                                                                                                                                                                                  |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Zarf           | `CoreAlign.Application/Common/ApiResponse.cs` (`ITraceableResponse`)                                                                                                                   |
| Exceptions     | `CoreAlign.Domain/Exceptions/DomainExceptions.cs`, `ForwardExceptions.cs`, `ObservabilityExceptions.cs`                                                                                |
| Middleware     | `CoreAlign.API/Middleware/{ExceptionHandlingMiddleware,CorrelationIdMiddleware}.cs`                                                                                                    |
| Başarı traceId | `CoreAlign.API/Common/CorrelationResultFilter.cs`                                                                                                                                      |
| DB error log   | `Domain/Entities/Observability/ErrorLogEntry.cs`, `Infrastructure/Observability/ErrorLogWriter.cs`, `Infrastructure/Repositories/ErrorLogRepository.cs`                                |
| Client report  | `CoreAlign.API/Controllers/ClientErrorsController.cs`, `Application/Observability/ReportClientErrorCommand.cs`                                                                         |
| Admin          | `CoreAlign.API/Controllers/Admin/ErrorLogsController.cs`, `src/pages/admin/ErrorLogsPage.tsx`                                                                                          |
| Observability  | `CoreAlign.API/Observability/{OpenTelemetryConfig,SentryStartupExtensions}.cs`, `Infrastructure/Observability/ErrorLogMetrics.cs`, `Application/Jobs/ErrorLogRetentionJob.cs`          |
| Frontend       | `src/shared/lib/safeRequest.ts`, `src/shared/errors/windowHandlers.ts`, `{src,apps/*/src}/shared/lib/clientErrorReporter.ts`, `shared/ui/ErrorBoundary.tsx`, `shared/api/apiClient.ts` |
