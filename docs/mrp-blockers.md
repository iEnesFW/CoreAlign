# MRP Module — Blockers & Follow-ups

Tracks deferred / snapshot-owner / cross-team items for the MRP deepening work
(Tranche T1, `docs/modules/mrp-tranche-design.md`). Append-only.

## ERP-MRP-001 — Phase72MrpPlanning snapshot reconcile [2026-06-12]

**Status:** OPEN (snapshot owner). Migration written + **APPLIED to the local dev
Postgres DB** by Group B (`dotnet ef database update Phase72MrpPlanning` →
"Applying migration '20260616000000_Phase72MrpPlanning'. Done."; verified via
`dotnet ef migrations list` — Phase72 is applied, not pending). Snapshot intentionally
untouched per the parallel-agent guard (INVARIANTS §28, §84). The four MRP entities are
registered via `IEntityTypeConfiguration` under
`CoreAlign.Infrastructure/Persistence/Configurations/Mrp/` and accessed via
`_context.Set<T>()` — **no `DbSet` added to `CoreAlignDbContext`, no DbContext edit**.
Sqlite integration tests build these tables from the EF model via `EnsureCreatedAsync`
(migrations are not run under Sqlite), so the suite exercises the real schema.

`20260616000000_Phase72MrpPlanning` adds — via idempotent `migrationBuilder.Sql`
(`ADD COLUMN/CREATE TABLE/CREATE INDEX IF NOT EXISTS`):

- `products`: `lot_sizing_policy`, `fixed_order_quantity`, `order_multiple`,
  `eoq_annual_demand`, `ordering_cost`, `holding_cost_rate`, `service_level_target`.
- Tables: `mrp_plan_runs`, `mrp_planned_orders`, `mrp_action_messages`, `mrp_peggings`
  (+ unique `(tenant_id, idempotency_key)` on runs, + per-table tenant-scoped indexes).

The EF runtime model includes these (registered via `IEntityTypeConfiguration`
picked up by `ApplyConfigurationsFromAssembly`), but `CoreAlignDbContextModelSnapshot.cs`
was **NOT** edited. Consequence: the next `dotnet ef migrations add` will diff
model-vs-snapshot and try to re-add these columns/tables (duplicate scaffold that
fails where Phase72 is already applied).

**Action (snapshot owner):** add the 4 MRP entities + the 7 `Product` planning
properties to `CoreAlignDbContextModelSnapshot.cs`. Until then, do **NOT** run
`migrations add` without first reconciling. Phase72's `.Designer.cs` uses the
empty-`BuildTargetModel` hand-authored pattern (INVARIANTS §28) — runtime `Migrate()`
applies via `Up()`; only tooling diffs against this migration are unreliable.

**RESOLVED [2026-06-12] — snapshot + Designer reconciled.** The snapshot now carries the
T1 MRP entities + the 7 `Product` planning columns (276→ model-as-of-Phase72), and
`20260616000000_Phase72MrpPlanning.Designer.cs` is a FULL `BuildTargetModel` (276 entities,
no longer the empty stub). Done via ef tooling, not hand-copy: Phase72.Designer was rebuilt
from the live snapshot body, then `ef migrations remove`/`add` regenerated Phase73 + the
snapshot deterministically (see ERP-MRP-006 and INVARIANTS stub-Designer-remove note).
`dotnet ef migrations has-pending-model-changes` → "No changes" (model == snapshot).

## ERP-MRP-002 — PurchaseOrderLine has no per-line due date [2026-06-12]

**Status:** OPEN (data-model enhancement, future migration).

Time-phasing in T1 buckets scheduled receipts by the PO **header** `ExpectedDate`
(fallback `today + remaining lead time`, else bucket 0) because `PurchaseOrderLine`
carries no date. This is a documented approximation. Per-line PO due dates would
let the engine phase partial receipts precisely and produce tighter Reschedule-In/Out
messages. Requires a `purchase_order_lines.expected_date` column (additive migration).

## MRP-BUG-5 — Convert-to-PO drops tax & FX [2026-06-12]

**Status:** DEFERRED (Purchasing sprint).

`ConvertRequisitionToPurchaseOrderHandler` hardcodes `TaxRatePercent: 0m` and
`ExchangeRate: 1m` (`MrpHandlers.cs:218, 233`). The T1 release flow
(`ReleasePlannedOrdersCommand`) reuses the requisition path and inherits this gap.
Threading the product/vendor tax rate and the tenant FX rate through requisition→PO
conversion is a Purchasing-module change; tracked here so the MRP release flow is
upgraded in lockstep when Purchasing addresses it.

