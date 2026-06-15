# MRP Tranche T1 — Buildable Design (This Sprint)

> Companion to `docs/modules/mrp-analysis.md`. Concrete design for the leap from shallow reorder-point to real MRP.
> Three non-overlapping build groups with explicit file scopes. Follows CLAUDE.md 1–16, INVARIANTS.md, and the parallel-agent guard (no ModelSnapshot / *.csproj edits; idempotent migration applied this pass; blocker follow-up in `docs/mrp-blockers.md`).

---

## 0. Design Principles & Constraints

1. **Reuse, don't replace.** The existing N+1-safe batch loaders (`LoadCandidateBatchAsync`), the `PurchaseRequisition` aggregate + FSM, the `IProductComponentRepository.GetTreeForProductsAsync` wave-batched traversal, and `IStockItemRepository` rollups are the primitives. The engine is **extended**, not rewritten.
2. **Compute layer is pure & persistence-agnostic.** Group A produces value objects (a "plan result") from in-memory snapshots loaded by repositories. This makes the planning math unit-testable on Sqlite/InMemory without a DB write path (BOM correctness, lot-sizing, lead-time offset, exception generation, forecast/safety-stock all testable as pure functions).
3. **Persistence is a separate concern (Group B).** A plan **run** can be executed read-only (preview, for the workbench grid) or **committed** (persist planned orders + action messages + pegging, emit outbox event). Read-only preview powers the grid without writes; commit is the idempotent, transactional path.
4. **Time-phasing constraint (hard).** `PurchaseOrderLine` carries **no date**; only header `PurchaseOrder.ExpectedDate` exists. Scheduled receipts are therefore bucketed by their PO header `ExpectedDate` (fallback: `today + remaining lead time`, else bucket 0). Document this approximation; per-line PO due dates are a future enhancement (out of T1 scope, noted as ERP-MRP-002).
5. **Money/quantity = `decimal`** (rule 16). No `float`/`double` on any entity (reflection audit gate, INVARIANTS §65). Display rounding only (decimal precision is display-only).
6. **Tenant isolation** on every query and every new endpoint (INVARIANTS §12, §49). Every new list endpoint paginated (INVARIANTS §15). Every new Command/Query gets a paired FluentValidator + ≥2 tests (INVARIANTS §21).
7. **Migration:** idempotent SQL (`ADD COLUMN/CREATE TABLE/CREATE INDEX IF NOT EXISTS`, `DROP ... IF EXISTS` in Down), applied this pass, snapshot untouched. Phase number after the latest (`Phase71StockItemConcurrencyToken @ 20260615`) → **`Phase72MrpPlanning @ 20260616000000`**.

---

## 1. Domain Model (new)

### 1.1 Enums (new) — `CoreAlign.Domain/Enums/MrpEnums.cs`

```csharp
public enum LotSizingPolicy { LotForLot = 0, FixedOrderQuantity = 1, MinMax = 2, EconomicOrderQuantity = 3, PeriodOrderQuantity = 4 }

public enum MrpBucketKind { Day = 0, Week = 1 }

public enum MrpActionType {
    Release = 0,            // release a new planned order now
    RescheduleIn = 1,       // existing scheduled receipt needed EARLIER
    RescheduleOut = 2,      // existing scheduled receipt needed LATER
    Expedite = 3,           // shortage inside lead time — expedite
    CancelSupply = 4,       // scheduled receipt no longer needed
    BelowSafetyStock = 5,   // projected on-hand dips below safety stock
    ProjectedStockout = 6   // projected on-hand goes negative
}

public enum MrpActionSeverity { Info = 0, Warning = 1, Critical = 2 }

public enum MrpPlanRunStatus { Preview = 0, Committed = 1 }

public enum ForecastModel { MovingAverage = 0, ExponentialSmoothing = 1 }
```

### 1.2 `Product` additive fields (lot-sizing policy)

Add to `Product` (private set + a single `SetPlanningPolicy(...)` mutator; do **not** widen the existing `Update(...)` signature to avoid churn — add a dedicated method):

| Field | Type | Default | Purpose |
| --- | --- | --- | --- |
| `LotSizingPolicy` | `LotSizingPolicy` | `MinMax` | which lot-sizing rule the engine applies |
| `FixedOrderQuantity` | `decimal` | `0` | FOQ batch size (when policy = FOQ) |
| `OrderMultiple` | `decimal` | `0` | round planned qty up to this multiple (0 = no rounding) |
| `EoqAnnualDemand` | `decimal` | `0` | optional override; if 0, derive from forecast |
| `OrderingCost` | `decimal` | `0` | EOQ S (cost per order) |
| `HoldingCostRate` | `decimal` | `0` | EOQ H as fraction of unit cost/yr |
| `ServiceLevelTarget` | `decimal` | `0` | e.g. `0.95`; drives z-factor for safety stock (0 = use stored SafetyStock as-is) |

