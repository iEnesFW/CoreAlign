# DB Professionalization — Status & Follow-up

> DB profesyonelleştirme çalışmasının durumu ve kalan işin planı. Kaynak: `docs/DB_AUDIT_REPORT.md` (multi-agent verified audit). Kurallar: `CLAUDE.md` §4.

## ✅ Tamamlanan (validate + commit)

Tüm migration zinciri (`InitialSchema` → `Phase84`) **sıfırdan temiz apply** edildi (brand-new DB). Build yeşil; 343 finansal + 2 concurrency-guard testi yeşil.

| Commit    | İş                                                                                                                                                 |
| --------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `2ef622e` | Kurallar — CLAUDE.md §4 PostgreSQL standardı + `docs/DB_AUDIT_REPORT.md`                                                                           |
| `291ed3f` | Wave 1a — `xmin` (8 finansal tablo), 3 ledger FK `Cascade→Restrict`, `journal_lines→gl_accounts` FK, 23505/23503/concurrency→409 (Phase77)         |
| `b0b7788` | Wave 1b — atomic `document_sequences` + ledger running-balance advisory lock (customer+vendor) + testler                                           |
| `179fa69` | Wave 2a — finansal CHECK constraint'ler (Phase78)                                                                                                  |
| `ad34cc6` | UUIDv7 PK (BaseEntity/TenantEntity)                                                                                                                |
| `85f40b0` | Wave 2b — hot-path FK index'leri (Phase79)                                                                                                         |
| `7fe8810` | Wave 3a — FX rate scale `18,8→18,6` unify (Phase82) + durdurulan ajan şema işinin checkpoint'i (Phase80/81)                                        |
| `c1d54d6` | Wave 4a — 153 gerçek `tenant_id` FK (`ApplyTenantForeignKeys` convention loop, Phase83)                                                            |
| `bf8511c` | Wave 2c — soft-delete-aware partial unique (glass_projects/purchase_requisitions/warranty_contracts) + pre-existing index-name drift fix (Phase84) |

## ⏳ Kalan iş

### Bilinçli olarak ertelenenler (ayrı, test edilmiş efor gerektirir — ajan resume'unu bloklamaz)

- **RLS — TEN-02 (defense-in-depth tenant izolasyonu).** `ENABLE/FORCE ROW LEVEL SECURITY` + `CREATE POLICY ... USING (tenant_id = current_setting('app.tenant_id')::uuid)`. **Neden ertelendi:** önkoşulu olan `DbConnection` interceptor (her bağlantıda `app.tenant_id` GUC'sini `TenantContextAccessor`'dan set eden) + non-owner DB rolü + **tüm** query path'lerinin (background job, webhook, health check, migration bağlantısı dahil) GUC set ettiğinin doğrulanması gerekir. GUC set edilmeyen tek bir path fail-closed boş sonuç döndürür. Ajan filosu resume olmadan önce aceleye getirilmesi riskli — kendi PR'ı + tam query-path testiyle yapılmalı.
- **Partitioning + BRIN — Wave 5 (ölçek).** High-growth tabloları (audit/log, ledger, stock, outbox/webhook) RANGE partition + per-partition BRIN + pg_partman rollover + retention'ı `DROP PARTITION`'a çevirme. **Neden ertelendi:** mevcut tabloları partition'a çevirmek tablo-rewrite ister (create-partitioned + data-migrate + swap), downtime-aware planlama gerektirir. Invasive; ajan resume öncesi rush edilmemeli. Partition key her UNIQUE/PK'nın parçası olmalı (PK'yı `(id, <ts>)` yap). Notification retention'ı şimdiden batched/keyset yap (PART-02, OOM riski).

### Düşük-değerli polish (stable-model window'da config-based migration ile)

- **GIN/trigram drift — DRIFT-01:** 10 `pg_trgm` GIN index'i (raw SQL'de, snapshot'ta yok) EF config'e `HasMethod("gin").HasOperators("gin_trgm_ops")` ile bildir veya `docs/RAW_SQL_INDEX_REGISTRY.md`'ye kaydet. Functional `lower(col)` index'leri EF'te temiz modellenemiyor → registry tercih edilebilir. (Benign drift: index'ler DB'de var, EF dokunmuyor.)
- **Phase79 index reconcile:** `OrderConfiguration`/`GlassProjectConfiguration`'a `HasIndex` ekle (raw-SQL Phase79 ile aynı isim → migration'da redundant `CreateIndex`'i elle kaldır, snapshot güncellensin).
- **IDX-06:** redundant non-unique prefix index'leri düşür. **IDX-03:** hot `(tenant_id, status)` index'lerine trailing `created_at_utc DESC`. **IDX-08:** outbox aktif-altküme partial + `SKIP LOCKED`. **IDX-02 (kalan):** invoices/return_requests/payment_transactions/quotes/service_tickets shadow `*_id` index triage.
- **Soft-delete partial unique (kalan):** RetentionPolicy, TenantIdentityProvider, NotificationTemplate, NotificationMessage, GlassWorkOrder(Revision), FieldSurvey, InstallationAcceptance, PaymentTransaction, ServiceTicket, DataSubjectRequest, UserPreferences (yüksek-trafikli 3'ü yapıldı).
- **DTS-03:** unbounded `numeric` pin. **DTS-04:** JSON `varchar`→`jsonb`. **DTS-06:** tutarsız string tipleri (`AccountCode`/`DealerApprovalStatus`). **DTS-08:** calendar-date → `date`/`DateOnly`.
- **UUIDv7 straggler'ları:** kendi `Id`'sini `Guid.NewGuid()` ile set eden ~12 low-volume/lookup entity (auth entity'ler §5 gereği dikkatli). Base class'lar (yüksek-velocity tablolar) yapıldı.
- **TEN-01:** `IGlobalReadable` semantiğini implement et veya marker'ı sil.

## Notlar

- **Delete-handler follow-up:** RIC-03 sonrası `DeleteCustomer/Product/Vendor` Restrict'e çarpıyor; middleware temiz 409 veriyor, ideal olan handler'da önce dependent-check.
- **Throwaway DB'ler:** `corealign_dbaudit`, `corealign_final_validate` (localhost:5432) validasyon için kaldı; `DROP DATABASE corealign_dbaudit; DROP DATABASE corealign_final_validate;` ile silinebilir.
- **Migration ordering:** EF wall-clock ID'leri verir ama proje Phase## tarihlerini ileri-tarihli kullanıyor (Phase84 = 20260628). Yeni migration üretince ID'yi son Phase'den sonraya rename et (yoksa apply order bozulur).
- **`migrations add --no-build` tuzağı:** config edit sonrası `--no-build` stale assembly kullanıp boş migration üretebilir; config değişiminden sonra build'li `migrations add` kullan.