## ERP-MRP-003 — Frontend workbench assumes Group B endpoint shapes [2026-06-12]

**Status:** OPEN (contract-sync; Group C frontend). No backend touched by Group C.

The planner workbench (`src/pages/mrp/MrpWorkbenchPage.tsx` + `src/features/mrp/*`)
consumes the 9 endpoints in `mrp-tranche-design.md` §10 by contract. The API client
(`src/features/mrp/api/mrpPlanningApi.ts`) is wired to:

- `GET /mrp/plan/preview?asOf=&bucket=&horizon=` → `ApiResponse<MrpPlanResult>`
- `GET /mrp/plan/item/{productId}?asOf=&bucket=&horizon=` → `ApiResponse<MrpItemPlan>`
- `GET /mrp/plan/runs?page=&pageSize=` → `ApiResponse<PagedResult<MrpPlanRun>>`
- `GET /mrp/action-messages?planRunId=&type=&severity=&supplierId=&includeDismissed=&page=&pageSize=`
  → `ApiResponse<PagedResult<MrpActionMessage>>`
- `GET /mrp/pegging/{planRunId}/{componentProductId}` → `ApiResponse<MrpPegging[]>`
- `POST /mrp/plan/commit` (body `{asOfDateUtc,bucketKind,horizonDays,operationId}`) → `ApiResponse<MrpPlanRun>`
- `POST /mrp/plan/{planRunId}/release` (body `{planRunId,plannedOrderIds[],operationId}`) → `ApiResponse<ReleaseResult>`
- `POST /mrp/planned-orders/{id}/firm` (body `{overrideQuantity,overrideDueDateUtc,operationId}`) → `ApiResponse<MrpPlannedOrder>`
- `POST /mrp/action-messages/{id}/dismiss` → `ApiResponse<void>`

The DTO field names in `src/features/mrp/model/mrp-planning.types.ts` (camelCase,
e.g. `MrpPlanResult.stockoutRiskCount/projectedStockoutCount/onOrderCount`,
`MrpItemPlan.buckets[]`, `MrpPegging.sourceOrderNumber/sourceParentProductName`,
`MrpActionMessage.relatedPlannedOrderId`) must match Group B's serialized DTOs.
Enums are serialized as **strings** (`bucketKind: "Day"|"Week"`, `severity`,
`actionType`, `sourcePolicy`/`policy`, `sourceKind`). If Group B emits integer enums
or different field names, update the model types + the `previewQuery` param mapping;
no UI logic change should be required. `operationId` is generated client-side
(`src/shared/lib/operationId.ts`, RFC-4122 v4 Guid) per INVARIANTS §26.

If an endpoint is not yet live, the corresponding `useQuery`/`useMutation` simply
returns empty/loading state — the UI degrades gracefully (empty grid, empty queue)
rather than blocking.

## ERP-MRP-004 — Planning engine column mapping + migration apply [2026-06-12]

**Status:** OPEN (snapshot owner / env). Group A backend planning engine landed.

Group A added the 7 `Product` planning fields (`LotSizingPolicy`,
`FixedOrderQuantity`, `OrderMultiple`, `EoqAnnualDemand`, `OrderingCost`,
`HoldingCostRate`, `ServiceLevelTarget`) + `Product.SetPlanningPolicy(...)`. These map
to the `products` columns created by Group B's `20260616000000_Phase72MrpPlanning`
via the global snake_case convention (`SnakeCaseNamingConvention.cs`) — no per-column
EF config needed, no separate migration written by Group A (would collide with
Phase72). The snapshot-reconcile obligation for these 7 columns is already tracked
under ERP-MRP-001.

**Migration apply (this pass):** Group A could not reach Postgres at the time of its
note. Group B subsequently APPLIED `20260616000000_Phase72MrpPlanning` to the local dev
Postgres (`Host=localhost;Port=5432;Database=corealign`, via the dev user-secret
connection string passed explicitly to `dotnet ef database update`). The 7 product
columns + 4 mrp_* tables are now live in dev. The migration is idempotent
(`ADD COLUMN/CREATE TABLE/CREATE INDEX IF NOT EXISTS`), so re-applying anywhere is a
no-op. See ERP-MRP-001 for the snapshot-reconcile obligation that still gates the next
`dotnet ef migrations add`.

