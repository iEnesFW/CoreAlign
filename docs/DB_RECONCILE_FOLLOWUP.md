# DB Professionalization — Reconcile & Follow-up

> Bu dosya, DB profesyonelleştirme çalışmasının **eşzamanlı ajan penceresinde** yapılan kısmının takibini ve kalan işin (stable-model window gerektiren) planını tutar. Kaynak: `docs/DB_AUDIT_REPORT.md` (multi-agent verified audit). Kurallar: `CLAUDE.md` §4.

## Bağlam

Çalışma sırasında repoda birçok ajan aktifti ve model snapshot'ı (`CoreAlignDbContextModelSnapshot.cs`) sürekli değişiyordu (`dotnet ef migrations has-pending-model-changes` = evet; örn. `notification_rate_counters` başka bir ajan tarafından eklenmiş, henüz migrate edilmemiş). CLAUDE.md §12.9 gereği: snapshot'a dokunmadan, **el-yazımı idempotent raw-SQL migration**'lar ile ilerlendi. Aşağıdaki "Reconcile gerekenler" bu yüzden EF modeline (config) işlenmedi — model stabilize olunca işlenmeli.

## ✅ Tamamlanan (validate + commit)

| Commit    | İş                                                                                                                                 |
| --------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `291ed3f` | Wave 1a — `xmin` (8 finansal tablo), 3 ledger FK `Cascade→Restrict`, `journal_lines→gl_accounts` FK, 23505/23503/concurrency→409   |
| `b0b7788` | Wave 1b — atomic `document_sequences` advisory lock, ledger running-balance advisory lock (customer+vendor) + concurrency testleri |
| `179fa69` | Wave 2a — finansal CHECK constraint'ler (Phase78)                                                                                  |
| `ad34cc6` | UUIDv7 PK (BaseEntity/TenantEntity)                                                                                                |
| `85f40b0` | Wave 2b — hot-path FK index'leri (Phase79)                                                                                         |
| `2ef622e` | Kurallar — CLAUDE.md §4 PostgreSQL standardı + audit raporu                                                                        |

Doğrulama: 100+ migration zinciri sıfırdan temiz apply (throwaway DB `corealign_dbaudit`); 343 finansal test + 2 yeni concurrency-guard testi yeşil.

## 🔁 Reconcile gerekenler (model stabilize olunca config'e işle)

- **Phase79 index'leri → EF config.** `OrderConfiguration`'a `HasIndex(o => new { o.TenantId, o.OriginDealerAccountId })` ve `HasIndex(o => new { o.TenantId, o.GlassProjectId })`; `GlassProjectConfiguration`'a `HasIndex(p => new { p.TenantId, p.AssignedSalespersonUserId })`. Sonra `migrations add` — EF `CreateIndex` üretir; raw-SQL ile aynı isimde olduğu için migration'ı idempotent yaz veya önce `DROP INDEX`. Amaç: snapshot bu index'leri görsün (drift kapansın).
- **Phase78 CHECK constraint'leri → reconcile GEREKMEZ.** EF CHECK constraint'leri (HasCheckConstraint dışında) modellemez; raw-SQL idiomatic (Phase48 precedent). Drift değil.
- **UUIDv7 straggler'ları.** `Guid.NewGuid()` ile kendi `Id`'sini set eden ~12 low-volume lookup entity (DataSubjectRequest, EmailVerificationToken, Module, ModulePricePlan, ClimateZone, WindZone, RetentionPolicy, ProcessedWebhookEvent, PasswordResetToken, UserDeviceToken, …) `Guid.CreateVersion7()`'ye çevrilmeli. Notifications altındakiler aktif churn'de olduğu için ertelendi.

## ⏳ Stable-model window gerektiren (kalan dalgalar)

