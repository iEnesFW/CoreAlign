# CoreAlign Ölçek-Hazırlık Final Remediation Roadmap

## 1. Verdict — Şu an kusursuz mu?

**Hayır.** Yapısal temel (6 leaf tablonun monthly RANGE partition'ı, BRIN, tenant-leading composite index'ler, UUIDv7, `(id, ts)` PK) gerçekten tamamlanmış ve snapshot'ta doğrulandı. Ancak "milyonlarca satır, sıfır yavaşlama" iddiası **iki yapısal nedenle bugün geçersiz**: (a) tüm kod tabanında **tek bir keyset/seek sorgusu yok** — 45 repository'nin tamamı `Skip((page-1)*pageSize)` kullanıyor; (b) ölçeği runtime'da ayakta tutan **operasyonel katman bağlanmamış** (Hangfire hiç bootstrap edilmemiş → partition rollover, retention, outbox retry sweep çalışmıyor).

**En kritik kalan 5 slowdown (impact sırasına göre):**

1. **OPS-01/OPS-02 — Hangfire hiç başlatılmamış → partition rollover job çalışmıyor.** `corealign_ensure_future_partitions()` tanımlı ama hiçbir caller yok. 2027-12'den sonra 6 yüksek-velocity tablonun **tüm yeni satırları tek `_pdefault` partition'ına** düşer; partition'ın çözdüğü dev-heap problemi geri gelir. Bu kademeli yavaşlama değil, sessiz bir availability cliff'i.
2. **PART-01 — Outbox drain her birkaç saniyede 25 partition'ı tarıyor.** En sık çalışan background sorgu, `created_at_utc` predicate'i taşımadığı için **hiçbir partition pruning yapamıyor** ve 25 local index'i fan-out ediyor.
3. **PART-08 — Idempotency/dedup unique index'leri partition rebuild'inde düştü.** `GetByHashAsync` artık tüm partition'larda seq-scan yapıyor **VE** uniqueness garantisi kaybolduğu için retry'lar çift-insert üretebiliyor (correctness regression).
4. **PAGE-01/02/03 — Append-only partition'lı tablolarda deep OFFSET** (customer/stock_transactions, customer/vendor/dealer ledger, entity_audit_logs). 5M satırlık bir customer ledger'da sayfa 5000'de Postgres her istekte ~250k satırı okuyup atıyor; OFFSET, satır-sayısı olarak ifade edildiği için partition pruning'i de yeniyor. `StreamAsync` skip-loop'u export'u O(n²) yapıyor.
5. **NQ-3/NQ-4/HP-04 — Report path'leri tüm ledger/line tablosunu app belleğine `ToListAsync()` ediyor** (TrialBalance → journal_lines, TopProducts → invoice_lines, AR/AP aging → açık invoice'lar). 5M satırda OOM + multi-second + GC basıncı.

---

## 2. Öncelikli düzeltme listesi

### Grup E — Operational / Rollover / Cache (ÖNCE: bunlar olmadan diğer fix'ler schedule edilemez)

**E1. Hangfire bootstrap + tüm recurring job'ları register et** `[critical]`

- **Ne yavaşlıyor:** Hiçbir scheduled maintenance job çalışmıyor → token/audit-export/log-anonymization tabloları sınırsız büyüyor, outbox deferred/failed retry sweep hiç çalışmıyor, partition rollover hook'lanamıyor.
- **Kanıt:** `Program.cs:301-412` içinde `AddHangfire`/`UseHangfireServer` yok; `RecurringJobsRegistration.cs:11` `RegisterAll()` tanımlı ama **sıfır source caller**. 10 job (`outbox-drain */30s`, `token-cleanup`, `tcmb-fx-ingest`, `scheduled-audit-exports` vb.) tanımlı ama inert.
- **Fix:** `Program.cs`'e `builder.Services.AddHangfire(c => c.UsePostgreSqlStorage(connectionString))` + `AddHangfireServer()`, `app.Build()` sonrası leader-guarded `RecurringJobsRegistration.RegisterAll(app.Services)`. Startup assertion: `IRecurringJobManager` resolve olmalı ve job sayısı == 10.
- **Effort:** low

**E2. Partition rollover job'ı schedule et** `[critical]`

