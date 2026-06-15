# Sprint 12 Blockers

## Group A — ERP-IDEMP-001 ApplyVendorPayment idempotency (natural key) [2026-06-10]

### Resolved: ApplyVendorPaymentCommand double-apply on retry

ERP-IDEMP-001 fixed WITHOUT a migration, using a NATURAL idempotency key
`(VendorPaymentId, VendorBillId)` instead of a new client-supplied `OperationId`
column (migration was forbidden this sprint — snapshot owned by the parallel agent).

**Fix shipped:**

1. `IVendorPaymentApplicationRepository.GetByPaymentAndBillAsync(paymentId, billId, ct)`
   added (interface `Purchasing.Repositories.cs` + impl `AccountsPayableRepositories.cs`).
   Returns the existing tenant-scoped application for the pair, or null.
2. `ApplyVendorPaymentHandler` now queries `GetByPaymentAndBillAsync` immediately after
   loading payment + bill; if an application already exists it returns that existing
   application's DTO idempotently — no 2nd `RecordApplication`, no 2nd `RecordPayment`,
   no 2nd `VendorPaymentApplication` row.
3. `ApplyVendorPaymentCommand` was already `ITransactionalRequest`, so the whole apply
   already runs inside one transaction via `TransactionBehavior` — the existence check +
   mutation are atomic per request.

`ApplyVendorPaymentIdempotencyTests` un-skipped (the `[Fact(Skip=ERP-IDEMP-001)]` is
gone) and proves: (a) single apply posts once; (b) retry of the same `(payment, bill)`
does NOT double-apply and returns the existing application; (c) the same payment applied
to a DIFFERENT bill still records a second application.

### Follow-up for snapshot owner: DB unique index (HARD guarantee)

The natural-key check inside the transaction closes the network-retry double-apply gap,
but for a true race-proof guarantee under concurrent applies of the same payment to the
same bill, a DB unique index is required. The snapshot owner (the agent that owns
`CoreAlignDbContext` / `CoreAlignDbContextModelSnapshot`) should add:

```
UNIQUE INDEX ix_vendor_payment_applications_tenant_payment_bill
    ON vendor_payment_applications (tenant_id, vendor_payment_id, vendor_bill_id)
```

This was NOT hand-authored this sprint (snapshot drift risk while the parallel agent is
mid-flight). Until it lands, the handler-level `GetByPaymentAndBillAsync` + single
transaction is the guard. Once the index exists, the handler can additionally translate a
unique-violation on insert into the same idempotent replay.

### Customer-side ApplyPayment audited + fixed (same gap class)

STEP 4 audit found the customer-side analog: `Payment.Apply(invoiceId, ...)` added a fresh
`PaymentApplication` on every call with no `(PaymentId, InvoiceId)` dedup, so a retry of
`ApplyPaymentCommand` double-credited the invoice + double-debited the payment.

**Fix shipped (natural key, no migration):**

- `Payment.Apply` now self-dedups against its own loaded `Applications` collection: if an
  application already exists for `invoiceId` it returns the existing one without mutating
  `AppliedAmount` / adding a row.
- `ApplyPaymentHandler` skips the `invoice.RecordPayment(...)` call for any invoice already
  present in `payment.Applications`, so the invoice side is not double-credited on retry.
- `ApplyPaymentIdempotencyTests` (new) proves same-invoice retry is a no-op and distinct
  invoices each record once.

`ConfirmPayment` (customer + implicit vendor) and `VoidVendorPayment` already self-guard
(`Confirm` throws on non-Draft; `VendorPayment.Void` throws `VendorPaymentAlreadyVoided`),
so they are not double-mutation vectors. Customer-side `VoidPayment` does not guard against
double-void and could double-reverse invoices on retry — NOT fixed this sprint (touches the
fx-modified `Payment.cs`/handler hot path mid-parallel-edit); logged here for a follow-up.

## Group B — ERP-IDEMP-002 IssueCreditNote + ApproveOrder idempotency [2026-06-10]

### Resolved: IssueCreditNoteCommand double-issue on retry (ERP-IDEMP-002)