`MinOrderQuantity`, `MinStock`, `MaxStock`, `SafetyStock`, `LeadTimeDays`, `ReorderPoint` already exist and are reused.

> These 7 columns go on the existing `products` table via the idempotent migration. `SafetyStock` becomes **computed-or-stored**: if `ServiceLevelTarget > 0`, the engine computes `z·σ·√LT` and uses `max(stored, computed)`; else stored value as today.

### 1.3 `MrpPlanRun` (new aggregate, persisted) — `CoreAlign.Domain/Entities/Mrp/MrpPlanRun.cs`

Header for one MRP execution. `TenantEntity, IHasConcurrencyToken`.

| Field | Type | Notes |
| --- | --- | --- |
| `Number` | `string` | sequence `MrpPlanRunNumber` ("MRP", width 5) |
| `Status` | `MrpPlanRunStatus` | Preview never persisted; only Committed runs are written |
| `AsOfDateUtc` | `DateTime` | plan anchor (UTC-normalized in ctor, INVARIANTS §24) |
| `BucketKind` | `MrpBucketKind` | Day/Week |
| `HorizonDays` | `int` | planning horizon |
| `IdempotencyKey` | `string` | natural key `"{AsOfDate:yyyyMMdd}:{BucketKind}:{HorizonDays}"` — dedup re-runs (fixes MRP-BUG-2) |
| `ProductsEvaluated` / `PlannedOrderCount` / `ActionMessageCount` | `int` | run summary |
| `CreatedByUserId` | `Guid` | planner |
| Children: `PlannedOrders`, `ActionMessages` | collections | |

Methods: ctor (UTC-normalize), `AddPlannedOrder`, `AddActionMessage`, `MarkCommitted`, `BumpConcurrencyToken`.

### 1.4 `MrpPlannedOrder` (new, persisted) — `CoreAlign.Domain/Entities/Mrp/MrpPlannedOrder.cs`

One planned supply order produced by netting. T1 routes all to purchase (make-vs-buy in T2).

| Field | Type | Notes |
| --- | --- | --- |
| `PlanRunId` | `Guid` | FK |
| `ProductId` | `Guid` | |
| `LowLevelCode` | `int` | BOM depth (0 = end item); planning order |
| `Quantity` | `decimal` | lot-sized net requirement |
| `DueDateUtc` | `DateTime` | when stock is needed (receipt date) |
| `ReleaseDateUtc` | `DateTime` | `DueDate − LeadTimeDays` (lead-time offset) |
| `PreferredSupplierId` | `Guid?` | |
| `EstimatedUnitCost` | `decimal` | |
| `SourcePolicy` | `LotSizingPolicy` | which rule produced the qty |
| `IsFirmed` | `bool` | default false (T3 will honor on re-plan) |
| `ConvertedRequisitionId` | `Guid?` | set when released into a requisition |

### 1.5 `MrpActionMessage` (new, persisted) — `CoreAlign.Domain/Entities/Mrp/MrpActionMessage.cs`

The planner's triage queue.

| Field | Type | Notes |
| --- | --- | --- |
| `PlanRunId` | `Guid` | FK |
| `ProductId` | `Guid` | |
| `ActionType` | `MrpActionType` | Release / Reschedule-In/Out / Expedite / Cancel-Supply / Below-Safety / Projected-Stockout |
| `Severity` | `MrpActionSeverity` | |
| `Quantity` | `decimal` | suggested/affected qty |
| `CurrentDateUtc` | `DateTime?` | existing receipt date (for reschedule) |
| `SuggestedDateUtc` | `DateTime?` | needed date |
| `RelatedPurchaseOrderId` | `Guid?` | for reschedule/cancel of existing PO |
| `RelatedPlannedOrderId` | `Guid?` | for Release |
| `DaysUntilStockOut` | `int` | triage sort |
| `IsDismissed` / `DismissedByUserId` / `DismissedAtUtc` | snooze/dismiss state | |
| `Message` | `string` | rendered human text |

### 1.6 `MrpPegging` (new, persisted) — `CoreAlign.Domain/Entities/Mrp/MrpPegging.cs`

Links a component requirement to the parent demand that drove it (captured during explosion).

| Field | Type | Notes |
| --- | --- | --- |
| `PlanRunId` | `Guid` | FK |
| `ComponentProductId` | `Guid` | the child needing supply |
| `RequirementQuantity` | `decimal` | gross req contributed by this source |
| `DueDateUtc` | `DateTime` | when needed |
| `SourceKind` | `string` | `"SalesOrder"` / `"PlannedOrder"` / `"Forecast"` |
| `SourceParentProductId` | `Guid?` | parent item (if dependent demand) |
| `SourceOrderLineId` | `Guid?` | originating sales order line (independent demand) |