**Forecast upgrade:** the planning engine forecasts with **exponential smoothing**
(`DemandForecaster`, α=0.3) + service-level safety stock `z·σ·√LT` (z from
`ZScore` lookup, interpolated). When `Product.ServiceLevelTarget > 0` the engine uses
`max(storedSafetyStock, z·σ·√LT)`; when `0` it keeps the stored `SafetyStock`
(regression-locked). σ is computed over **days-with-data padded to the window**
(zero-demand days count toward variance). Demand history is bucketed by the parent
order's `OrderDate` (not `OrderLine.UpdatedAtUtc`) — MRP-BUG-4 fix carried into the
engine's `MrpPlanningDataLoader.LoadDemandHistoryAsync`. The legacy `MrpService`
moving-average path is left untouched for the existing reorder-point flow.

## ERP-MRP-005 — T2 make-vs-buy frontend assumes Group B/A contract shapes [2026-06-12]

**Status:** OPEN (contract-sync; Group C frontend, T2). No backend touched by Group C.

T2 frontend (make-vs-buy) extends `src/features/mrp/*` + the product-edit form
assuming the following Group A/B contract additions. If any field name / enum
serialization differs, update `src/features/mrp/model/mrp-planning.types.ts` +
`src/features/products/model/product.types.ts` — no UI logic change should be required:

- **`Product.procurementType`** — string enum `"Buy" | "Make"` (default `Buy`).
  The product form (`ProductFormModal`) sends `procurementType` on both create and
  update. Group A/B must add the column (idempotent Phase73 — see below) + thread it
  through the product create/update command + DTO. Until live, the API echoes whatever
  it stores; the form defaults to `Buy` when the field is absent (`?? 'Buy'`).
- **`MrpPlannedOrder.procurementType`** (`"Buy" | "Make"`) + **`productionOrderId`**
  (`string | null`, the Make sink — a `PlannedProductionOrder` id, DISTINCT from any
  glass `GlassWorkOrder`) + existing `convertedRequisitionId` (the Buy sink). The drawer
  splits planned orders into Make / Buy groups and labels the release action
  per-type (Make → "Create production order"; Buy → "Convert to PO").
- **`MrpItemPlan.procurementType`** — drives the grid per-row badge + the workbench
  procurement filter (`All | Make | Buy`, client-side over `items`).
- **`MrpPlanResult.makeOrderCount` / `buyOrderCount`** — header counts shown in the grid
  toolbar.
- **`ReleaseResult.productionOrderIds` / `productionOrdersCreated`** — release now routes
  to two sinks (requisitions for Buy, planned production orders for Make).
- **Change-impact endpoint (NEW, not in T1):**
  `GET /mrp/change-impact/{planRunId}?sourceKind=&sourceId=` →
  `ApiResponse<ChangeImpactResult>` where `ChangeImpactResult` =
  `{ planRunId, demandSourceKind, demandSourceId, demandSourceLabel,
  affectedPlannedOrderCount, affectedNodes: ChangeImpactNode[] }` and
  `ChangeImpactNode` = `{ plannedOrderId, productId, productSku, productName,
  procurementType, quantity, dueDateUtc, lowLevelCode, parentProductId }`. Walks the
  persisted pegging chain from a demand source (a sales-order line) down to every
  downstream planned order it pegs to. The "Change impact" workbench tab is wired to it;
  if the endpoint is not yet live the `useMrpChangeImpactQuery` returns empty/loading and
  the tab degrades gracefully (prompt → empty state). The cache TTL rule `/mrp/plan/`
  does **not** cover `/mrp/change-impact/`; it currently falls through to no-cache
  (acceptable — it is an on-demand analysis). Add a `/mrp/change-impact/` TTL_RULES regex
  when the endpoint ships if revalidation is desired.

The demand-source dropdown in the impact tab is populated **client-side** from the
preview's pegging (`SalesOrder` sources only) as a stop-gap until Group B exposes a
"list demand sources for a plan run" endpoint; this avoids a blocking dependency.

## ERP-MRP-006 — Phase73MrpMakeVsBuy snapshot reconcile + apply [2026-06-12]

**Status:** OPEN (snapshot owner / env). Group A (T2 backend engine + domain) landed.

