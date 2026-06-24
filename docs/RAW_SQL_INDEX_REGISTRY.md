# Raw-SQL Index Registry (DRIFT-01)

> Bu indeksler EF model snapshot'ında GÖRÜNMEZ çünkü functional `lower(col)` / `USING gin` / `USING brin` ifadeleri EF Core fluent API'de temiz modellenemez (veya partition mekanizması raw-SQL ile kurar). `migrationBuilder.Sql(... IF NOT EXISTS)` ile yaratılırlar; benign drift'tir (DB'de varlar, EF dokunmaz). Sqlite test yolu (`EnsureCreated`, model'den) bunları KURMAZ → bu indekslere bağlı plan'lar yalnız Postgres integration ile doğrulanır. Yeni raw-SQL index eklenince BURAYA kaydet (CLAUDE.md §4.2 snapshot-drift kuralı).

## pg_trgm GIN trigram arama indeksleri (`20260513070147_AddTrigramSearchIndexes`)

`CREATE EXTENSION IF NOT EXISTS pg_trgm;` + `USING gin (lower(<col>) gin_trgm_ops)` — leading-wildcard `%term%` ILIKE aramaları (CustomerRepository/InvoiceRepository/...) hızlandırır.

| Index                                   | Tablo     | İfade                         |
| --------------------------------------- | --------- | ----------------------------- |
| ix_customers_name_trgm                  | customers | lower(name)                   |
| ix_customers_email_trgm                 | customers | lower(email)                  |
| ix_customers_phone_trgm                 | customers | lower(phone)                  |
| ix_customers_tax_number_trgm            | customers | lower(tax_number)             |
| ix_products_name_trgm                   | products  | lower(name)                   |
| ix_products_sku_trgm                    | products  | lower(sku)                    |
| ix_products_description_trgm            | products  | lower(description)            |
| ix_invoices_invoice_number_trgm         | invoices  | lower(invoice_number)         |
| ix_invoices_customer_name_snapshot_trgm | invoices  | lower(customer_name_snapshot) |
| ix_orders_order_number_trgm             | orders    | lower(order_number)           |

İyileştirme adayı (IDX-07, follow-up): tenant-scoped trigram (`CREATE EXTENSION btree_gin; gin (tenant_id, lower(col) gin_trgm_ops)`) cross-tenant candidate set'i küçültür.

## Partition altyapısı (raw-SQL — EF `PARTITION BY` emit edemez)

- `corealign_ensure_future_partitions(table, ts_col, months_ahead)` — kalıcı rollover fonksiyonu (`Phase86`, UTC-pinned `Phase89`). `PartitionMaintenanceHostedService` günlük çağırır.
- RANGE-partition leaf tablolar + per-partition `USING brin (<ts_col>)`:
  - `Phase86`: activity_logs, login_audit_logs, outbox_messages, stock_movements, customer_transactions (notification_messages `Phase87`'de geri alındı — idempotency unique partition-key içeremez)
  - `Phase95`: error_logs, customer_ledger_entries, vendor_ledger_entries, stock_transactions (FK-koruyan v2 fonksiyon; error_logs RLS/tenant-FK'siz cross-tenant)
- `entity_audit_logs` bilinçli partition DIŞI (hash-chain `(tenant_id, sequence)` unique partition-key içeremez).

## RLS (raw-SQL, opt-in `Database:EnableRls`)

`Phase85` + `Phase94` re-run: tüm tenant-FK'li tablolarda `tenant_isolation` policy (`current_setting('app.tenant_id')`), `corealign_app` non-owner rolü. error_logs + IGlobalReadable (payroll_parameters/tax_brackets) muaf.