---

## 2. Compute Value Objects (Group A — not persisted)

`CoreAlign.Application/Mrp/Planning/*.cs` (new folder).

- `MrpBucket(DateTime StartUtc, decimal GrossRequirements, decimal ScheduledReceipts, decimal ProjectedOnHand, decimal NetRequirements, decimal PlannedReceipts, decimal PlannedReleases)` — one time bucket.
- `MrpItemPlan(Guid ProductId, string Sku, string Name, int LowLevelCode, decimal OnHand, decimal SafetyStock, LotSizingPolicy Policy, IReadOnlyList<MrpBucket> Buckets, IReadOnlyList<PlannedOrderDraft> PlannedOrders, IReadOnlyList<ActionMessageDraft> Actions, IReadOnlyList<PeggingDraft> Pegs)`.
- `MrpPlanResult(DateTime AsOfUtc, MrpBucketKind BucketKind, int HorizonDays, int ProductsEvaluated, IReadOnlyList<MrpItemPlan> Items)` — the full preview payload.
- `PlannedOrderDraft`, `ActionMessageDraft`, `PeggingDraft` — pre-persistence shapes Group B maps to entities.

---

## 3. The Planning Algorithm (Group A core)

`MrpPlanningEngine` (new, pure, in `CoreAlign.Infrastructure/Mrp/Planning/MrpPlanningEngine.cs`) operates on an in-memory `MrpPlanningSnapshot` (loaded by `IMrpPlanningDataLoader`, §5):

1. **Low-level coding.** Build the BOM DAG from `IProductComponentRepository.GetTreeForProductsAsync` over all stock-tracked active products. Assign each product its **maximum** depth across all paths (low-level code). Plan items in **ascending** low-level-code order so a part is planned once, after every parent that consumes it.
2. **Independent demand → buckets.** Place committed `OrderLine` demand (`QuantityAllocated − QuantityShipped`) in the bucket of its parent order's `RequestedDeliveryDate`/`DueDate` (fallback bucket 0). Add forecast demand (§6) net of already-committed.
3. **Explosion (per level, descending parents).** For each parent's **planned release** (after lot-sizing + offset), multiply BOM `Quantity` and add the result as **dependent gross requirement** to each child in the bucket of the parent's **release** date. Write a `PeggingDraft(child ← parent, qty, dueDate)`.
4. **Scheduled receipts.** Bucket open `PurchaseOrderLine` remainder (`Quantity − QuantityReceived`) by PO header `ExpectedDate` (fallback `today + lead time`, else bucket 0).
5. **Time-phased netting per bucket** (the MRP record):
   - `ProjectedOnHand[t] = ProjectedOnHand[t-1] + ScheduledReceipts[t] + PlannedReceipts[t] − GrossRequirements[t]` (bucket 0 seeds with `onHand − reserved`).
   - `NetRequirement[t] = max(0, SafetyStock − (ProjectedOnHand[t] before planned receipt))`.
   - When `NetRequirement[t] > 0`, create a **planned receipt** sized by the lot-sizing policy (§4), then a **planned release** at `t − leadTime` buckets.
6. **Lead-time offset.** `ReleaseDate = DueDate − LeadTimeDays`. If the release date is in the past (shortage inside lead time), still release in bucket 0 and emit an **Expedite** action.
7. **Exception generation (§7).**

### 4. Lot-Sizing (Group A) — `LotSizingCalculator`