`20260617000000_Phase73MrpMakeVsBuy` adds — via idempotent `migrationBuilder.Sql`
(`ADD COLUMN/CREATE TABLE/CREATE INDEX IF NOT EXISTS`, `DROP ... IF EXISTS` in `Down`):

- `products.procurement_type` — `character varying(10) NOT NULL DEFAULT 'Buy'`
  (stored as string to match `ProductConfiguration.Property(ProcurementType).HasConversion<string>()`,
  mirroring `products.status` / `products.lot_sizing_policy`). Enum `ProcurementType`
  (Buy=0 default, Make=1).
- Table `planned_production_orders` — the Make-item planned-order sink, DISTINCT from
  the parallel-agent glass work orders (`GlassWorkOrder` untouched). Columns:
  `source_plan_run_id`, `product_id`, `low_level_code`, `quantity`, `due_date_utc`,
  `release_date_utc`, `estimated_unit_cost`, `source_policy`, `pegging_parent_product_id`,
  `pegging_source_order_line_id`, `status` + tenant/timestamps. Tenant-scoped indexes on
  `(tenant_id, source_plan_run_id)`, `(tenant_id, product_id)`, and the pegging order-line.

The entity (`CoreAlign.Domain.Entities.Manufacturing.PlannedProductionOrder`, table
`planned_production_orders`) is registered via `IEntityTypeConfiguration`
(`Persistence/Configurations/Mrp/PlannedProductionOrderConfiguration.cs`,
picked up by `ApplyConfigurationsFromAssembly`) and accessed via `_context.Set<T>()` /
the dedicated `IPlannedProductionOrderRepository` — **no `DbSet` added to
`CoreAlignDbContext`, no DbContext edit, `CoreAlignDbContextModelSnapshot.cs` NOT touched**
(parallel-agent guard, INVARIANTS §28/§84). Sqlite integration tests build the table from
the EF model via `EnsureCreatedAsync` (migrations not run under Sqlite), so the suite
exercises the real schema.

**Migration apply (this pass — Group B):** APPLIED to the local dev Postgres
(`Host=localhost;Port=5432;Database=corealign`, dev user-secret connection string passed
explicitly to `dotnet ef database update 20260617000000_Phase73MrpMakeVsBuy` →
"Applying migration '20260617000000_Phase73MrpMakeVsBuy'. Done."; verified via
`dotnet ef migrations list` — Phase73 listed after Phase72 with no "(Pending)" marker).
The `products.procurement_type` column + `planned_production_orders` table are now live in
dev. The migration is idempotent (`ADD COLUMN/CREATE TABLE/CREATE INDEX IF NOT EXISTS`),
so re-applying anywhere is a no-op.

**RESOLVED [2026-06-12] — snapshot + Designer reconciled (was: Still OPEN, snapshot owner).**
`products.procurement_type` + the `PlannedProductionOrder` entity are now in
`CoreAlignDbContextModelSnapshot.cs` (277 entities), and `20260617000000_Phase73MrpMakeVsBuy.Designer.cs`
is a FULL `BuildTargetModel` (277, no longer a stub). Regenerated via ef tooling:
the snapshot was rolled back to model-as-of-Phase72 (via the now-full Phase72.Designer),
the stub Phase73 deleted, then `dotnet ef migrations add Phase73MrpMakeVsBuy` re-scaffolded
a clean T2-only diff (`procurement_type` + `planned_production_orders` + 3 indexes) and
updated the snapshot. The ef-generated file (timestamp `20260612113504`, which sorts BEFORE
Phase72) was renamed to `20260617000000` and its `[Migration("…")]` attribute updated to keep
chain order; the generated `AddColumn defaultValue: ""` Up was replaced with the idempotent
`migrationBuilder.Sql` form using `DEFAULT 'Buy'` (an empty string would fail `ProcurementType`
enum parsing on existing rows). `has-pending-model-changes` → "No changes". The dev DB already
has Phase73 applied (same migration ID, idempotent same-DDL), so history stays consistent.
**Env note:** `dotnet ef database update` via the design-time factory is blocked here
(`28P01 password authentication failed for user "design"` — the factory uses throwaway
`corealign_design`/design:design creds; the real dev `corealign` connection string is a
user-secret unavailable in the sandbox). Runtime `Migrate()` applies on app startup.