Fixed WITHOUT a migration using layered idempotency (durable natural key + cache fingerprint),
not a new DB `OperationId` column (migration forbidden — snapshot owned by the parallel agent).

**Fix shipped (`IssueCreditNoteCommandHandler`):**

1. DURABLE natural key — when the command carries `ReturnRequestId` (already a real persisted
   column on `Invoice`, set on every return-driven credit note), the handler queries
   `GetCreditNotesForInvoiceAsync` and replays the existing non-cancelled/non-void credit note
   whose `ReturnRequestId` matches. This is the real production path (`ReceiveReturnedItemsCommandHandler`
   passes the return id) and is cache-eviction-proof.
2. CACHE key — `IssueCreditNoteCommand` gained an optional `Guid? OperationId`, and the handler
   gained an OPTIONAL `IDistributedCacheService? cache = null` ctor param. The param is optional so
   the fixed-signature `IssueCreditNoteCommandHandlerTests` (5 tests) do not churn; in production
   DI auto-injects the registered `IDistributedCacheService` (Redis or InMemory), so cache-based
   idempotency is live. Key = `Generic` region + `BuildKey(tenant, fingerprint)`, TTL 10 min.
3. CONTENT fingerprint — when no `OperationId` is supplied, the fingerprint is a SHA256 over
   `(InvoiceId, sorted lineId:qty, ReturnRequestId)`, covering the pure network-retry window.

`IssueCreditNoteIdempotencyTests` un-skipped (`[Fact(Skip=ERP-IDEMP-002)]` gone) and now proves:
same-command retry suppresses the duplicate `AddAsync` and burns no new sequence; same-`OperationId`
retry returns the same credit note id; same-`ReturnRequestId` retry replays durably.

**Design note / deviation from the prescribed option:** the sprint directive's preferred option
(b) was pure cache-based idempotency keyed on `OperationId`. Pure cache-only was insufficient here
because (i) the real prod caller (Returns) supplies no `OperationId`, and (ii) cache is best-effort.
So the durable `ReturnRequestId` natural key is the PRIMARY guard and the cache fingerprint is the
secondary network-retry guard. Adding `IDistributedCacheService` as a REQUIRED ctor dep was rejected
because it would have forced churn on the fixed-4-arg `IssueCreditNoteCommandHandlerTests`; the
optional-param approach keeps those green while still being live in production.

### Follow-up for snapshot owner: durable OperationId column (HARD guarantee)

Cache-based dedup is best-effort (evicts after TTL / under memory pressure). For a race-proof,
durable network-retry guarantee independent of the Returns path, add a client-supplied
`operation_id uuid` column to `invoices` (or a dedicated credit-note idempotency table) with a
partial-unique index `(tenant_id, origin_invoice_id, operation_id)` for credit notes. The handler
would then prefer a `GetByOperationIdAsync` lookup over the cache. NOT hand-authored this sprint
(snapshot drift risk while the parallel agent is mid-flight).

### Review note (Group B Phase-1 review): cache-set-before-commit = latent phantom-replay

The cache `SetAsync` runs inside the handler body, i.e. BEFORE `SaveChangesBehavior` and BEFORE
`TransactionBehavior.CommitAsync`. If SaveChanges or the commit fails and the transaction rolls
back, the cached credit-note DTO survives (cache is non-transactional) and a retry would replay a
DTO for a credit note that was never persisted (caller then `AttachCreditNote`s a dangling id).
This is LATENT, not live: the sole caller `ReceiveReturnedItemsCommandHandler` always passes
`ReturnRequestId`, so the DURABLE re-query path is taken every time and the cache path is currently
unreachable. BEFORE any direct-API caller that omits `ReturnRequestId` is added, EITHER move the
cache write to a post-commit outer behavior, OR land the durable `operation_id` column above and
drop the best-effort cache layer. Verified by red-test: neutralizing the durable + cache guards
turns 3 of the 4 idempotency facts red, confirming the guards are load-bearing.

### ApproveOrder idempotency — already correct, just verified (no code change)