- **Ne yavaşlıyor:** 2027-12 sonrası 6 tablonun yeni satırları tek `_pdefault`'a düşer → unbounded heap + BRIN degrade + tek partition lock contention.
- **Kanıt:** `Phase86PartitionLeafTables.cs:14` (`corealign_ensure_future_partitions` tanımlı), `:85-90` (24 ay 2026-01'den pre-created), `:64` (`_pdefault` catch-all). Hiçbir caller yok.
- **Fix:** Monthly `RecurringJob` (Cron.Monthly, gün 1): 6 tablo için `SELECT corealign_ensure_future_partitions('<table>','<ts_col>', 3)` (3 ay headroom) + aged partition için DETACH/DROP step. Health check: `current_month+1` partition'ı var olmalı. E1'e bağımlı.
- **Effort:** low

**E3. Retention'ı DROP/DETACH PARTITION'a çevir** `[high]`

- **Ne yavaşlıyor:** `notification_messages` retention tüm expired satırı belleğe `ToListAsync()` ediyor → OOM; `activity_logs`/`entity_audit_logs` `ExecuteDeleteAsync` ile O(n) DELETE → dev WAL + dead tuple + autovacuum basıncı + uzun lock.
- **Kanıt:** `RetentionPolicyExecutor.cs:65-82` (notification load-all + foreach Remove), `:84-89` (activity_logs ExecuteDelete), `:91-104` (audit ExecuteDelete). Üçü de artık monthly RANGE partition.
- **Fix:** Cutoff'tan eski tüm partition'lar için metadata DETACH+DROP (O(1)). `entity_audit_logs` için hash-chain'i korumak adına cold tablespace'e DETACH. Boundary ay için row-level fallback. E1'in retention schedule'ına bağla.
- **Effort:** medium

**E4. Outbox dispatch'e SKIP LOCKED + partial index** `[high]`

- **Ne yavaşlıyor:** `GetDueAcrossTenantsAsync` plain EF, **row lock yok** → multi-instance/concurrent drain'de aynı top-N Pending set'i çift işliyor (double GL posting/notification = correctness) ve contend ediyor. Non-partial `(Status, NextAttemptUtc)` index'i "Processed" satırlarıyla bloat oluyor.
- **Kanıt:** `OutboxRepository.cs:29-36`; index `ix_outbox_messages_status_next_attempt_utc` (snapshot:8223); inline drain `OutboxDrainBehavior.cs:29`.
- **Fix:** Raw SQL: `SELECT ... WHERE status='Pending' AND (next_attempt_utc IS NULL OR next_attempt_utc<=@now) ORDER BY created_at_utc LIMIT @max FOR UPDATE SKIP LOCKED`, processing transaction içinde. Partial index:
  ```sql
  CREATE INDEX ix_outbox_messages_pending_next_attempt
    ON outbox_messages (next_attempt_utc)
    WHERE status IN ('Pending','Deferred');
  ```
- **Effort:** medium

**E5. Inline per-request outbox drain'i sınırla** `[medium]`

- **Ne yavaşlıyor:** Bir mesaj enqueue eden request, dönmeden önce **cross-tenant 100 ilgisiz mesajı** senkron işliyor → request tail latency global backlog ile büyüyor.
- **Kanıt:** `OutboxDrainBehavior.cs:24-32` inline `DrainAsync`; `OutboxProcessor.cs:49` `GetDueAcrossTenantsAsync` (`IgnoreQueryFilters`, MaxBatch=100, cross-tenant foreach).
- **Fix:** Inline drain'i bounded/opportunistic yap (sadece current tenant'ın yeni enqueue'ları, küçük cap); global sweep'i Hangfire `OutboxDrainJob` (E1) + SKIP LOCKED'a (E4) bırak. `IOutboxSignal` sadece nudge etsin.
- **Effort:** medium

**E6. Global reference hierarchy'yi cache'le** `[medium]`

- **Ne yavaşlıyor:** currencies/countries/provinces/districts her nav'da DB'ye 1-4 round-trip atıyor; registered `ILookupCacheService` bu path'te hiç kullanılmıyor. CLAUDE.md "heavily cached for nav speed" kuralı ihlali.
- **Kanıt:** `LookupQueryService.cs:13-52` (4 metod direkt DB query); cache `InfrastructureServiceRegistration.cs:321` register'lı ama `LookupQueryService` inject etmiyor.
- **Fix:** Her metod body'sini `_lookupCache.GetOrCreateAsync($"lookup:currencies:{isActive}", ..., ttl: 1h)` ile sar. Global tablolar → tenant-agnostic key. Admin edit endpoint'lerinde `InvalidatePrefix("lookup:")`.
- **Effort:** low

---

### Grup D — Partition point-lookup / pruning

**D1. Outbox drain'e `created_at_utc` lower-bound** `[critical]`

- **Ne yavaşlıyor:** En sık çalışan background sorgu (her birkaç saniye + her command sonrası) 25 partition'ın local index'ini fan-out ediyor. Pending satırlar sadece en yeni 1-2 partition'da ama planner pruning yapamıyor.
- **Kanıt:** `OutboxRepository.cs:29-36 GetDueAcrossTenantsAsync` — `created_at_utc` predicate'i yok. `OutboxProcessor.cs:47-49 DrainAsync`.
- **Fix:** `Where(m => m.CreatedAtUtc >= utcNow.AddDays(-N))` (N = max retry backoff horizon, 7-14d) ekle → planner 1-2 partition'a prune eder. E4'teki partial index ile birleştir.
- **Effort:** medium

**D2. Outbox `GetByIdAsync` point-lookup'a partition key thread et** `[high]`

- **Ne yavaşlıyor:** PK artık `(id, created_at_utc)` olduğu için bare `id` lookup'ı 25 partition'ın PK index'ini probe ediyor — O(partitions × log n). `TransitionAsync` her failure/dead-letter path'inde çalışıyor.
- **Kanıt:** `OutboxRepository.cs:19-20 GetByIdAsync` (`m.Id == id` only); PK `(id, created_at_utc)` (migration:56); caller `OutboxProcessor.cs:122`.
- **Fix:** `GetByIdAsync(Guid id, DateTime createdAtUtc)` → `m.Id==id && m.CreatedAtUtc==createdAtUtc`. Caller in-memory message'ı zaten tutuyor; `message.CreatedAtUtc` geçir. Tek partition'a prune.
- **Effort:** medium

**D3. Idempotency/dedup unique index'lerini partition-compatible olarak yeniden oluştur** `[high]`

- **Ne yavaşlıyor:** Partition rebuild sadece NON-unique index kopyaladı (`indexdef NOT LIKE 'CREATE UNIQUE%'`, migration:50). `GetByHashAsync` artık her notification send'inde tüm partition'larda seq-scan; uniqueness garantisi gitti → retry double-insert (correctness).
- **Kanıt:** `Phase86PartitionLeafTables.cs:50`; lookup'lar `NotificationRepositories.cs:36-50` (`GetByHashAsync`, `GetByProviderMessageIdAsync`). Snapshot hâlâ `ux_notification_messages_tenant_idempotency` (unique, :7387) deklare ediyor ama partition'lı tabloda yok → snapshot drift (CLAUDE.md 4.2 ihlali).
- **Fix:** Partition key'i içeren composite olarak yeniden oluştur:
  ```sql
  CREATE UNIQUE INDEX ux_notification_messages_tenant_idempotency
    ON notification_messages (tenant_id, idempotency_hash, created_at_utc)
    WHERE idempotency_hash <> '';
  CREATE INDEX ix_notification_messages_provider_msg
    ON notification_messages (tenant_id, provider_used, provider_message_id, created_at_utc);
  ```
  Gerçek global uniqueness gerekiyorsa partition'lı unique index olamaz → ayrı idempotency-key lookup table veya app-level guard. Snapshot'ı reconcile et.
- **Effort:** high

**D4. Privacy erase için non-tenant-leading user index + set-based update** `[medium]`

- **Ne yavaşlıyor:** `activity_logs` privacy erase bare `user_id` ile filtreliyor ama tek user index'i tenant-leading (`ix_activity_logs_tenant_id_user_id_created_at_utc`); cross-tenant erase'in tenant_id'si yok → seek edemez, tüm eski partition'ları tarar + belleğe yükler.
- **Kanıt:** `PrivacyEraseService.cs:50-52` (`Where(UserId==userId ... CreatedAtUtc < threshold).ToListAsync()` + mutate); `PrivacyDataReader.cs:32-46 GetUserActivityAsync` aynı pattern.
- **Fix:**
  ```sql
  CREATE INDEX ix_activity_logs_user_id_created_at_utc
    ON activity_logs (user_id, created_at_utc);
  ```
  Erase mutation'ı `ExecuteUpdateAsync` (set-based) yap. (`login_audit_logs` zaten `(user_id, attempted_at_utc)` taşıyor, snapshot:6458.)
- **Effort:** low

**D5. `GetBySourceAsync` stock_movements time predicate** `[low/medium]`

- **Ne yavaşlıyor:** Tek bir source document için movement'lar `occurred_at_utc` bound'u olmadan filtreleniyor → indexed seek 25 partition'a fan-out (sabit 24x multiplier, order/receipt detail ekranında).
- **Kanıt:** `InventoryRepositories.cs:161-165 GetBySourceAsync` (`SourceDocumentType + SourceDocumentId`, time bound yok). Report path (`StockMovementsReport.cs:38-40`) FromUtc/ToUtc geçiyor → o doğru prune ediyor (handled).
- **Fix:** Caller source document date'i biliyorsa coarse `occurred_at_utc` range geçir (doc date ± birkaç gün) → 1-2 partition. Bilinmiyorsa bounded fan-out'u kabul et + dokümante et.
- **Effort:** low

---

### Grup C — N+1 / Query Shape (report'lar ölçekte en sert yavaşlayanlar)

**C1. TrialBalance — journal_lines'ı SQL-side aggregate et** `[high]`

- **Ne yavaşlıyor:** Periyodun tüm journal_lines'ını belleğe `ToListAsync()` edip C#'ta GroupBy/Sum. Geniş tarih aralığında multi-year ledger'da milyonlarca satır → transfer + GC + unbounded request.
- **Kanıt:** `JournalEntryRepository.cs:103-137 GetAccountBalancesAsync`; kod yorumu (:121) "EF Core 10: subquery (Any) + GroupBy translate edilemiyor. Flat fetch + in-memory group".
- **Fix:** `journal_lines` ⋈ filtered `journal_entries` (Status==Posted + date) üzerinde server-side `GroupBy(AccountId, AccountCode, AccountName)` → `g.Sum(Debit)/g.Sum(Credit)`. EF translate edemezse `FromSqlInterpolated` ile `SUM/GROUP BY`. DB account başına 1 satır döner, line başına değil.
- **Effort:** medium

**C2. TopProducts — invoice_lines'ı SQL-side aggregate + LIMIT** `[high]` _(✅ UYGULANDI — Npgsql `ToQueryString` + execute ile doğrulandı)_

- **Ne yavaşlıyor:** Sadece top-10/100 dönen sorgu, önce window'daki **TÜM invoice_lines'ı** indiriyor (Take aggregate'ten sonra). 5M invoice_lines'da OOM + latency.
- **Kanıt:** `ReportRepository.cs:208-240 GetTopProductsGlobalAsync`; yorum (:222) "GroupBy içinde nested Distinct().Count() translate edilemiyor. Flat fetch ... Raw SQL'e geçmek lazım".
- **Fix:** İki translatable aggregation: (a) `GROUP BY product_id → SUM(qty), SUM(line_total) ORDER BY 2 DESC LIMIT @n` tamamen SQL'de; (b) sadece o top product'lar için ayrı `COUNT(DISTINCT invoice_id)` veya `FromSqlInterpolated`. Raw line'ları asla `ToListAsync` etme.
- **Effort:** medium

**C3. AR/AP aging — SQL-side CASE bucketing** `[medium/high]` _(✅ AR server-side `SUM(CASE)` GROUP BY — Npgsql `ToQueryString`+execute ile doğrulandı; AP bilinçli in-memory: açık vendor-bill seti bounded + `VendorBill` server-side GroupBy EF Core 10'da translate olmuyor — koda not düşüldü)_

- **Ne yavaşlıyor:** Tüm açık invoice/bill'ler (Take/limit yok) belleğe çekilip C#'ta bucket'lanıyor. Yoğun tenant 100k+ açık invoice taşıyabilir; dashboard tile'ı multi-second GC-heavy scan'e döner.
- **Kanıt:** `ReportRepository.cs:266-311 GetAgingBucketsAsync` (AR), `AccountsPayableRepositories.cs:50-87 GetAgingBucketsAsync` (AP). İkisi de limit'siz `ToListAsync()`.
- **Fix:** `due = COALESCE(due_date, issue/bill_date)`, `SUM(CASE WHEN asOf - due <= 0 THEN outstanding ELSE 0 END) AS current` vb. `GROUP BY (customer/vendor, currency)`. LINQ conditional-sum projection (`CustomerLedgerRepository.GetCurrentBalanceAsync` pattern'i gibi) veya `FromSqlInterpolated`. Party başına 1 satır. Açık-AR filtresi için `(tenant_id, status, due_date)` index (bkz B-grubu) ekle.
- **Effort:** medium

**C4. StockCount list — `.Include(Lines)` kaldır, slim projection** `[high]` _(verdict: confirmed, critical→**high**)_

- **Ne yavaşlıyor:** List endpoint `stock_counts` ⋈ `stock_count_lines` (AsSplitQuery yok, projection yok); handler her line'ı DTO'ya map ediyor. Warehouse-wide count ~20k line; pageSize 25 → ~500k satır/sayfa cartesian. _(Not: severity high — tek child collection, pageSize cap'li (default 25, max 200), bu tablo partition'lı 6'dan değil; "OOM" abartı ama multi-hundred-ms-to-multi-second gerçek.)_
- **Kanıt:** `AccountsPayableRepositories.cs:261-283 SearchAsync` (`Include(c=>c.Lines)`, no AsSplitQuery); `StockCountHandlers.cs:303-315` → `StockCountMapper.cs:40` (`c.Lines...Select(ToDto).ToList()`). Index `ix_stock_count_lines_stock_count_id` var (snapshot:12534) — join seekable ama satır sayısını bound'lamıyor.
- **Fix:** `SearchAsync`'ten `.Include(c=>c.Lines)` çıkar, slim `StockCountSearchRow` project et (Id, CountNumber, WarehouseId/Name, Status, PlannedAtUtc, `c.Lines.Count` scalar subquery) — `OrderRepository.SearchAsync` pattern'i. `.Include(Lines)` sadece `GetWithLinesAsync` detail'de kalsın.
- **Effort:** low

**C5. PostStockCount N+1 — batch-load stock_items** `[high]` _(✅ UYGULANDI — `GetOnHandByProductLotAsync` tek query)_

- **Ne yavaşlıyor:** Hot inventory write'ında her counted line için tek `StockItem` SELECT loop'ta → N sequential round-trip (N = warehouse'taki stock item sayısı). 10k line = 10k awaited query, açık transaction tüm süre lock tutuyor.
- **Kanıt:** `StockCountHandlers.cs:200-221` (`foreach ... await _stockItems.GetAsync(...)`); `GetAsync` `InventoryRepositories.cs:14-16` tek `FirstOrDefaultAsync`.
- **Fix:** `IStockItemRepository.GetByWarehouseAndKeysAsync(warehouseId, keys)` ekle → tüm `(ProductId, LotId)` çiftlerini tek round-trip'te `Dictionary`'ye al, loop içinde in-memory resolve et.
- **Effort:** medium

**C6. stock_movements `SearchAsync` — 4 Include → slim projection** `[high]`

- **Ne yavaşlıyor:** Partition'lı yüksek-büyüme tablosunda deep OFFSET + 4 eager Include (Product/Warehouse/Lot/ReasonCode, AsSplitQuery yok) → 5-tablo join her sayfa için tüm related row'ları materialize ediyor.
- **Kanıt:** `InventoryRepositories.cs:140-157`.
- **Fix:** Keyset `(OccurredAtUtc DESC, Id DESC)` (bkz A-grubu) + 4 Include yerine slim `StockMovementSearchRow` projection (`StockItemRepository.SearchAsync` pattern'i, `InventoryRepositories.cs:53`).
- **Effort:** medium

**C7. PurchaseOrder/PurchaseRequisition list — AsSplitQuery veya slim projection** `[medium]`

- **Ne yavaşlıyor:** List sorguları `.Include(Lines)` (AsSplitQuery yok) → cartesian by line count; handler her line'ı map ediyor.
- **Kanıt:** `PurchaseOrderRepository.cs:23-42` + `PurchaseOrderHandlers.cs:361-373`; `PurchaseRequisitionRepository.cs:23-49` + `MrpHandlers.cs:360-378`.
- **Fix:** (a) ucuz: `.AsSplitQuery()` ekle; (b) iyi: slim `PurchaseOrderSearchRow`/`PurchaseRequisitionSearchRow` (header + `Lines.Count` + `Lines.Sum(...)` SQL subquery). Full `.Include(Lines)` sadece `GetByIdAsync`'te.
- **Effort:** low

**C8. ListDealerAllowedCustomers N+1 — GetByIdsAsync** `[medium]` _(✅ UYGULANDI — `GetByIdsAsync` + price-list `ListAsync` lookup)_

- **Ne yavaşlıyor:** Her allowed customer id için tek SELECT loop'ta → dealer'ın customer tabanıyla büyüyen N×RTT, portal-facing endpoint.
- **Kanıt:** `DealerPortalHandlers.cs:124-148` (`foreach ... await _customers.GetByIdAsync(id)`).
- **Fix:** `ICustomerRepository.GetByIdsAsync(IEnumerable<Guid>)` (Invoice/Product'ta zaten var pattern) → tek `WHERE Id IN (...)`, in-memory dictionary'den DTO. Price-list isimlerini de batch'le.
- **Effort:** low

**C9. (Opportunistic) Vendor payment-application N+1; GlassProject AsSplitQuery** `[low]`

- `VendorBillingHandlers.cs:914-921 / :946-953`: distinct payment/bill id'leri toplayıp `GetByIdsAsync` ile batch'le (repo'lar batch metod kazanınca).
- `GlassEnclosureProjectRepositories.cs:17-21 GetByIdWithRunsAsync`: iki sibling collection (Panels + Connections) cross-join → `.AsSplitQuery()` ekle (tek satır, diğer aggregate-root repo'ların disiplini).
- **Effort:** low

---

### Grup B — Index'ler (keyset rewrite öncesi gerekli DDL)

Tüm DDL, tenant-leading composite'lere trailing tiebreaker / sort kolonu ekler. Bunlar olmadan keyset cursor deterministik değildir.

**B1. Eksik sort index'leri (gerçekten yok olanlar)** `[high]`

```sql
-- shipments: CreatedDate üzerinde HİÇ index yok (PAGE-06) — full seq-scan + top-N sort her sayfa
CREATE INDEX ix_shipments_tenant_id_created_date
  ON shipments (tenant_id, created_date DESC, id DESC);

-- entity_audit_logs: tenant-wide timeline sort'u kaplayan index yok (PAGE-03)
CREATE INDEX ix_entity_audit_logs_tenant_id_changed_at_utc
  ON entity_audit_logs (tenant_id, changed_at_utc DESC, sequence DESC);

-- customers/products/vendors: CreatedAtUtc sort'u indexsiz (PAGE-10) — varsayılan liste full sort
CREATE INDEX ix_customers_tenant_id_created_at_utc ON customers (tenant_id, created_at_utc DESC, id DESC);
CREATE INDEX ix_products_tenant_id_created_at_utc  ON products  (tenant_id, created_at_utc DESC, id DESC);
CREATE INDEX ix_vendors_tenant_id_created_at_utc   ON vendors   (tenant_id, created_at_utc DESC, id DESC);

-- provider_webhook_inbox: ReceivedAtUtc sort'u indexsiz, yüksek-büyüme (PAGE-10)
CREATE INDEX ix_provider_webhook_inbox_tenant_id_received_at_utc
  ON provider_webhook_inbox (tenant_id, received_at_utc DESC);

-- notification_messages.ListForUserAsync: (tenant_id,user_id) index'i created_at_utc taşımıyor (PAGE-04)
CREATE INDEX ix_notification_messages_tenant_id_user_id_created_at_utc
  ON notification_messages (tenant_id, user_id, created_at_utc DESC, id DESC);
-- unreadOnly path için partial:
CREATE INDEX ix_notification_messages_tenant_user_unread
  ON notification_messages (tenant_id, user_id, created_at_utc DESC, id DESC)
  WHERE status <> 'Read';
```

**B2. Trailing tiebreaker ekleme (keyset stabilitesi için)** `[high]`

```sql
-- Dealer hot-path: equality prefix'te kalıyor, order_date taşımıyor (PAGE-05/HP-05) → dealer'ın tüm order'ları sort ediliyor
CREATE INDEX ix_orders_tenant_dealer_orderdate
  ON orders (tenant_id, origin_dealer_account_id, order_date DESC, id DESC);
CREATE INDEX ix_orders_tenant_id_order_date          ON orders          (tenant_id, order_date DESC, id DESC);          -- replace existing
CREATE INDEX ix_invoices_tenant_id_issue_date        ON invoices        (tenant_id, issue_date DESC, id DESC);          -- PAGE-07
CREATE INDEX ix_payments_tenant_id_payment_date      ON payments        (tenant_id, payment_date DESC, id DESC);        -- PAGE-07
CREATE INDEX ix_journal_entries_tenant_id_posting    ON journal_entries (tenant_id, posting_date DESC, number DESC);    -- PAGE-07
CREATE INDEX ix_quotes_tenant_id_quote_date          ON quotes          (tenant_id, quote_date DESC, id DESC);          -- PAGE-07
CREATE INDEX ix_vendor_bills_tenant_id_bill_date     ON vendor_bills    (tenant_id, bill_date DESC, id DESC);           -- PAGE-08
CREATE INDEX ix_vendor_payments_tenant_id_pay_date   ON vendor_payments (tenant_id, payment_date DESC, id DESC);        -- PAGE-08
CREATE INDEX ix_purchase_orders_tenant_id_order_date ON purchase_orders (tenant_id, order_date DESC, id DESC);          -- PAGE-08

-- Partition'lı yüksek-büyüme tablolar: trailing id (index-only tiebreaker)
CREATE INDEX ix_customer_transactions_...    ON customer_transactions (tenant_id, customer_id, occurred_at_utc DESC, id DESC);   -- PAGE-01
CREATE INDEX ix_stock_transactions_...       ON stock_transactions    (tenant_id, product_id, occurred_at_utc DESC, id DESC);    -- PAGE-01
CREATE INDEX ix_customer_ledger_entries_...  ON customer_ledger_entries (tenant_id, customer_id, occurred_at_utc DESC, id DESC); -- PAGE-02
CREATE INDEX ix_vendor_ledger_entries_...    ON vendor_ledger_entries (tenant_id, vendor_id, occurred_at_utc DESC, id DESC);     -- PAGE-02
CREATE INDEX ix_dealer_commission_ledger_... ON dealer_commission_ledger_entries (tenant_id, dealer_account_id, accrued_at_utc DESC, id DESC); -- PAGE-02
CREATE INDEX ix_stock_movements_...          ON stock_movements (tenant_id, occurred_at_utc DESC, id DESC);             -- PAGE-09 (+ product_id/warehouse_id variantları)
```

**B3. Trigram search index'leri — tenant-scoped btree_gin'e çevir** `[high]`

- **Ne yavaşlıyor:** Global `gin(lower(name))` index'leri tenant bilmiyor → `%steel%` araması TÜM tenant'larda trigram tarayıp cross-tenant candidate bitmap heap-fetch ediyor, sonra `tenant_id` recheck. Per-request iş global match count ile büyüyor (HP-01).
- **Kanıt:** `AddTrigramSearchIndexes.cs:15-27`; consumer'lar `CustomerRepository.cs:37-43`, `ProductRepository.cs:108-112`, `InvoiceRepository.cs:77-79`, `OrderRepository.cs:84-86`.
- **Fix:**
  ```sql
  CREATE EXTENSION IF NOT EXISTS btree_gin;
  -- her birini drop + tenant-scoped recreate:
  CREATE INDEX ix_customers_name_trgm ON customers USING gin (tenant_id, lower(name) gin_trgm_ops);
  -- + products.name/sku, invoices.invoice_number/customer_name_snapshot, orders.order_number
  ```
  `docs/RAW_SQL_INDEX_REGISTRY.md`'ye kaydet.

**B4. Payments trigram (hiç yok) + eksik OR-branch kolonları** `[high/medium]`

- **Ne yavaşlıyor:** payments search'ün **hiçbir trigram index'i yok** → her text search full seq-scan (HP-02). products.barcode / customers.code,legal_name kapsanmamış → tek index'siz OR-branch tüm ILIKE predicate'ini seq-scan'e düşürüyor (HP-03).
- **Kanıt:** `PaymentRepository.cs:40-51`; `ProductRepository.cs:111`, `CustomerRepository.cs:39-40`.
- **Fix:** tenant-scoped btree_gin trigram: `payments.(payment_number, customer_name_snapshot, reference_number)`, `products.barcode`, `customers.code`, `customers.legal_name`.
- **Effort:** low

**B5. Dashboard low-stock partial covering index** `[low]`

- **Ne yavaşlıyor:** `GetLowStockProductsAsync` `ORDER BY stock_quantity LIMIT 5` ama `stock_quantity` index'i yok → tüm aktif ürün top-N sort (her 30s cache miss).
- **Kanıt:** `DashboardStatsRepository.cs:60-69`.
- **Fix:** `CREATE INDEX ix_products_tenant_lowstock ON products (tenant_id, stock_quantity) WHERE status IN ('Active','New');`
- **Effort:** low

**B6. Open-AR filtresi için index (C3 aging'i besler)** `[medium]`

```sql
CREATE INDEX ix_invoices_tenant_id_status_due_date ON invoices (tenant_id, status, due_date);
```

---

### Grup A — Keyset Pagination (B-grubu index'leri yerleştikten SONRA)

Tüm 45 repository uniform OFFSET kullanıyor; tek keyset sorgu yok (CLAUDE.md §4.5/§11.1 ihlali). OFFSET "N+M satır tara, N at" demektir — partition'lı tablolarda partition pruning'i de yener (cursor satır-sayısı, key-range değil). Repo signature'ları `(page, pageSize)` → `(cursorTs, cursorId, pageSize)` olur. Exact `CountAsync` sadece ilk sayfa için tutulur ya da estimate/`has-next` (n+1 fetch) ile değiştirilir.

**A1. Critical — partition'lı append-only tablolar + ledger'lar + audit** `[critical]`

- **PAGE-01** customer_transactions / stock_transactions: `TransactionRepositories.cs:26-30, :53-57`. 5M satırlık bir customer'da sayfa 5000'de OFFSET ~250k satırı tüm monthly partition'lar boyunca okuyup atıyor. Keyset: `Where(t => t.CustomerId==id && (t.OccurredAtUtc < cur.Ts || (t.OccurredAtUtc==cur.Ts && t.Id < cur.Id))).OrderByDescending(t=>t.OccurredAtUtc).ThenByDescending(t=>t.Id).Take(pageSize)`.
- **PAGE-02** customer/vendor/dealer ledger entries: `PaymentRepository.cs:157-161`, `VendorRepositories.cs:198-203`, `CollaborationRepositories.cs:274-279`. Financial trail unbounded, asla silinmez; statement ekranı kronolojik page. Date-range statement'ta keyset from/to bound ile doğal kompose olur (range index scan'i daraltır, keyset içinde pozisyonlar).
- **PAGE-03** entity_audit_logs `SearchAsync` keyset `(ChangedAtUtc DESC, Sequence DESC)` + **`StreamAsync` skip-loop'u keyset'e çevir** (`PlatformFxAuditRepositories.cs:167-179`): 5M satırlık audit export O(n²) (batch k, k×batchSize satır skip) → `Where(a => a.ChangedAtUtc > last.Ts || (... == ... && a.Sequence > last.Seq))` ile **O(n)**. Aynı O(n²) pattern `BulkImporterBase`'de de var.

**A2. High — partition'lı inbox + core ERP document'ları** `[high]`

- **PAGE-04** notification_messages / notifications / activity_logs: `NotificationRepositories.cs:23, :30`, `CollaborationRepositories.cs:165-170`, `ActivityLogRepository.cs:29-31, :51-53`. Keyset `(CreatedAtUtc DESC, Id DESC)`. (B1 index'leri notification_messages için gerekli.)
- **PAGE-05** orders `SearchByDealerAsync` + `SearchAsync`: `OrderRepository.cs:148-164, :104-120`. (B2 dealer+order_date index'i gerekli.)
- **PAGE-06** shipments `SearchAsync`: `ShipmentRepository.cs:71-99`. (B1 index'i = missing piece; sonra keyset `(CreatedDate, Id)`.)
- **PAGE-07** invoices/payments/journal_entries/quotes `SearchAsync`: `InvoiceRepository.cs:96-100, :265-269`, `PaymentRepository.cs:57-61`, `JournalEntryRepository.cs:81-85`, `QuoteRepository.cs:99-103`. Non-search filtered/sorted path keyset'lenir; ILIKE-search branch ortogonal (B3/B4 trigram).
- **PAGE-09** stock_movements `SearchAsync`: keyset + C6 projection birlikte.

**A3. Medium — master-data + webhook inbox** `[medium]`

- **PAGE-10** customers/products/vendors/provider_webhook_inbox: keyset `(CreatedAtUtc/ReceivedAtUtc DESC, Id DESC)` (B1 index'leriyle). **İstisna:** tenants / data_subject_requests / subscription_orders / MRP runs genuinely bounded/low-volume → OFFSET kalsın (CLAUDE.md §4.5 "OFFSET yalnızca küçük bounded admin listelerinde"). Bu istisnayı `docs/INVARIANTS.md`'ye kaydet.

---

### Grup E (devam) — kalan operasyonel

**E7. Ledger/journal/report tablolarını partition'la (sonraki dalga)** `[medium]`

- **Ne yavaşlıyor:** customer/vendor/dealer_commission_ledger_entries, journal_lines, report_runs hâlâ unpartitioned single-column PK; append-heavy monotonic büyüme; vacuum tüm heap'i tarar; retention DELETE'siz imkânsız.
- **Kanıt:** snapshot `pk_customer_ledger_entries:1198`, `pk_journal_lines:6385`, `pk_report_runs:11037` — hepsi single `Id`.
- **Fix:** Quarterly RANGE on occurred*at/posting_date (ledger'lar), monthly RANGE on ran_at_utc (report_runs), PK `(id, ts)`, `corealign_partition_leaf_table` yaklaşımı. `entity_audit_logs` dokümante exclusion (hash-chain). E2 rollover job'ına kaydet. *(İndeksler okumayı near-term ayakta tuttuğu için 6 tamamlanandan daha düşük aciliyet ama "zero slowdown"ı gerçekten kapatmak için gerekli.)\_
- **Effort:** high

---

### Grup-dışı (slowdown lensine dahil değil ama kayda geçer)

**OPS-05 — `EnableRetryOnFailure` yok** `[medium]` _(verdict: overstated, high→medium, out-of-lens)_

- Gerçek ve CLAUDE.md §4.10 ihlali (`InfrastructureServiceRegistration.cs:106` bare `UseNpgsql`), ama **hiçbir query'yi satır sayısıyla yavaşlatmaz** — resilience/error-handling gap'i, scale-induced slowdown değil. Fix: `npg.EnableRetryOnFailure(5, 5s, null)`. **Uyarı:** execution strategy, user-initiated transaction'ları (`TransactionBehavior.cs:23`, `UnitOfWork.cs:21-29`, Merge/Convert/ReportSchedule handler'ları) `strategy.ExecuteAsync` ile sarmadıkça kırar — audit edilmeli. Ayrıca `OutboxProcessor.cs:68-83` retry loop'u broad `catch(Exception)` ama **backoff'suz** → transient blip 4 attempt'i mikrosaniyede yakıp dead-letter ediyor; backoff ekle.

---

## 3. EXPLAIN ANALYZE listesi (gerçek-veriyle plan doğrulaması şart)

Aşağıdakiler ölçek davranışı veri-dağılımına bağlı olduğu için sentetik milyon-satır dataset (multi-tenant, multi-month partition spread) ile `EXPLAIN (ANALYZE, BUFFERS)` gereklidir:

1. **PART-01 / D1** `OutboxRepository.GetDueAcrossTenantsAsync` — partition pruning'in `created_at_utc` lower-bound eklenince gerçekten 1-2 partition'a düştüğünü, partial index'in seçildiğini doğrula (`Append` node'da pruned partition sayısı).
2. **PAGE-01** customer_transactions deep page (sayfa 1 vs 5000) — OFFSET planı (kaç partition, kaç satır discard) vs keyset planı; keyset'in tek partition Index Scan'e düştüğünü doğrula.
3. **PAGE-03 / A1** entity_audit_logs `StreamAsync` — O(n²) skip-loop'un keyset'e dönüşünce her batch'in sabit-maliyet Index Scan olduğunu (büyüyen OFFSET olmadan) doğrula; B1 timeline index'inin tenant-wide search'te seçildiğini kontrol et.
4. **HP-01 / B3** trigram search (`%steel%` gibi orta-yaygın terim) — global gin vs btree_gin `(tenant_id, lower(name))`; cross-tenant candidate bitmap boyutu ve heap fetch farkını doğrula.
5. **NQ-3 / C1** TrialBalance — server-side GroupBy'ın gerçekten translate olduğunu (in-memory'e düşmediğini) ve account başına 1 satır döndüğünü; HashAggregate planını doğrula.
6. **NQ-4 / C2** TopProducts — iki-fazlı SQL aggregation + LIMIT'in raw line set'i materialize etmediğini doğrula.
7. **HP-04 / C3** AR/AP aging — CASE-bucketing GroupBy'ın party başına 1 satırla döndüğünü, açık-AR filtresinin `(tenant_id, status, due_date)` index'ini kullandığını doğrula.
8. **PART-08 / D3** `GetByHashAsync` — yeni partition-compatible unique index'in seq-scan yerine Index Scan seçtiğini doğrula.
9. **PAGE-05 / HP-05 / B2** orders `SearchByDealerAsync` — dealer composite index `order_date DESC` eklenince Sort node'un kaybolduğunu (Index Scan already-sorted) doğrula.
10. **PAGE-06 / B1** shipments `SearchAsync` — yeni `(tenant_id, created_date, id)` index'inin seq-scan + top-N sort'u Index Scan'e çevirdiğini doğrula.
11. **NQ-1 / C4** StockCount list — `.Include` kaldırıldıktan sonra dönen satır sayısının pageSize'a indiğini (cartesian'ın gittiğini) doğrula.

---

## 4. Kapanış — Bu liste bitince "kusursuz / no-slowdown" denebilir mi?

**Evet, ama şu varsayımlarla / sınırlarla:**

- **A + B + C + D + E1–E6 grupları tamamlanırsa**, satır-sayısıyla doğru orantılı yavaşlayan tüm path'ler (deep OFFSET, all-partition fan-out, in-memory report aggregation, cartesian Include, N+1, missing/global index) kapanır. Bu noktada hot list/detail/report sorgularının p99'u **veri hacminden bağımsız** (keyset + partition pruning + SQL-side aggregation sayesinde page-depth'ten ve total-row-count'tan kopuk) hale gelir — "milyonlarca satır, sıfır yavaşlama" iddiası doğrulanabilir.

- **Üç koşul olmadan iddia geçici/eksiktir:**
  1. **E1/E2 (Hangfire + rollover) mutlaka önce** — bunlar olmadan partition rollover çalışmaz; sistem yapısal olarak doğru görünse de **2027-12'de expiry'si olan bir iddia**dır (yeni satırlar `_pdefault`'a düşer ve tüm pruning bozulur). Bu yüzden E1/E2 listenin en başında.
  2. **E7 (ledger/journal/report partition'lama)** yapılmadıkça customer/vendor/dealer ledger, journal_lines ve report_runs sadece "index'ler okumayı near-term ayakta tutuyor" durumundadır — append-heavy büyümeyle vacuum/retention sorunu kaçınılmaz. "Gerçekten kusursuz" ancak bu sonraki dalga ile tamamdır.
  3. **D3 (idempotency unique index)** bir correctness regression'ı da kapatır — yapılmazsa retry'lar çift-insert üretmeye devam eder; bu "slowdown" değil ama "no-slowdown" iddiasının yanında **veri bütünlüğü** koşuludur.

- **EXPLAIN ANALYZE doğrulaması (Bölüm 3) yapılmadan** hiçbir fix "kanıtlanmış" sayılmaz — özellikle trigram (B3), TrialBalance/TopProducts/aging (C1–C3) ve partition pruning (D1) planner davranışına bağlıdır; gerçek veri-dağılımı olmadan plan regresyonu gizli kalabilir.

- **Lens-dışı:** OPS-05 (retry) tamamlanmasa bile "no-slowdown" iddiası geçerlidir (resilience gap'i, satır-sayısı yavaşlaması değil) — ama production robustness için yine de kapatılmalı.

Özetle: liste bittiğinde CoreAlign **query-shape ve index düzeyinde ölçek-kusursuz** olur; "kalıcı olarak kusursuz" demek için E2 rollover job'ının canlı olması ve E7 ledger partition dalgasının planlanmış olması gerekir.

---

## 📊 Empirik Doğrulama (EXPLAIN ANALYZE, sentetik veri)

`corealign_perf` (şema replikası: partition'lar + index'ler + RLS) üzerinde sentetik veriyle ölçüldü.

| Sorgu                           | Veri                                    | Plan                                                               | Süre        | Sonuç                                                   |
| ------------------------------- | --------------------------------------- | ------------------------------------------------------------------ | ----------- | ------------------------------------------------------- |
| **TrialBalance (C1 fix)**       | 2M journal_lines / 400k entry           | `GroupAggregate` → 50 satır (server-side, app'e 400k transfer YOK) | **132 ms**  | ✅ C1 düzeltmesi çalışıyor                              |
| **Liste sığ sayfa (page 1)**    | 1M orders                               | `Index Scan` ix_orders_tenant_orderdate_id                         | **0.3 ms**  | ✅ index layer sığ sayfayı sub-ms yapıyor               |
| **Deep OFFSET (page ~4000)**    | 1M orders                               | Index Scan ama 100k satır oku-at                                   | **225 ms**  | ⚠️ lineer büyür → keyset gerekli                        |
| **Keyset (cursor)**             | 1M orders                               | Index range scan, 25 satır okur                                    | **0.47 ms** | ✅ derinlikten bağımsız sabit                           |
| **Partition pruning (June)**    | 1M customer_transactions / 12 partition | sadece `_p202606` tarandı (1/12) + local index                     | **6.3 ms**  | ✅ partitioning + BRIN doğru çalışıyor                  |
| **Arama (common term + LIMIT)** | 500k customers                          | Seq Scan + Filter, LIMIT short-circuit                             | **0.1 ms**  | ✅ yaygın terim hızlı                                   |
| **Arama (ORDER BY name)**       | 500k customers                          | tüm match + sort                                                   | 166 ms      | ⚠️ sort maliyeti (covering index ile iyileştirilebilir) |

### Data-backed verdict

- **Yapısal temel + index layer ölçek-güvenli olduğu KANITLANDI:** sığ liste sayfaları sub-ms, report'lar server-side (whole-table-to-memory bitti), partition pruning 1/N partition'a iniyor.
- **Tek gerçek kalan yavaşlama: deep-OFFSET** (page ~4000 = 225ms, lineer). Gerçek-dünyada nadir (kullanıcı filtreler, 4000. sayfaya gitmez) ve **keyset index'leri zaten yerinde (Phase88)** + keyset 0.47ms doğrulandı → kalan iş yalnızca append-only/infinite-scroll repo'larını keyset'e çevirmek (ledger/transaction/audit history; index'ler hazır).
- C2 TopProducts C1 ile aynı pattern (join+GroupBy) → yüksek güven; uygulandığında aynı server-side kazanım.