**Change-impact endpoint (as shipped by Group B, supersedes the assumed shape in
ERP-MRP-005):** `GET /api/v1/mrp/change-impact/{planRunId}/{sourceOrderLineId}` →
`ApiResponse<ChangeImpactResultDto>` where `ChangeImpactResultDto` =
`{ planRunId, sourceOrderLineId, downstreamSupply: ChangeImpactSupplyOrderDto[] }` and
`ChangeImpactSupplyOrderDto` = `{ productId, lowLevelCode, sinkKind ("PurchaseRequisition"
| "ProductionOrder"), quantity, dueDateUtc, releaseDateUtc, directParentProductId }`.
It re-runs the committed run's preview (`asOf/bucket/horizon` from the stored
`MrpPlanRun`) and traces the in-memory plan via `MrpChangeImpactAnalyzer` (pure,
unit-tested) from the sales-order line's root product down every BOM-explosion pegging
link. Group C must align `mrp-planning.types.ts` to this shape (string enum `sinkKind`,
route uses path-param `sourceOrderLineId`, not a query `sourceKind/sourceId`).
The upstream "why does this component order exist?" chain is a separate endpoint:
`GET /api/v1/mrp/pegging-chain/{planRunId}/{componentProductId}` →
`ApiResponse<MrpPeggingDto[]>` (walks persisted peggings component→parent→…→sales order).

## ERP-MRP-007 — Group C change-impact contract reconciled to Group B [2026-06-12]

**Status:** RESOLVED (Group C review+fix pass).

Group C had wired the change-impact frontend to the **assumed** ERP-MRP-005 shape
(`GET /mrp/change-impact/{planRunId}?sourceKind=&sourceId=` →
`{ demandSourceKind, demandSourceId, demandSourceLabel, affectedPlannedOrderCount,
affectedNodes[] }`). Group B actually shipped the ERP-MRP-006 shape
(`GET /api/v1/mrp/change-impact/{planRunId}/{sourceOrderLineId}` →
`{ planRunId, sourceOrderLineId, downstreamSupply[] }`, supply node =
`{ productId, lowLevelCode, sinkKind, quantity, dueDateUtc, releaseDateUtc,
directParentProductId }`). The two never matched → the live endpoint would have 404'd
on the missing `{sourceOrderLineId}` path segment, and the view would have crashed on
`result.affectedNodes` (undefined). Verified against the backend route
(`MrpController.ChangeImpact`, `change-impact/{planRunId:guid}/{sourceOrderLineId:guid}`)
and the integration test (`MrpMakeVsBuyIntegrationTests.ChangeImpact_lists_downstream_supply_for_the_run`,
which calls `/api/v1/mrp/change-impact/{run.Id}/{Guid.NewGuid()}`).

**Fix (frontend only, no backend/glass touched):**
- `mrp-planning.types.ts`: replaced `ChangeImpactNode`/old `ChangeImpactResult` with
  `ChangeImpactSupplyOrder` + the real `ChangeImpactResult`
  (`{ planRunId, sourceOrderLineId, downstreamSupply }`); added `OrderSinkKind`
  (`"PurchaseRequisition" | "ProductionOrder"`) + `sinkKindToProcurementType`
  (`ProductionOrder→Make`, `PurchaseRequisition→Buy`); `ChangeImpactParams` now
  `{ planRunId, sourceOrderLineId }`.
- `mrpPlanningApi.changeImpact`: path-param route `${base}/${planRunId}/${sourceOrderLineId}`
  (no query string), matching the `pegging` two-path-param pattern.
- `useMrpChangeImpactQuery(planRunId, sourceOrderLineId)` — dropped the spurious
  `sourceKind` arg (route is always a sales-order line).
- `ChangeImpactView`: renders `downstreamSupply`; derives the Make/Buy badge from
  `sinkKind`; SKU/name enriched client-side via an optional `productInfo`
  (`productId → {sku,name}`) lookup with `productId` fallback; summary uses an optional
  `sourceLabel` (the sales-order number) falling back to the line id.
- `MrpWorkbenchPage`: builds the `productInfo` lookup from `plan.items`, removed the
  degenerate single-option source-kind selector, passes the analyzed source label.
- Test `MrpMakeVsBuy.test.tsx` (`ChangeImpactView` describe) rewritten to the real shape
  (red→green): sink-kind→badge mapping + product-id fallback + empty/prompt states.