Bunlar config-tabanlı `migrations add` ister (snapshot'a yazar); eşzamanlı ajanlar model değişikliklerini commit edip snapshot stabilize olunca yapılmalı.

### Wave 2 (kalan)

- **IDX-04 / RIC-05 / SOFT-01:** Soft-deletable tablolardaki unique index'leri partial yap (`HasFilter("is_deleted = false")` / `"deleted_at_utc IS NULL"`). Mevcut full unique'i DROP + partial CREATE → EF-managed olduğu için config'den yapılmalı (raw-SQL DROP'u EF geri ekler).
- **IDX-06:** 11 redundant non-unique prefix index'i düşür (daha uzun unique'in left-prefix'i). EF-managed → config'den.
- **IDX-03:** Hot `(tenant_id, status)` index'lerine trailing `created_at_utc DESC` ekle, redundant bare'i düşür.
- **IDX-08 / OUTBOX-01:** `outbox_messages` aktif-altküme partial index (`WHERE status IN (...)`) + `SKIP LOCKED` dispatch.
- **IDX-02 (kalan):** invoices/return_requests/payment_transactions/quotes/service_tickets/warranty_contracts/journal_lines shadow `*_user_id`/`*_id` index triage (Phase79 sadece orders+glass_projects'i kapsadı).

### Wave 3 — Types/precision

- **DTS-05:** FX rate scale birle — `JournalLine.ExchangeRate`, `VendorLedgerEntry.ExchangeRate`, `GlassEnclosureProject.FxRateToBase` `numeric(18,8)` → master ile hizalı `numeric(18,6)`.
- **DTS-03:** Unbounded `numeric` kolonları pinle (money→18,4, quantity→uygun scale).
- **DTS-04:** JSON payload `varchar`→`jsonb`.
- **DTS-06:** Tutarsız string tipleri (`AccountCode` unbounded text → varchar(32); `DealerApprovalStatus` → varchar(20)).
- **DTS-08:** Calendar-date alanları (`valid_on_date`, due dates) `date`/`DateOnly`.

### Wave 4 — Multi-tenant integrity

- **RIC-01:** `tenant_id` için convention loop ile 154 gerçek FK (`HasOne<Tenant>()...OnDelete(Restrict)`), tek migration. Önce orphan data validate.
- **TEN-02:** Defense-in-depth RLS (finansal/ledger/stock önce) — `ENABLE/FORCE ROW LEVEL SECURITY` + `CREATE POLICY ... USING (tenant_id = current_setting('app.tenant_id')::uuid)`. **Önkoşul:** `DbConnection` interceptor `app.tenant_id` GUC'sini `TenantContextAccessor`'dan set etmeli (app kodu) — bu olmadan RLS tüm erişimi bloklar. App + non-owner DB rolü koordineli yapılmalı.
- **TEN-01:** `IGlobalReadable` semantiğini implement et (`e.TenantId == current || e.TenantId == Guid.Empty`) veya marker'ı sil + explicit `IgnoreQueryFilters` tek yol yap.

### Wave 5 — Ölçek & Partitioning (strategic, invasive)

- High-growth tabloları RANGE partition (ledger çeyreklik, stock/audit aylık, outbox/webhook haftalık-aylık) — `migrationBuilder.Sql()` + partition key'i PK/unique'e absorbe et + pg_partman/scheduled rollover. Mevcut tabloları partition'a çevirmek tablo-rewrite ister (create-partitioned + data-migrate + swap) → planlı, downtime-aware.
- Append-only zaman kolonlarına per-partition BRIN.
- Retention'ı `DROP/DETACH PARTITION`'a çevir; notification retention'ı şimdiden batched/keyset yap (PART-02, OOM riski).

### Wave 6 — Drift reconcile & governance

- **DRIFT-01:** 10 GIN/trigram (pg_trgm) index'i EF config'e `HasMethod("gin").HasOperators("gin_trgm_ops")` ile bildir (snapshot drift kapansın) veya `docs/RAW_SQL_INDEX_REGISTRY.md` + INVARIANTS kaydı.
- Bu dosyadaki "Reconcile gerekenler"i config'e işle.
- Migration governance: Phase## uniqueness CI check, `docs/MIGRATION_LOG.md`, scratch (`TempPendingProbe`) migration temizliği.

## Notlar

- **Delete-handler follow-up:** RIC-03 sonrası `DeleteCustomer/Product/Vendor` handler'ları Restrict'e çarpıyor; middleware temiz 409 veriyor ama ideal olan handler'da önce dependent-check.
- **Throwaway DB:** `corealign_dbaudit` (localhost:5432) validasyon için duruyor; `DROP DATABASE corealign_dbaudit` ile silinebilir.
- **Eşzamanlı ajan:** `notification_rate_counters` (entity+config+DbSet eklenmiş, migration'ı YOK) başka bir ajanın işi — onların migrate etmesi gerekiyor; bana ait değil.
