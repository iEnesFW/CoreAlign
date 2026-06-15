# Sprint 13 Blockers

Financial-core audit fixes. All confirmed bugs were fixed migration-free with
RED→GREEN regression tests. Two items touch areas owned by the snapshot owner
(outbox drain path / GLPostingMapping seed) and are documented here rather than
edited directly.

## A-1 follow-up — WithholdingReceivable GL mapping seed [2026-06-10]

### Fixed (code-only)

The withholding (tevkifat) bug — sales revenue understated by the withholding
amount because `SalesGLLines.Build` booked revenue as `Total − Tax` (and `Total`
is already net of withholding) — is fixed. `SalesGLLines.Build` now takes
`(revenue, tax, withholding)`, books revenue at the taxable base, debits AR for
only what the customer owes (`taxable + VAT − withholding`), and debits a new
`GLPostingKey.WithholdingReceivable` line for the withholding so the entry still
balances: `DR(AR + Withholding) == CR(Revenue + VAT)`.

`GLPostingKey.WithholdingReceivable = 12` was appended to the enum; its standard
TDHP default is **193** (Peşin Ödenen Vergiler ve Fonlar) in
`GLPostingDefaults.CodeFor`.

### Follow-up for snapshot/seed owner: ensure account 193 exists in the chart

`GLPostingService.Resolve` falls back to the default code `193` when the tenant
has no `GLPostingMapping` override for `WithholdingReceivable`. If a tenant's
chart of accounts does **not** contain a postable, active account with code `193`,
a withholding invoice's GL posting will be **deferred** (`SkippedUnmapped`) rather
than posted — same behavior as any other unmapped key today.

Action: confirm the TDHP chart seed (`DemoDataSeeder` / tenant bootstrap)
provisions account **193** as postable+active, OR add a tenant
`GLPostingMapping` row mapping `WithholdingReceivable → <tenant account>`.
No schema change is required (the enum value and default are code-only); this is
purely a seed-data assertion for the snapshot/seed owner.

## D-3 — Background outbox drain is not tenant-aware (durability gap) [2026-06-10]

### Confirmed, NOT fixed here (drain path owned by snapshot owner)

`OutboxMessage : TenantEntity` is `ITenantOwned`, so the global query filter
applies `TenantId == CurrentTenantIdOrEmpty`, which falls back to `Guid.Empty`
when `CurrentTenantId` is null. `OutboxRepository.GetPendingAsync` does **not**
`IgnoreQueryFilters`. The Hangfire recurring `OutboxDrainJob` runs with no tenant
scope (`CurrentTenantId == null`), so the filter collapses to
`tenant_id == Guid.Empty` and the background drain returns **zero** real-tenant
rows.