Backend verified green: solution build 0/0; `CoreAlign.Application.Tests` MRP 112/112;
`CoreAlign.Integration.Tests` change-impact 6/6. Frontend: vitest 146/146, tsc 0 errors in
changed files, eslint `--max-warnings=0` clean. The unused `Mrp.Workbench.ChangeImpact.SourceKind`
i18n key is left in both `en.json`/`tr.json` (parity preserved, 4110/4110); harmless dead key.

## ERP-MRP-008 — T3 Firm Planned Orders, Overrides, Net-change + table-name fix [2026-06-12]

**Status:** Backend SHIPPED + tested (161 Application + 19 Integration green). DB-finalized
(Phase74 + snapshot reconciled, empty-probe clean). `dotnet ef database update` env-blocked
(design-time factory creds; runtime `Migrate()` applies). Frontend T3 UI = follow-up.

**Shipped (T3 backend):**
- **Firm-as-fixed-supply.** `MrpPlanningDataLoader.LoadFirmedSupplyAsync` loads firmed buy
  orders (`MrpPlannedOrder` `IsFirmed && !IsReleased`) + firm production orders
  (`PlannedProductionOrder` `Status==Firm`) into `MrpPlanningSnapshot.FirmedSupply`. The engine
  (`BucketFirmedSupply`) nets them as scheduled receipts (due-bucket) so re-planning honours
  firmed decisions and emits no duplicate orders; make firm orders ALSO feed `plannedReleases`
  (release-bucket) so component demand still explodes. See INVARIANTS (firmed-as-supply).
- **Override audit.** `MrpPlannedOrder` + `PlannedProductionOrder` gained `OriginalQuantity`
  + `OriginalDueDateUtc`, captured in `Firm()` (first-value-wins). Migration Phase74.
- **Net-change.** `MrpPlanningMode.NetChange` on `CommitMrpPlanCommand` → `CommitAsync`
  skips creating a redundant run when the plan equals the latest committed run
  (`FindUnchangedLatestRunAsync` signature compare). **Limitation:** this is skip-if-unchanged,
  NOT true incremental regeneration. True net-change (re-plan only changed items) needs a
  persistent "current plan" table / in-place update — the run-scoped append-only model can't
  support it (baseline degrades each run). Deferred to T5+ (see INVARIANTS net-change note).
  Practical trigger is rare (asOf-relative safety-stock dates + horizon-sensitive lot-sizing).

**Pre-existing prod bug FIXED (uncovered by T3):** the 4 Phase72 MRP aggregates
(`MrpPlanRun/MrpPlannedOrder/MrpActionMessage/MrpPegging`) had NO `ToTable` in their EF config,
so the model mapped them to SINGULAR snake_case names (`mrp_planned_order`) while the
hand-authored Phase72 SQL created PLURAL tables (`mrp_planned_orders`). Sqlite tests
(EnsureCreated from model) passed because they were self-consistent (singular), HIDING a prod
defect: against the real Postgres (plural) the model would query a non-existent singular table
(`relation "mrp_planned_order" does not exist`). Fix: `ToTable("<plural>")` added to all 4
configs (index/PK/FK names re-derive to plural, matching Phase72 SQL); Phase74 carries a
defensive `ALTER TABLE IF EXISTS <singular> RENAME TO <plural>` (no-op on real Postgres,
self-heals a stale EnsureCreated dev DB) ahead of the audit `ADD COLUMN IF NOT EXISTS`.
`PlannedProductionOrder` was already correct (T2 had explicit `ToTable`). The snapshot +
Phase72/73/74 Designers were all reconciled to plural; `has-pending-model-changes` → "No changes".

**Follow-up:** T3 frontend (override badge / "overridden from X" display, firmed-survives
indicator, net-change mode toggle in the workbench). Not blocking backend deploy.

## MRP-BUG-5 RESOLVED [2026-06-15]

Convert requisition→PO now resolves per-line tax (Product.TaxRateId → TaxRate.RatePercent, 0 if none, batch-loaded/cached, no N+1) and the PO ExchangeRate via IFxRateResolver.ResolveAsync→BuyingRate (1 for TRY/base or on resolver failure, graceful). ConvertRequisitionToPurchaseOrderHandler gained IProductRepository + ITaxRateRepository + optional IFxRateResolver?/ITenantContext? (DI-registered in prod, null-safe in tests). 3 new tests (tax carried, foreign-currency FX resolved, base-currency uses 1 without calling resolver). The MRP release flow (requisition path) now converts with correct tax/FX. 194 MRP/Requisition/Convert/Purchase tests pass.
