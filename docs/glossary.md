# Domain Glossary

Authoritative vocabulary for CoreAlign. Use these terms verbatim in code (PascalCase), API contracts,
UI strings, ADRs, and customer-facing copy. Turkish equivalents are listed where the term has a
canonical TR translation that appears in `tr.json` locale files or legal documents.

The glossary is **case-sensitive** for code. Plural forms follow English (`Invoices`) unless stated.

## A

### Aggregate

**TR:** Toplam (kavramsal). A DDD cluster of entities and value objects treated as a single transactional unit with one **Aggregate Root**. State mutations cross-aggregate are forbidden inside a single handler — they must flow via domain events or the outbox.

### Aggregate Root

**TR:** Toplam kokü (kavramsal). The single entity inside an aggregate that is allowed to be loaded from a repository and the only entity that external code may hold a reference to.

### Allocate (stock)

**TR:** Tahsis et. Reserve stock against a confirmed order line. Allocated stock is no longer "available to promise" but still belongs to the warehouse until **Ship** posts the movement. Reconciliation between `Allocate` and `Confirm` is an open inventory item — see `MEMORY.md` "Inventory dual-ledger gap".

### Audit Log

**TR:** Denetim kaydı. Append-only record of who did what when, on which tenant. Stored in PostgreSQL with monthly partitioning.

## B

### B2B Portal

**TR:** B2B Portali. The reseller-facing SPA at `apps/b2b/`. Persona: `Dealer`.

## C

### Credit Note

**TR:** İade faturası / Alacak dekontu. A negative-value financial document issued against a previously-issued **Invoice**, typically after a **Return**. Carries a reference to the original invoice number; tax authority reporting must match.

### Customer

**TR:** Müşteri. The buyer-side counterparty of a tenant business. May log in via the **Customer Portal**. Distinct from a CoreAlign user account: a customer record holds tax id, billing address, and commercial terms.

### Customer Portal

**TR:** Müşteri Portali. The end-customer SPA at `apps/customer-portal/`. Persona: `Customer`.

## D

### Dealer

**TR:** Bayi. A B2B reseller persona with elevated catalog and pricing rights. Logs into the **B2B Portal**.

### DecimalPlaces (tenant setting)

**TR:** Ondalık basamak sayısı (kiracı ayarı). Tenant-configurable number of decimal places used to format monetary and quantity values **for display only**. Storage uses 4dp at the invoice-line boundary; rounding never mutates persisted amounts. See `MEMORY.md` "Decimal precision is display-only".

### Document Renderer

**TR:** Belge oluşturucu. The `IDocumentRenderer` abstraction (implemented by `QuestPdfDocumentRenderer`, ADR 0006) that turns a typed view-model into a PDF byte array.

## F

### Fire (incident)

**TR:** Yangın olayı. Domain entity capturing a customer-reported critical incident (delivered Sprint 6). Has its own lifecycle distinct from a regular support ticket.

## G

### Global Query Filter

**TR:** Global sorgu filtresi. The EF Core mechanism (ADR 0002) that injects `WHERE TenantId = @currentTenantId` into every query against a `TenantEntity`-derived type. Bypassed only via explicit `IgnoreQueryFilters()`.

## H

### Hangfire

**TR:** Hangfire (proper noun). The PostgreSQL-backed background job runner used for the outbox drain, scheduled reports, and ad-hoc jobs (ADR 0007).

## I

### Invoice

**TR:** Fatura. A legally-issued sales document. Once issued, an invoice is immutable — corrections happen via a **Credit Note** or a follow-up invoice. Stored with a per-tenant document sequence.

### Invoice Line

**TR:** Fatura kalemi. A single billed line on an **Invoice**. Quantities and amounts are stored at 4dp; display rounding is governed by tenant **DecimalPlaces**.

### Iyzico

**TR:** Iyzico (proper noun). TR-market payment gateway (ADR 0008). Handles 3D-Secure card payments and TRY settlement.

## O

### Order

**TR:** Sipariş. A confirmed commercial commitment between a **Customer** and a tenant. Created from a **Quote** (or directly) and flows: `Draft -> Confirmed -> Allocated -> Shipped -> Closed` (or `Cancelled`).

### Outbox

**TR:** Outbox / Çıkış kutusu. The transactional outbox pattern (ADR 0004). A row in `OutboxMessages` is written in the same DB transaction as the aggregate change; a Hangfire job drains and dispatches asynchronously.

## P

### Persona

**TR:** Persona. One of `Customer`, `Dealer`, `Tenant`, `PlatformAdmin`. Encoded as a JWT claim (ADR 0005) and mapped to ASP.NET authorization policies. A single user account may hold multiple personas.

### PlatformAdmin

**TR:** Platform Yöneticisi. CoreAlign internal operations staff persona. Has cross-tenant powers; every cross-tenant action is audited.

### Product

**TR:** Ürün. Catalog entry. Carries `StockQuantity` as a denormalised on-hand cache that must reconcile with the **Stock Item** ledger (see "Inventory dual-ledger gap" in `MEMORY.md`).

## Q

### Quote

**TR:** Teklif. A non-binding price proposal to a **Customer**. Has an expiry date; once accepted, converts to an **Order**.

## R

### Return

**TR:** İade. Customer-initiated return of previously-delivered goods. Drives the creation of a **Credit Note** and a stock-in movement.

### Refresh Token

**TR:** Yenileme jetonu. Single-use, 7-day-TTL token used to mint a fresh access token. Stored server-side; rotated on every use (ADR 0005).

## S

### safeRequest

**TR:** safeRequest (proper noun, fonksiyon adı). Frontend HTTP helper that wraps fetch in a `[data, error]` tuple, removing the need for `try/catch` in components. Variants: `safeRequestWithNotify`, `safeBatchRequest`. See user global rules.

### Stock Item

**TR:** Stok kalemi. A per-warehouse, per-product on-hand record. The authoritative source for stock; `Product.StockQuantity` is a derived cache.

### Stock Voucher

**TR:** Stok fişi. A document recording a stock movement: in (`Mal Girişi`), out (`Mal Çıkışı`), or transfer (`Sevk`). Double-entry: every voucher posts at least two ledger lines that sum to zero across warehouses.

## T

### Tenant

**TR:** Kiracı (firma). A customer business of CoreAlign — i.e. one ERP installation. The unit of isolation: every tenant-owned row carries a `TenantId` (ADR 0002).

### TenantContext

**TR:** Kiracı bağlamı. The `ITenantContext` abstraction. `RequireTenantId()` is the canonical accessor inside handlers; it throws if no tenant has been resolved.

### TenantEntity

**TR:** Kiracı varlığı (taban sınıf). Base class for every aggregate owned by a tenant. Holds the non-nullable `TenantId` column wired into the global query filter.

## U

### Unit of Measure (UoM)

**TR:** Ölçü birimi. The unit a **Product** is sold or stocked in: `adet`, `kg`, `lt`, `m`, etc. Conversions between units are stored per product and applied at the stock-movement boundary.

### Outbox Message

**TR:** Outbox mesajı. A single row of the outbox table; payload is a JSON-serialised integration event with a stable type discriminator.

## W

### Warehouse

**TR:** Depo. A physical or logical location holding **Stock Items**. Tenant-scoped. Vouchers transfer stock between warehouses of the same tenant.

---

## Cross-references

- Tenant isolation contract: ADR 0002.
- Persona claim shape: ADR 0005.
- Outbox semantics: ADR 0004.
- Document rendering: ADR 0006.
- Background jobs: ADR 0007.
- Payments: ADR 0008.