Impact (LOW): the happy path drains in-request via `OutboxDrainBehavior` (runs
inside the acting tenant's scope), so this only affects messages that were
**Deferred** in-request (`SkippedClosedPeriod` / `SkippedUnmapped`) or left
**Pending** after an in-request failure — they are never picked up by the
background safety net. No cross-tenant leak (safe-by-omission).

### Required fix (drain-path change — snapshot owner)

Make the background drain tenant-aware. Either:

1. Have `OutboxDrainJob` enumerate distinct tenant ids that have Pending/Deferred
   messages (via an `IgnoreQueryFilters` admin query) and `PushScope` each tenant
   before `DrainAsync`; OR
2. Add an `IgnoreQueryFilters` admin `GetPendingAsync` that returns each row +
   its `TenantId`, and set the tenant context per message before processing.

No schema change required — this is a behavioral change to the
`OutboxDrainJob` / `OutboxProcessor` drain path. Tracked as **ERP-OUTBOX-001**.

## TEST-PROJECT-BLOCK — parallel-agent glass DTO break [2026-06-10]

`server/tests/CoreAlign.Application.Tests/GlassEnclosure/Templates/CreateProjectFromTemplateHandlerTests.cs:48` constructs `GlassProjectDto` with the OLD arity — the parallel agent added a required `PolygonVerticesJson` parameter to the DTO but did not update this test. CS7036, blocks compilation of the ENTIRE `CoreAlign.Application.Tests` project.

**Impact on Sprint 13:** the 13 BugHunt regression tests (BugHuntA/B/C/D, proving the 12 financial-core fixes) are committed to the real tree but **cannot be run in-tree** until the glass DTO test compiles. They were verified GREEN in an isolated source copy by the workflow's independent final-check.

**Verified independently this session (real working tree):**

- `CoreAlign.Domain` build → 0 errors
- `CoreAlign.Application` build → 0 errors
- `CoreAlign.Infrastructure` build → 0 errors (all 12 financial fixes compile clean)
- Fix markers present in tree: `GLPostingKey.WithholdingReceivable=12`, `Payment.Void` Status==Void guard (line 168), `SalesGLPostingHandlers` TaxableTotal revenue base, `MarkInvoiceAsPaidCommandHandler` IsFinalized guard, `Order.IsTransitionAllowed`, `AllocationService.SyncProductStockAsync`.

**Owner:** parallel agent (glass enclosure). One-line fix: add the `PolygonVerticesJson` argument to the test's `GlassProjectDto(...)`. NOT touched this session per the user's "skip the 3D/glass region" directive.

## Sprint 13 — 12 confirmed financial-core bugs fixed (summary)

Adversarial bug-hunt (4 lenses → refute-by-default verify → fix). All migration-free, RED→GREEN tested.

| id  | sev            | bug                                                                                                                                                                                                                          |
| --- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| A-1 | CRITICAL       | Withholding-tax invoices understate GL revenue + never book the stopaj receivable (193). `revenue = Total − Tax` subtracted withholding; no Withholding GL line.                                                             |
| A-2 | CRITICAL       | Credit note didn't reverse origin when origin had header discount / shipping / rounding (header adjustments not copied → over-credit).                                                                                       |
| C-1 | CRITICAL       | `Payment.Void()` had no terminal guard → double-void double-credits Cash / double-debits AR.                                                                                                                                 |
| C-2 | CRITICAL       | `MarkInvoiceAsPaid` guard omitted Void → a voided invoice flips to Paid + phantom ledger movement.                                                                                                                           |
| A-3 | HIGH           | Payment GL/ledger ignored `Payment.ExchangeRate` (hardcoded 1m/TRY) → multi-currency AR never clears.                                                                                                                        |
| B-1 | HIGH           | StockCount.Post applied a stale snapshot-time variance against live OnHand → silent stock loss if stock moved during the count.                                                                                              |
| B-2 | HIGH           | `AllocationService` receipt updated only StockItem.OnHand, not Product.StockQuantity → real stock unsellable (false InsufficientStock).                                                                                      |
| B-3 | HIGH           | `AllocationService` issue drained only StockItem.OnHand → order-confirm guard reads stale Product.StockQuantity → phantom over-sell.                                                                                         |
| C-3 | MEDIUM         | `Order.MarkFullyShipped` bypassed the FSM → a Delivered order could revert to Shipped + re-emit OrderShippedEvent.                                                                                                           |
| D-1 | MEDIUM         | 4 financial GetById handlers (GLAccount/JournalEntry/AccountingPeriod/VendorBill) returned null→200 instead of 404 (Sprint 11 Payment pattern recurrence).                                                                   |
| D-2 | LOW            | 4 period/price mutation handlers threw BCL KeyNotFoundException → 500 instead of 404.                                                                                                                                        |
| D-3 | LOW (DEFERRED) | Background outbox drain runs with null tenant → tenant filter collapses to Guid.Empty → deferred/failed GL postings never retried by the safety net. Needs tenant-aware drain (ERP-OUTBOX-001, snapshot-owner / drain-path). |

## TEST-PROJECT-BLOCK — RESOLVED [2026-06-10]

Unblocked: added the missing `PolygonVerticesJson: null` named-arg to `CreateProjectFromTemplateHandlerTests.BuildProjectDto` (one-line test-only fix; the parallel agent's 3D/glass source untouched). User confirmed the parallel agent is on the 3D-drawing side and not touching tests.

**In-tree verification now complete:**

- `CoreAlign.Application.Tests` compiles → 0 errors.
- BugHunt regression tests (the 12 financial-core fixes): **13/13 PASS in-tree**.
- Full Application suite: 1583 pass / 5 fail — the 5 are 100% parallel-agent (2 Installation checklist + 3 GlassEnclosure WorkOrderRevisions), zero financial-core failures.
- Domain/Application/Infrastructure build 0/0.

Sprint 13's financial-core bug-hunt is fully closed and proven in the real working tree.

## ERP-CONCUR-001 — RESOLVED [2026-06-10]

StockItem (warehouse-level OnHand/Reserved/AvgCost) now participates in optimistic concurrency — closes the Sprint-13 confirmed lost-update gap on concurrent stock mutation.

**Applied (migration-free for tests, hand-authored migration for prod, snapshot UNTOUCHED per user directive):**

- Domain: `StockItem : TenantEntity, IHasConcurrencyToken` + `long ConcurrencyToken` + `BumpConcurrencyToken()` (mirrors ProductVariant).
- Infrastructure: `StockItemConfiguration` → `.Property(s => s.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L)`.
- Migration (hand-authored): `20260615000000_Phase71StockItemConcurrencyToken.cs` + `.Designer.cs` → `AddColumn concurrency_token bigint NOT NULL default 0` on `stock_items`. **CoreAlignDbContextModelSnapshot.cs NOT touched.**
- Tests: `Inventory/StockConcurrencyTokenTests.cs` — flipped the negative guard to a positive guarantee (`StockItem_now_carries_concurrency_token_ERP_CONCUR_001_closed`) + added a real two-context race (`Two_racing_issues_on_same_StockItem_...`) proving concurrent ApplyIssue → `DbUpdateConcurrencyException` (→ 409 via ConcurrencyTokenBehavior) + no lost update (OnHand stays 7). 5/5 green.

**⚠️ SNAPSHOT-OWNER FOLLOW-UP (required):** `CoreAlignDbContextModelSnapshot.cs` was intentionally left without the StockItem `ConcurrencyToken` property. The runtime EF model now includes it (via config), so the NEXT `dotnet ef migrations add` will diff model-vs-snapshot and try to re-add `stock_items.concurrency_token` (duplicate AddColumn that fails on a DB where Phase71 is already applied). The snapshot owner MUST add the `ConcurrencyToken` property line to the `StockItem` entity in `CoreAlignDbContextModelSnapshot.cs` (mirror the ProductVariant `ConcurrencyToken` snapshot lines) so future scaffolds stay clean. Until then, do NOT run `migrations add` without first reconciling this one property.

**Note on the Phase71.Designer.cs empty BuildTargetModel:** intentional (INVARIANTS-28 hand-authored pattern). It does not affect `Migrate()` runtime apply (which uses Up()); it only means tooling diffs against this specific migration are unreliable — superseded once the snapshot owner reconciles.

## ConcurrencyTokenBehavior force-overwrite path — DEAD CODE (no action) [2026-06-10]

`IForceConcurrencyOverride` is defined but implemented by ZERO commands (only referenced inside `ConcurrencyTokenBehavior` itself). The `ResolveForceOverwriteAsync` branch (which re-invokes `next()` and would double-apply the handler's mutation) is therefore unreachable — a latent hazard, not a live bug. If a command ever adopts `IForceConcurrencyOverride`, the re-run-next() approach must be replaced with a save-level retry (the behavior cannot currently retry SaveChanges in isolation). Logged for awareness; no edit this session.