`Calculate(policy, netRequirement, product, periodNetReqs) → decimal`:
- **LotForLot:** exactly `netRequirement`.
- **FixedOrderQuantity:** smallest integer multiple of `FixedOrderQuantity ≥ netRequirement`.
- **MinMax:** `maxStockTarget − projectedAvailable` (today's behavior preserved; `maxStockTarget = MaxStock>0 ? MaxStock : ROP·2`).
- **EconomicOrderQuantity:** `EOQ = ceil(√(2·D·S / H))` where `D = annual demand` (`EoqAnnualDemand` or `avgDaily·365`), `S = OrderingCost`, `H = HoldingCostRate · unitCost`; if inputs are 0, fall back to MinMax. Then satisfy `netRequirement` in EOQ multiples.
- **PeriodOrderQuantity:** group net requirements across the next *N* buckets into one order (N derived from EOQ or a default of 1 week).
- **Post-processing for all policies:** round up to `OrderMultiple` (if >0), then enforce `MinOrderQuantity` (if set). Order of operations documented in the calculator and tested.

### 5. Data Loader (Group A) — `IMrpPlanningDataLoader` / `MrpPlanningDataLoader`

Loads one `MrpPlanningSnapshot` in a **bounded** number of round-trips (N+1 budget asserted in integration tests, INVARIANTS §34/§39):
- products (1) · BOM tree (wave-batched, O(depth)) · on-hand+reserved by product (1 batch) · open PO lines + PO `ExpectedDate` (1) · committed order lines + parent order dates (1) · demand history for forecast (1).
- Lives in `CoreAlign.Infrastructure/Mrp/Planning/`. Uses `_context.Set<T>()` and the existing repositories; **no DbContext edit** for reads.

### 6. Forecasting Upgrade (Group A) — `DemandForecaster`

- **MovingAverage:** existing behavior, but **fix MRP-BUG-4**: bucket demand by a stable demand date (use the order's date, not `OrderLine.UpdatedAtUtc`) and divide by **days-with-data** when computing per-day rates for σ.
- **ExponentialSmoothing:** `F[t] = α·A[t-1] + (1-α)·F[t-1]`, α default `0.3` (configurable const). Returns smoothed average daily demand + **σ of daily demand** for safety-stock.
- **Service-level safety stock:** `SS = z(ServiceLevelTarget) · σ_daily · √LeadTimeDays`. `z` from a small lookup table (0.90→1.2816, 0.95→1.6449, 0.975→1.96, 0.99→2.3263; interpolate otherwise). Effective safety stock = `ServiceLevelTarget>0 ? max(storedSafetyStock, computed) : storedSafetyStock`.

### 7. Exception Message Generation (Group A) — `ActionMessageGenerator`

From the netted plan per item:
- **Release** — a planned release exists in bucket 0..n (route to a requisition).
- **Expedite** — planned release date < today (shortage inside lead time).
- **Reschedule-In** — an open PO's `ExpectedDate` is **later** than the bucket where its quantity is first needed.
- **Reschedule-Out** — an open PO arrives in a bucket **before** any requirement consumes it (early receipt → excess).
- **Cancel-Supply** — an open PO whose quantity is never consumed within the horizon.
- **Below-Safety-Stock** — `ProjectedOnHand[t]` dips below `SafetyStock`.
- **Projected-Stockout** — `ProjectedOnHand[t] < 0`.
Each carries severity, quantity, current/suggested dates, related PO/planned-order id, and `DaysUntilStockOut` for sort.

---

## 8. Application Layer (contracts, handlers, validators)

New file `CoreAlign.Application/Mrp/MrpPlanningContracts.cs` (keep separate from existing `MrpContracts.cs` to avoid collision with co-edits):

**Queries (read-only preview — no writes):**
- `RunMrpPreviewQuery(DateTime? AsOfDateUtc, MrpBucketKind BucketKind = Day, int HorizonDays = 60) : IRequest<MrpPlanResultDto>` — runs the engine, returns the grid without persisting.
- `GetMrpItemPlanQuery(Guid ProductId, DateTime? AsOfDateUtc, MrpBucketKind, int HorizonDays) : IRequest<MrpItemPlanDto?>` — single-item drill (buckets + pegging).
- `ListMrpActionMessagesQuery(Guid? PlanRunId, MrpActionType?, MrpActionSeverity?, Guid? SupplierId, bool IncludeDismissed=false, int Page=1, int PageSize=25) : IRequest<PagedResult<MrpActionMessageDto>>` — the action queue (paginated).
- `GetMrpPeggingQuery(Guid PlanRunId, Guid ComponentProductId) : IRequest<IReadOnlyList<MrpPeggingDto>>`.
- `ListMrpPlanRunsQuery(int Page, int PageSize) : IRequest<PagedResult<MrpPlanRunDto>>`.

**Commands (Group B — transactional):**
- `CommitMrpPlanCommand(DateTime? AsOfDateUtc, MrpBucketKind, int HorizonDays, Guid OperationId) : IRequest<MrpPlanRunDto>, ITransactionalRequest` — runs the engine and **persists** plan run + planned orders + actions + pegging; idempotent on `IdempotencyKey` (re-run returns the existing run — fixes MRP-BUG-2).
- `ReleasePlannedOrdersCommand(Guid PlanRunId, IReadOnlyList<Guid> PlannedOrderIds, Guid OperationId) : IRequest<ReleaseResultDto>, ITransactionalRequest` — converts selected planned orders into purchase requisition(s) grouped by supplier; sets `ConvertedRequisitionId`. **Reuses `CreatePurchaseRequisitionHandler` semantics**; fixes the missing-SaveChanges (MRP-BUG-1) by following the manual path's `EnsureExists → SaveChanges → Consume` order.
- `FirmPlannedOrderCommand(Guid PlannedOrderId, decimal? OverrideQuantity, DateTime? OverrideDueDateUtc, Guid OperationId) : IRequest<MrpPlannedOrderDto>, ITransactionalRequest` — firm/adjust before release (T1 persists the flag; T3 honors it on re-plan).
- `DismissMrpActionMessageCommand(Guid ActionMessageId) : IRequest<Unit>, ITransactionalRequest` — snooze/dismiss.

**Handlers:** new `CoreAlign.Application/Mrp/MrpPlanningHandlers.cs`. Query handlers delegate to `IMrpPlanningService` (preview/drill), command handlers to `IMrpPlanningService` (commit/release/firm) + repositories.
**Validators:** new `CoreAlign.Application/Mrp/MrpPlanningValidators.cs` — one per Command/Query (INVARIANTS §21): horizon 1–365, page-size 1–200, release list non-empty, override qty `GreaterThan(0)` when present, `OperationId` non-empty on money/stock-mutating commands (INVARIANTS §26).

**Service interface** (extend, don't break the existing `IMrpService`): new `IMrpPlanningService` in `CoreAlign.Application/Mrp/IMrpPlanningService.cs`:
```csharp
Task<MrpPlanResult> RunPreviewAsync(DateTime asOfUtc, MrpBucketKind kind, int horizonDays, CancellationToken ct);
Task<MrpItemPlan?> GetItemPlanAsync(Guid productId, DateTime asOfUtc, MrpBucketKind kind, int horizonDays, CancellationToken ct);
Task<MrpPlanRun> CommitAsync(DateTime asOfUtc, MrpBucketKind kind, int horizonDays, Guid operationId, CancellationToken ct);
Task<ReleaseResult> ReleaseAsync(Guid planRunId, IReadOnlyList<Guid> plannedOrderIds, Guid operationId, CancellationToken ct);
```

---

## 9. Persistence + Migration (Group B)

### 9.1 EF configurations (new — via `ApplyConfigurationsFromAssembly`, NO DbContext edit)
`CoreAlign.Infrastructure/Persistence/Configurations/Mrp/`:
- `MrpPlanRunConfiguration` — table `mrp_plan_runs`; unique index `(tenant_id, idempotency_key)`; concurrency token; money/qty `numeric(18,4)`; dates `timestamptz`.
- `MrpPlannedOrderConfiguration` — `mrp_planned_orders`; FK→run (Cascade); index `(tenant_id, plan_run_id)`, `(tenant_id, product_id)`.
- `MrpActionMessageConfiguration` — `mrp_action_messages`; FK→run (Cascade); index `(tenant_id, plan_run_id, action_type)`, `(tenant_id, is_dismissed)`.
- `MrpPeggingConfiguration` — `mrp_peggings`; FK→run (Cascade); index `(tenant_id, plan_run_id, component_product_id)`.

Access via `_context.Set<MrpPlanRun>()` etc. — **no `DbSet` added to `CoreAlignDbContext`** (preferred path per the guard). If the parallel agent's tree makes `Set<T>()` insufficient, a single surgical disjoint `DbSet` Edit is the fallback (last resort).

### 9.2 Migration — `20260616000000_Phase72MrpPlanning.cs` (idempotent, applied this pass)
`Up()` via `migrationBuilder.Sql(...)`:
- `ALTER TABLE products ADD COLUMN IF NOT EXISTS lot_sizing_policy integer NOT NULL DEFAULT 2;` + the other 6 product columns (`fixed_order_quantity`, `order_multiple`, `eoq_annual_demand`, `ordering_cost`, `holding_cost_rate`, `service_level_target` — all `numeric(18,4) NOT NULL DEFAULT 0`).
- `CREATE TABLE IF NOT EXISTS mrp_plan_runs (...)`, `mrp_planned_orders`, `mrp_action_messages`, `mrp_peggings` with FKs + `tenant_id`.
- `CREATE UNIQUE INDEX IF NOT EXISTS ix_mrp_plan_runs_tenant_idempotency ON mrp_plan_runs (tenant_id, idempotency_key);` + the per-table indexes above (all `IF NOT EXISTS`).
- Sequence type: add `DocumentSequenceType.MrpPlanRunNumber` enum member (code-only; seeded lazily by `EnsureExistsAsync`).
`Down()`: `DROP TABLE IF EXISTS` (children first) + `ALTER TABLE products DROP COLUMN IF EXISTS ...`.
A `.Designer.cs` with empty `BuildTargetModel` per the INVARIANTS §28 hand-authored pattern. **`CoreAlignDbContextModelSnapshot.cs` NOT touched** → ERP-MRP-001 in `docs/mrp-blockers.md`.

### 9.3 Repositories (new)
`CoreAlign.Infrastructure/Repositories/MrpPlanRunRepository.cs` (+ interface in `CoreAlign.Domain/Interfaces/IMrpPlanRunRepository.cs`):
- `AddAsync(run)`, `GetByIdAsync(id, includeChildren)`, `GetByIdempotencyKeyAsync(key)` (dedup), `SearchPlanRunsAsync(page, pageSize)`, `SearchActionMessagesAsync(filters, page, pageSize)`, `GetPlannedOrdersAsync(planRunId, ids)`, `GetPeggingAsync(planRunId, componentProductId)`, `Update(run)`. All tenant-filtered, `AsNoTracking` on reads, paginated.

---

## 10. API (Group B) — `MrpController` additions (new methods, same controller)

All `[Authorize]`; gate planning mutations behind a `Planner`/`PurchasingManager` policy (follow the existing controller's `[Authorize]` convention; add `[Authorize(Roles = ...)]` where the repo's role scheme supports it). All list endpoints paginated. Responses wrapped in `ApiResponse<T>` (INVARIANTS §33).

| Method | Route | Maps to |
| --- | --- | --- |
| `GET` | `mrp/plan/preview?asOf=&bucket=&horizon=` | `RunMrpPreviewQuery` |
| `GET` | `mrp/plan/item/{productId:guid}?asOf=&bucket=&horizon=` | `GetMrpItemPlanQuery` |
| `POST` | `mrp/plan/commit` | `CommitMrpPlanCommand` |
| `GET` | `mrp/plan/runs?page=&pageSize=` | `ListMrpPlanRunsQuery` |
| `GET` | `mrp/action-messages?planRunId=&type=&severity=&supplierId=&includeDismissed=&page=&pageSize=` | `ListMrpActionMessagesQuery` |
| `POST` | `mrp/action-messages/{id:guid}/dismiss` | `DismissMrpActionMessageCommand` |
| `GET` | `mrp/pegging/{planRunId:guid}/{componentProductId:guid}` | `GetMrpPeggingQuery` |
| `POST` | `mrp/plan/{planRunId:guid}/release` | `ReleasePlannedOrdersCommand` |
| `POST` | `mrp/planned-orders/{id:guid}/firm` | `FirmPlannedOrderCommand` |

Fixed-path routes precede `{id:guid}` (INVARIANTS §27). Existing `dashboard`/`stock-projection`/`demand-forecast`/`generate-suggestions` untouched.

---

## 11. Frontend Workbench (Group C)

New page `src/pages/mrp/MrpWorkbenchPage.tsx` — 3-tab workbench + right-hand pegging drawer. The existing `MrpDashboardPage` becomes a thin summary that links into the workbench. The card-list `PurchaseRequisitionsPage` gains a status-aware action menu (incl. **Convert→PO**, wiring the orphaned `useConvertRequisition` — fixes MRP-BUG-3) and a reason prompt for reject/cancel (fixes MRP-BUG-7).

**Feature files** (`src/features/mrp/`):
- `model/mrp-planning.types.ts` — `MrpBucket`, `MrpItemPlan`, `MrpPlanResult`, `MrpActionMessage`, `MrpPlannedOrder`, `MrpPlanRun`, `MrpPegging`, enums.
- `api/mrpPlanningApi.ts` — preview/item/commit/runs/action-messages/dismiss/pegging/release/firm; `cachedGet` for reads, `mutate` + `invalidateHttpCache` for writes (mirror existing `mrpApi.ts`).
- `hooks/useMrpWorkbench.ts`, `hooks/useMrpActionMessages.ts`, `hooks/useMrpPlanRun.ts` — `useQuery`/`useMutation` wrappers (data-access-in-hooks rule).

**UI components** (`src/features/mrp/ui/`):
- `MrpPlanningGrid.tsx` — spreadsheet grid: one row per item, columns = buckets (Gross / Sched. Receipts / Proj-On-Hand / Net / Planned Releases); proj-on-hand cell turns red below ROP/safety. Replaces the CSS-bar chart.
- `MrpTimePhasedChart.tsx` — **recharts** supply/demand line + ROP reference line + below-ROP marker (replaces `StockProjectionChart`'s div bars).
- `ActionMessageQueue.tsx` — typed, sortable, multi-selectable exception table; row actions Release / Dismiss / Open-in-grid; bulk bar "Release selected".
- `PeggingDrawer.tsx` — item header + ABC/warehouse + time-phased bucket table + pegging (which SO/planned-order drove it) + forecast-vs-actual + Firm/Convert actions.
- `KpiStrip.tsx` — Stockout-Risk Items / Open Exceptions / Projected-Stockouts / On-Order counts.

**i18n:** all strings via `t("Mrp.*")`, `tr.json` + `en.json` synced (INVARIANTS §10). Dark-mode + responsive (CLAUDE.md §3). `logger.ts` not `console.*`. New cache TTL regex for `/mrp/plan/` and `/mrp/action-messages/` in `httpCache.ts` `TTL_RULES`.

---

## 12. Test Plan

### Unit (Group A — pure compute, Sqlite/InMemory not required)
`server/tests/CoreAlign.Application.Tests/Mrp/Planning/`:
- **BOM explosion correctness:** 3-level bill (A→B×2→C×3); low-level coding assigns shared component its **max** depth; gross req propagates `parent.release · qty`; single-plan-per-part. Diamond BOM (component used by two parents) sums correctly.
- **Lead-time offset:** release = due − leadTime; past release → bucket 0 + Expedite action.
- **Lot-sizing math:** one `[Theory]` per policy — L4L exact; FOQ ceil-to-multiple; MinMax = today's number (regression lock); EOQ = `√(2DS/H)` rounded + multiples; POQ groups N buckets; `OrderMultiple` + `MinOrderQuantity` post-processing order.
- **Exception generation:** each of the 7 `MrpActionType`s from a crafted netted scenario (early PO → Reschedule-Out; late PO → Reschedule-In; unconsumed PO → Cancel-Supply; sub-lead shortage → Expedite; below-SS / negative → those two).
- **Forecast/safety-stock math:** exponential smoothing α; σ over days-with-data; `z·σ·√LT` for 0.90/0.95/0.99; `ServiceLevelTarget=0` keeps stored SS (regression).
- **MRP-BUG-4 regression:** demand bucketed by order date not `UpdatedAtUtc`.

### Integration (Group B — endpoints, tenant isolation, N+1)
`server/tests/CoreAlign.Integration.Tests/MrpPlanningControllerIntegrationTests.cs`:
- **Fresh-tenant first run (MRP-BUG-1 regression):** commit on a tenant with **no** pre-seeded `PurchaseRequisitionNumber`/`MrpPlanRunNumber` sequence → 200, not 500. (RED-before by reverting the SaveChanges order.)
- **Idempotency (MRP-BUG-2):** two commits with the same `IdempotencyKey`/`OperationId` → one `MrpPlanRun`, identical result (assert row count == 1, not just 200).
- **Tenant isolation:** TenantA token + TenantB plan-run/action-message/pegging id → `{404,403}` only (INVARIANTS §40/§54). One cross-tenant `[Fact]` per new GET-by-id endpoint (append to `CrossTenantIsolationTests`).
- **N+1 budget:** `RunMrpPreviewQuery` over K products with BOMs stays within an explicit round-trip budget (products + BOM-waves(depth) + 4 batch loads); assert via `DbCommandRoundTripInterceptor` (INVARIANTS §34/§39). Budget justified in-test (BOM depth makes it depth-bounded, not constant — documented).
- **Release flow:** release N planned orders → grouped requisition(s) created, `ConvertedRequisitionId` set, FSM intact.

### Frontend (Group C)
`src/features/mrp/__tests__/` (vitest + RTL, INVARIANTS §44–46): grid renders buckets with red below-ROP cell; action queue multi-select + bulk release calls the mutation; pegging drawer shows source orders; convert-to-PO button reachable and calls `useConvertRequisition` (MRP-BUG-3 regression); reject/cancel prompt collects a reason (MRP-BUG-7). Accessible queries per INVARIANTS §55.

---

## 13. Three Build Groups (non-overlapping file scopes)

> Hard rule: a file appears in exactly ONE group. Shared types flow A→B→C by contract, not by co-editing. No group touches `CoreAlignDbContextModelSnapshot.cs` or `*.csproj`.

### GROUP A — Backend Planning Engine (pure compute; fixes MRP-BUG-4)
**Owns / creates:**
- `server/src/CoreAlign.Domain/Enums/MrpEnums.cs` (new enums)
- `server/src/CoreAlign.Application/Mrp/Planning/` (all VOs: `MrpBucket`, `MrpItemPlan`, `MrpPlanResult`, `*Draft`)
- `server/src/CoreAlign.Application/Mrp/IMrpPlanningService.cs`
- `server/src/CoreAlign.Infrastructure/Mrp/Planning/MrpPlanningEngine.cs`
- `server/src/CoreAlign.Infrastructure/Mrp/Planning/LotSizingCalculator.cs`
- `server/src/CoreAlign.Infrastructure/Mrp/Planning/DemandForecaster.cs`
- `server/src/CoreAlign.Infrastructure/Mrp/Planning/ActionMessageGenerator.cs`
- `server/src/CoreAlign.Infrastructure/Mrp/Planning/MrpPlanningDataLoader.cs` (+ `IMrpPlanningDataLoader`)
- `server/tests/CoreAlign.Application.Tests/Mrp/Planning/*` (all unit tests in §12)
**May add (additive, single surgical Edit each, re-Read first):** the 7 `Product` planning fields + `SetPlanningPolicy(...)` in `Product.cs`; `DemandForecaster` reuses existing forecast queries. **Does NOT** create entities, migrations, controllers, or persistence.

### GROUP B — Backend Workbench API + Persistence + Migration (fixes MRP-BUG-1, MRP-BUG-2)
**Owns / creates:**
- `server/src/CoreAlign.Domain/Entities/Mrp/{MrpPlanRun,MrpPlannedOrder,MrpActionMessage,MrpPegging}.cs`
- `server/src/CoreAlign.Domain/Interfaces/IMrpPlanRunRepository.cs`
- `server/src/CoreAlign.Application/Mrp/MrpPlanningContracts.cs`, `MrpPlanningHandlers.cs`, `MrpPlanningValidators.cs`
- `MrpPlanningService` (implements `IMrpPlanningService` from A) in `server/src/CoreAlign.Infrastructure/Mrp/MrpPlanningService.cs` — orchestrates loader→engine→persist
- `server/src/CoreAlign.Infrastructure/Repositories/MrpPlanRunRepository.cs`
- `server/src/CoreAlign.Infrastructure/Persistence/Configurations/Mrp/{MrpPlanRun,MrpPlannedOrder,MrpActionMessage,MrpPegging}Configuration.cs`
- `server/src/CoreAlign.Infrastructure/Persistence/Migrations/20260616000000_Phase72MrpPlanning.cs` (+ `.Designer.cs`) — idempotent, applied this pass
- `DocumentSequenceType.MrpPlanRunNumber` enum member (single surgical Edit to the enum)
- New methods in `server/src/CoreAlign.API/Controllers/MrpController.cs` (§10) — append-only, existing methods untouched
- DI registration for the new service/repository (in the existing Infrastructure DI module — single surgical Edit; mirrors `MrpService` registration)
- `server/tests/CoreAlign.Integration.Tests/MrpPlanningControllerIntegrationTests.cs` + cross-tenant `[Fact]`s appended to `CrossTenantIsolationTests`
- `docs/mrp-blockers.md` (ERP-MRP-001 snapshot follow-up; ERP-MRP-002 PO-line dates)
**Depends on A:** consumes A's VOs + `IMrpPlanningService` contract. **Does NOT** edit anything under A's folders or any frontend file. **Does NOT** touch ModelSnapshot/*.csproj.

### GROUP C — Frontend Workbench (fixes MRP-BUG-3, MRP-BUG-7)
**Owns / creates:**
- `src/pages/mrp/MrpWorkbenchPage.tsx` (new)
- `src/features/mrp/model/mrp-planning.types.ts`
- `src/features/mrp/api/mrpPlanningApi.ts`
- `src/features/mrp/hooks/{useMrpWorkbench,useMrpActionMessages,useMrpPlanRun}.ts`
- `src/features/mrp/ui/{MrpPlanningGrid,MrpTimePhasedChart,ActionMessageQueue,PeggingDrawer,KpiStrip}.tsx`
- `src/features/mrp/__tests__/*` (Group C tests)
- i18n keys `Mrp.*` in `tr.json` + `en.json`; `httpCache.ts` `TTL_RULES` regex additions
**May edit (additive, re-Read first):** `MrpDashboardPage.tsx` (link into workbench), `PurchaseRequisitionsPage.tsx` (Convert→PO action + reject/cancel reason prompt — wires existing `useConvertRequisition`), route registration. **Does NOT** touch any backend file. Consumes B's endpoints by contract (the DTO shapes in §8).

---

## 14. Blocker Follow-ups (write to `docs/mrp-blockers.md`)
- **ERP-MRP-001** (snapshot): `Phase72MrpPlanning` adds 7 product columns + 4 tables via idempotent SQL but does **not** touch `CoreAlignDbContextModelSnapshot.cs`. The EF runtime model includes them (via config), so the next `dotnet ef migrations add` will diff and try to re-add them. Snapshot owner must add the new entities + product properties to the snapshot before the next scaffold. Do NOT run `migrations add` until reconciled.
- **ERP-MRP-002** (data model): `PurchaseOrderLine` has no per-line due date; T1 buckets scheduled receipts by PO **header** `ExpectedDate`. Per-line PO due dates would tighten time-phasing — future migration.
- **MRP-BUG-5** (deferred): convert-to-PO still hardcodes tax 0% / FX 1.0; release flow inherits this until purchasing tax/FX is threaded through (track for a Purchasing sprint).