`ApproveOrderHandler` needed no change: `Order.Approve()` self-guards via the FSM — it throws
`InvalidOrderStatusTransitionException` when `Status != Submitted`, and the handler calls
`order.Approve()` BEFORE `SaveChangesAsync`, so a retry against an already-Approved order throws
before any second save / second `OrderApprovedEvent`. `ApproveOrderIdempotencyTests` (already
un-skipped in the repo) pass as-is: first approval transitions + saves once; second approval is
rejected and the status stays Approved.

## Group C — Stock + money mutation correctness coverage (rule-16 verification) [2026-06-10]

Added 31 new rule-16 verification tests across Inventory / Invoices / Accounting. All green.
No production code changed except: NONE — every guarantee tested was already implemented
correctly, EXCEPT one real gap surfaced and documented below (ERP-CONCUR-001, migration-bound).

New test files:

- `server/tests/CoreAlign.Application.Tests/Inventory/NegativeStockRejectionTests.cs` (10) —
  Issue/Adjust/Allocate reject driving on-hand below zero; ATP guard on reserve; explicit
  backorder (`allowNegative`) goes negative; `ProductVariant.AdjustStock` rejects below zero;
  exact-to-zero is allowed.
- `server/tests/CoreAlign.Application.Tests/Inventory/StockConcurrencyTokenTests.cs` (4) —
  REAL Sqlite two-context race on `ProductVariant` (holds `StockQuantity`, implements
  `IHasConcurrencyToken`): one decrement wins, the other gets `DbUpdateConcurrencyException`,
  no lost update (verified reload). `ConcurrencyTokenBehavior` translates that to a 409
  `DomainConcurrencyException`. Token bump verified. Plus the ERP-CONCUR-001 guard test.
- `server/tests/CoreAlign.Application.Tests/Invoices/MoneyRoundingBoundaryTests.cs` (6) —
  repeating-decimal unit prices (33.333 / 16.6667 / 0.005), Σ rounded lines == header to 4dp,
  per-line tax rounding contract, 17-line basket no penny-drift, credit-note rounding parity.
- `server/tests/CoreAlign.Application.Tests/Accounting/GLBalanceInvariantTests.cs` (6) —
  Σ debits == Σ credits on posted entries for sales invoice, reversing credit note, vendor
  payment, customer receipt, foreign-currency residual correction; unbalanced input never
  reaches a Posted entry.
- `server/tests/CoreAlign.Application.Tests/Inventory/StockCountPostIdempotencyTests.cs` (3) —
  second `PostStockCountCommand` throws `InvalidStockCountStateException` (Status==Posted, not
  Reconciliation) → exactly one `AdjustAsync` + one GL enqueue across both calls.
- `server/tests/CoreAlign.Application.Tests/Inventory/MoneyDecimalTypeAuditTests.cs` (3) —
  reflection audit: NO float/double property or backing field on any `TenantEntity`/`BaseEntity`;
  spot-checks stock + invoice money/quantity fields are `decimal`. (Only `double` in Domain is
  `PaymentBehavior.AvgDaysToPayment`, a read-side analytics projection record, not a persisted
  money/quantity entity field — acceptable.)

### ERP-CONCUR-001 — StockItem lacks IHasConcurrencyToken (migration-bound, NOT fixed)

