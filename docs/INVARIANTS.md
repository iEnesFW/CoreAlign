# CoreAlign — Invariants Log

> "Bir daha aynı hatayı yapma" defteri. CLAUDE.md madde 15 bağlayıcı:
> her task başında oku, task sonunda yeni invariant çıktıysa ekle.
> Format: `- [ALAN] Durum → Kural`
> Alanlar: [ARCH] [DB] [PERF] [SECURITY] [API] [FRONTEND] [TEST] [I18N] [TENANT] [BUILD]

## Aktif invariants

- [I18N] Görünen metin t() dışında bırakıldı → tüm string `t("NS.Key")`; tr.json + en.json senkron.
- [FRONTEND] console.\* kullanıldı → `shared/lib/logger` kullan; prod'da no-op.
- [TENANT] Repo sorgusu global tenant filtresini bypass etti → her sorgu tenant filter ile; auth lookup'ta `IgnoreQueryFilters` bilinçli.
- [SECURITY] Refresh token JS'e açıldı → httpOnly cookie; response body'den RefreshToken silinir.
- [DB] Tablo/sütun snake_case+plural değildi → convention; para `decimal(18,4)`/minor-unit; zaman `timestamptz` (UTC).
- [API] Liste endpoint'i pagination'sız döndü → tüm liste endpoint'leri zorunlu sayfalı (büyük tabloda keyset).
- [ARCH] Controller'a iş mantığı sızdı → slim controller; mantık Application handler'a.
- [PERF] N+1 / sınırsız query → projection veya `Include`+`AsSplitQuery` + paging; sınırsız `ToList()` yasak.

## Yeni invariants (buraya ekle)

- [TEST] Her Command/Query record paired FluentValidation validator olmalı → kayıt eklerken aynı PR'da `*Validator.cs` + en az 2 birim test (red + green) ekle.
- [API] Money/stock-mutating Command'lar pozitif-non-zero amount validate etmeli (handler'a düşmeden) → validator katmanında `GreaterThan(0m)` zorunlu, error key `Validation.QuantityMustBePositive` / `Validation.AmountMustBePositive`.
- [API] State-machine geçişleri exception fırlatıyor → aggregate'ler `Ensure*` veya `EnsureTransitionAllowed` ile self-guard etmeli; test her transition ve her reddedilen transition için ayrı `[Fact]`.
- [ARCH] Aggregate constructor'ları UTC normalizasyonu yapmıyor → her aggregate `DateTime.SpecifyKind(value, DateTimeKind.Utc)` ile incoming tarihleri sabitlemeli; aksi halde EF Core PostgreSQL `timestamptz` ile mismatch atar.
- [ARCH] Çoklu tablo reassignment (ör. müşteri merge) çalıştırıldı → `IUnitOfWork.BeginTransactionAsync` ile tek transaction; her hedef tabloda `ExecuteUpdateAsync` (ChangeTracker bypass), commit/rollback handler'ında.
- [API] Para/stok/durum-mutasyonu yapan komut tekrar oynatıldı (network retry) → komut başlığında `OperationId` (Guid client tarafından üretilir); handler önce `GetByOperationIdAsync` ile idempotency kaydını arar, varsa aynı sonucu döner, source/target eşleşmezse 409.
- [API] Liste endpoint'i ile çakışan path için yeni sub-route eklenirken `{id:guid}` yönlendirici ile çakışmamalı → fixed-path route (örn. `customers/merge`) `{id:guid}` parametresinden önce tanımlanabilir; route constraint yeterli ayrımı sağlar.
- [DB] Snapshot tek bir paralel ajan tarafından korunuyor → hand-authored migration eklenip snapshot/csproj/`OnModelCreating` değiştirilmeden bırakılır; takip notu `docs/sprint10-blockers.md`'e yazılır.
- [SECURITY] Upload endpoint sadece Content-Type'a güveniyordu → her dosya yükleme handler'ı magic-byte sniff (LooksLikeImageAsync / LooksLikeLogoAsync) + extension+content-type cross-check + MaxBytes guard zincirini sırayla çalıştırır; SVG için `<?xml` veya `<svg` prefix kontrolü, raster için byte signature.
- [API] Body cap'leri tek noktadan ayarlanıyordu → Kestrel global limit (MaxRequestBodySize, default 30 MB) generic upper-bound; her endpoint kendi `[RequestSizeLimit]` ile daha sıkı limit dayatır (logo 1 MB, product image 5 MB); FileStorageOptions.MaxBytesPerFile Kestrel ile aynı/eşdeğer olmalı (drift = 413 yerine 500).
- [DB] Stok/balance tutan TenantEntity concurrency-token'sız bırakıldı → `IHasConcurrencyToken` implement et (long ConcurrencyToken, `IsConcurrencyToken()`, default 0L); pipeline `ConcurrencyTokenBehavior` 409 DomainConcurrencyException üretir.
- [API] Multipart upload formdata gönderirken `RequestSizeLimit` controller seviyesinde belirlenir → metin alanları için lazy stream, Length kontrolü payload reach handler etmeden önce reject; `await using var stream = file.OpenReadStream()` ile dispose garantisi.
- [API] Bütün controller yanıtları `ApiResponse<T>` ile sarılır (`{ isSuccess, data, errors, statusCode }`) → harici test/CLI/k6 araçları payload alanlarına `data.<field>` ile erişmek zorunda; yanıt sözleşmesi değiştirilirse yan etki olarak load-test ve istemci sözleşmeleri güncellenir.
- [TEST] N+1 regresyonu için EF Core round-trip-sayıcı devrede → `DbCommandRoundTripInterceptor` + `IDbContextOptionsConfiguration<CoreAlignDbContext>` test factory'de kayıtlı; her yeni liste/detay endpoint'i için integration test `BeginScope()` ile sayıcı açıp `counter.Total ≤ budget` assert etmeli (örnek bütçe: Customers 6, Products 6, Orders 8 — `Include` arttıkça büyütülür ama explicit assert ile dokümanlanır).
- [ARCH] Müşteri merge sonrası target.CurrentBalance stale kalıyordu → ledger reassignment'tan SONRA `_ledger.GetCurrentBalanceAsync(target.Id)` çağrılıp `target.RecalculateBalance` ile yeniden yazılmalı; aynı zamanda source.RecalculateBalance(0,0) ile sıfırlanmalı.
- [API] Müşteri ekstresi gibi büyük ledger listeleri pagination cap'i ile silently truncate ediliyordu → handler önce `CountByCustomerAsync` ile range size kontrol etmeli, MaxLines'ı aşarsa 409 fırlatmalı; opening balance pagination yerine `GetBalanceAsOfAsync` GROUP BY SUM ile hesaplanmalı.
- [PERF] Bulk preference/setting upsert handler `foreach` içinde `GetAsync` çağırınca N+1 → repo'ya `ListByUserTrackedAsync` (tracked, AsNoTracking'siz) ekle; handler tek round-trip'te tracked listeyi alır, dictionary üzerinden in-place update; eklenen entity de dictionary'e geri yazılır ki response inşası ikinci DB sorgusu istemesin.