`StockItem` (`server/src/CoreAlign.Domain/Entities/StockItem.cs`) holds the warehouse-level
stock balances (`OnHand`, `Reserved`, `AvgCost`) but does NOT implement `IHasConcurrencyToken`,
and `StockItemConfiguration` (`Persistence/Configurations/InventoryConfigurations.cs`) declares
NO `.IsConcurrencyToken()`. This violates INVARIANTS line 31 ("stock/balance-holding
TenantEntity left without a concurrency token"). Concurrent `AllocationService.ApplyIssueAsync` /
`ApplyAdjustmentAsync` commits on the same `StockItem` row can therefore lost-update each other
at the database (the last writer silently wins) instead of one getting a 409.

Sibling stock-holding entity `ProductVariant` DOES implement the token (and is proven correct by
`StockConcurrencyTokenTests`), so the pattern is established — `StockItem` just needs to follow it.

**Why not fixed this sprint:** closing it requires a NEW `concurrency_token bigint NOT NULL
DEFAULT 0` column on `stock_items` → a migration + `CoreAlignDbContextModelSnapshot` change, both
FORBIDDEN this sprint (snapshot owned by the parallel agent, hand-authoring drift risk too high).

**Fix (follow-up for snapshot owner):**

1. `StockItem : TenantEntity, IHasConcurrencyToken` — add `long ConcurrencyToken { get; private set; }`
   - `void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;` (mirror `ProductVariant`).
2. `StockItemConfiguration`: `builder.Property(s => s.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);`
3. Migration: `ALTER TABLE stock_items ADD COLUMN concurrency_token bigint NOT NULL DEFAULT 0;`
4. Flip `StockConcurrencyTokenTests.StockItem_lacks_concurrency_token_documented_gap_ERP_CONCUR_001`
   to assert `IsAssignableFrom == true` and add a real Sqlite two-context `ApplyIssue` race test
   mirroring the `ProductVariant` one.

The guard test `StockItem_lacks_concurrency_token_documented_gap_ERP_CONCUR_001` asserts the CURRENT
(broken) reality so the suite stays green AND the gap is loudly visible; it will fail the moment the
token is added, forcing the follow-up to flip it to the positive guarantee.

## Phase-1 Review (Group C) — flaky idempotency tests under parallel xUnit [2026-06-10] — FIXED

### ERP-TEST-FLAKY-001 NSubstitute idempotency tests race under xUnit collection parallelism

Cold-read review of the un-skipped idempotency tests surfaced a real test-suite reliability defect
(NOT a production bug). Running `dotnet test --filter Idempotency` (the 5 idempotency handler test
classes only) intermittently failed 3 tests:

- `ApplyVendorPaymentIdempotencyTests.RetryWithSameVendorPaymentAndBill_IsIdempotent_DoesNotDoubleApply`
  → `AddAsync` `Received(2)` instead of `Received(1)`.
- `ApplyPaymentIdempotencyTests.Apply_SameInvoiceTwice_DoesNotDoubleApply` → `Applications.Count == 2`.
- `IssueCreditNoteIdempotencyTests.RetryFromSameReturnRequest_ReplaysExistingCreditNoteDurably`
  → second credit note added.

**Root cause:** xUnit runs distinct test collections in parallel by default. NSubstitute's
`SubstitutionContext` holds the pending argument-matcher queue (`Arg.Do` / `Arg.Any`) in an
`AsyncLocal`/thread-local; when the idempotency handler tests' `async` continuations hop thread-pool
threads while a sibling class is concurrently configuring/checking its own substitutes, a matcher
enqueued by one class can be consumed by another, corrupting the `Received(1)` count. Each class
passes in ISOLATION and the FULL 1575-test suite passed (the 5 classes get scheduled far apart), so the
"baseline green" was scheduling luck. A filtered CI run or a busier box reproduces it — and flaky
money-correctness tests get muted, defeating their purpose.

**Fix (test-only, no production/migration/snapshot change):**

- New `server/tests/CoreAlign.Application.Tests/Idempotency/IdempotencyTestCollection.cs` —
  `[CollectionDefinition(DisableParallelization = true)]` (private ctor for SonarS1118; can't be static
  because xUnit needs it as a collection marker).
- All 5 idempotency classes (`ApplyVendorPayment`, `ApplyPayment`, `IssueCreditNote`, `ApproveOrder`,
  `MergeCustomers`) now carry `[Collection(IdempotencyTestCollection.Name)]` so they run serially
  relative to each other — the race window is closed without serializing the whole 1575-test suite.

**Verification:** the previously-flaky `--filter Idempotency` run is now 21/21 green across 4 repeats;
combined Group-C + idempotency filter 49/49 green across 3 repeats; full Application suite 1573 pass /
2 fail (the 2 are the pre-existing `Installation.*` failures in FORBIDDEN parallel-agent territory,
deterministic and unrelated). Production build 0/0.
