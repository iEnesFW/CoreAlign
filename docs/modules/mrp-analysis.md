# MRP Module — Gap Analysis & Deepening Roadmap

> Status: 2026-06-12 · Owner: MRP module agent · Audience: planners, backend, frontend, product
> Source: synthesis of three parallel audits (current-state code audit, professional-MRP requirements gap, planner-UX gap) against the live tree on branch `v2_3_26`.

---

## 1. Executive Summary

CoreAlign markets an "MRP" module. In its current form it is **not MRP** — it is a **single-level, untimed reorder-point (min/max) replenishment engine** with a sales-history moving-average forecast and an approval inbox for the purchase requisitions it generates.

The engine computes, per stock-tracked product, a single scalar:

```
available = onHand − reserved + onOrder − committed
```

compares it to a reorder point (`ROP = SafetyStock + LeadTimeDays · avgDailyDemand · 1.2`), and — when `available < ROP` — emits a purchase requisition to top the item up to a max-stock target. There is **no BOM explosion, no time-phasing, no lot-sizing choice, no exception/action messages, no pegging, and no make-vs-buy routing**. The required data to do real MRP already exists in the domain (multi-level `ProductComponent` BOM tree, open `PurchaseOrder` lines as scheduled receipts, `OrderLine` committed/dependent demand, vendor lead times, `StockItem` on-hand) — the engine simply does not use most of it.

The planner UX mirrors this shallowness: it is a **read-only manager dashboard plus a card-per-row approval list**. A planner cannot run their core daily loop (open one screen → see the time-phased plan → act). Worse, two already-built backend capabilities are **unreachable from the UI**: `convert-requisition-to-PO` (the requisition workflow dead-ends at "Approved") and `demand-forecast`.

### Headline defects (fix-now, independent of the roadmap)

| Ref       | Severity     | Defect                                                                                                                                                                                                                                                                                                                        | Location                                                                              |
| --------- | ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| MRP-BUG-1 | **CRITICAL** | Auto-generate path calls `EnsureExistsAsync` then `ConsumeAsync` with **no `SaveChangesAsync` between them**. A fresh tenant with no pre-seeded `PurchaseRequisitionNumber` sequence throws `InvalidOperationException` → 500 on the very first MRP run. Masked in demo only because `DemoDataSeeder` pre-seeds the sequence. | `MrpService.cs:172–176` (manual path at `MrpHandlers.cs:89–91` is correct — it saves) |
| MRP-BUG-2 | HIGH         | No idempotency/dedup on suggestion generation. Re-running the same day (weekly job + manual trigger both fire) creates **duplicate requisitions** for the same shortages — violates INVARIANTS §59–61 natural-key idempotency.                                                                                                | `MrpService.GenerateRequisitionSuggestionsAsync`                                      |
| MRP-BUG-3 | HIGH         | `convertRequisition` API + `useConvertRequisition` hook are fully built but **called by no component** — the requisition workflow has no "convert to PO" button; an Approved PR is a dead-end.                                                                                                                                | `src/features/mrp/*`                                                                  |
| MRP-BUG-4 | MEDIUM       | Demand date is `OrderLine.UpdatedAtUtc` (any later row touch reshuffles demand into the wrong day/window); average divides by full calendar `windowDays` not days-with-data (sparse history diluted).                                                                                                                         | `MrpService.cs:62–68`                                                                 |
| MRP-BUG-5 | MEDIUM       | Convert-to-PO hardcodes `TaxRatePercent: 0`, `ExchangeRate: 1` — tax and FX silently lost on every requisition→PO conversion.                                                                                                                                                                                                 | `MrpHandlers.cs:218, 233`                                                             |
| MRP-BUG-6 | MEDIUM       | Dashboard recomputes the full candidate set (4 grouped queries + per-product loop) on **every** hit — no caching, contra CLAUDE.md caching guidance.                                                                                                                                                                          | `MrpService.GetDashboardAsync`                                                        |
| MRP-BUG-7 | LOW          | Reject/Cancel send `reason: null` hardcoded from the UI — `rejectReason`/`cancelReason` audit fields are dead by construction.                                                                                                                                                                                                | `PurchaseRequisitionsPage.tsx:133–134`                                                |

> **MRP-BUG-1 is the gating fix and is included in this sprint's tranche (Group A).** The rest are addressed opportunistically inside the tranche groups (BUG-2 by idempotency keying, BUG-3 by the workbench convert action, BUG-4 by the forecast upgrade, BUG-6 by workbench-run caching).

---

## 2. Capability Matrix

Legend: **EXISTS** = production-grade · **PARTIAL** = present but limited/naive · **MISSING** = absent.

| #   | Capability                                                                                                                                               | Status                                         | Evidence                                                                                                                                                                                                                                                       | Business Impact of the Gap                                                                                                                                                                                                   |
| --- | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | **Multi-level BOM explosion / dependent demand**                                                                                                         | **MISSING**                                    | `MrpService` never touches `ProductComponent` / `IProductComponentRepository.GetTreeForProductsAsync`. Demand read only from `OrderLine.QuantityShipped`.                                                                                                      | The defining feature of MRP. A spike in finished-goods orders generates **zero** component requisitions. Cannot plan a manufacturing bill at all.                                                                            |
| 2   | **Time-phased planning buckets** (gross req / scheduled receipts / projected-on-hand / net req / planned receipts / planned releases) + lead-time offset | **MISSING** (cosmetic undated projection only) | `ProjectStockBalanceAsync` dumps all on-order + committed into bucket 0, then flat-decays on-hand by `avgDaily`. `PurchaseOrderLine` has no dates; header `ExpectedDate` is ignored.                                                                           | Planners can't see _when_ a shortage hits or _when_ to release an order. Everything is "order now to max." Chronic over/under-ordering; no actionable release schedule.                                                      |
| 3   | **Lot-sizing policies** (lot-for-lot, FOQ, min/max, EOQ, POQ)                                                                                            | **PARTIAL** (one hardcoded min/max top-up)     | `suggestedQty = maxStockTarget − available`. `Product.MinOrderQuantity` / `MinStock` exist but are **not applied**. No order multiple / pack rounding. No per-product policy field.                                                                            | Ignores supplier MOQ/pack sizes (orders rejected/rounded by hand), ignores carrying-vs-ordering tradeoff (EOQ), can't do exact L4L for expensive/MTO parts. Excess inventory or supplier non-compliance.                     |
| 4   | **Demand & supply source completeness**                                                                                                                  | **PARTIAL**                                    | Firm sales demand ✓; safety stock ✓; forecast ✓ (but historical-shipment based); on-hand ✓; scheduled receipts ✓ (but **undated**); **dependent demand ✗** (no BOM); **firm planned orders ✗**. Forecast not netted against actual orders → double-count risk. | Missing dependent demand = can't plan manufacturing. Undated receipts = can't time-phase. Forecast-vs-actual un-netted = demand inflation/deflation.                                                                         |
| 5   | **Forecasting models + service-level safety stock**                                                                                                      | **PARTIAL** (naive MA + deterministic SS)      | Simple MA `total / windowDays` over 90d. Safety stock is a static field; ROP uses a hardcoded `1.2` magic factor. No σ, no z-factor, no service-level input, no trend/seasonality.                                                                             | Flat MA can't track trend/seasonality (peak-season stockouts, off-season overstock). Static SS + magic 1.2 doesn't adapt to demand/lead-time variability. No "I want 95% fill rate."                                         |
| 6   | **Exception / action messages** (release, reschedule-in/out, expedite, cancel, de-expedite)                                                              | **MISSING**                                    | No exception type/table/generation. Output is reorder candidates + auto-draft requisitions only. `DaysUntilStockOut` used only for dashboard sort.                                                                                                             | Exception messages are how planners run MRP **by exception**. Without them a planner must manually compare every open PO date to need date. Too-early/too-late POs never flagged → premature receipts (excess) or shortages. |
| 7   | **Pegging** (trace component need → parent demand)                                                                                                       | **MISSING** (N/A until #1)                     | Requisition lines carry only a free-text note. No parent→child chain.                                                                                                                                                                                          | Planners can't answer "why am I ordering this?" or assess impact of cancelling/changing a sales order on downstream components. Critical for MTO/ETO.                                                                        |
| 8   | **Make-vs-buy routing**                                                                                                                                  | **MISSING**                                    | Every candidate becomes a **purchase** requisition unconditionally. `Product` has no make/buy flag. Generic manufacturing work order does not exist (only off-limits GlassEnclosure work orders).                                                              | Manufactured items erroneously ordered from vendors; their components never planned. Fundamental routing defect for any MTS/MTO business.                                                                                    |
| 9   | **Firm planned orders / planner overrides**                                                                                                              | **MISSING**                                    | Planned orders aren't persisted re-plannable objects; each run creates new drafts with no dedup. Drafts are human-editable but no "firm" flag survives the next regeneration.                                                                                  | Re-running MRP spams duplicate requisitions; planners can't pin a decision against the next run. No stable planning loop.                                                                                                    |
| 10  | **ABC classification + differentiated policies**                                                                                                         | **MISSING**                                    | No ABC field on `Product`; no classification logic; uniform `1.2` factor for all items.                                                                                                                                                                        | A-items (high value/volume) get the same loose policy as C-items → cash tied in the wrong inventory, service failures on the items that matter.                                                                              |
| 11  | **Requisition workflow & FSM**                                                                                                                           | **EXISTS**                                     | `PurchaseRequisition` aggregate self-guards Draft→Submitted→Approved→Converted (+Reject/Cancel) via `InvalidOrderStatusTransitionException`; has `IHasConcurrencyToken` + `ISoftDeletable`.                                                                    | — (solid; reused as the planned-order "firm/release" sink).                                                                                                                                                                  |
| 12  | **Single-level reorder candidate batch**                                                                                                                 | **EXISTS** (N+1-safe)                          | `BuildCandidatesAsync` + `LoadCandidateBatchAsync` use 4 grouped `AsNoTracking` queries.                                                                                                                                                                       | — (reused as the netting primitive; extended, not replaced).                                                                                                                                                                 |
| 13  | **Planner workbench UX** (time-phased grid + action queue + pegging + firm/adjust before convert)                                                        | **MISSING**                                    | UI is a read-only dashboard + card-list approval inbox. `convert`/`demand-forecast` unreachable. Raw GUID product entry in the create form; CSS-bar projection chart while recharts ships.                                                                     | A planner cannot do their core daily job from this UI at all.                                                                                                                                                                |

---

## 3. Prioritized Roadmap (Tranches)

Tranches are ordered by **value-per-unit-effort to close the "is-it-really-MRP" gap**, and by dependency (each builds on the prior). Tranche T1 is **this sprint** (designed in §4 / `mrp-tranche-design`).

### T1 — Planning Engine Core + Planner Workbench (THIS SPRINT)

The single highest-value leap from reorder-point to real MRP. Delivers the three capabilities that _define_ MRP plus the UI to operate them:

- **(a) Multi-level BOM explosion** with low-level coding (capability #1, #4, #7-foundation).
- **(b) Time-phased MRP grid** — daily/weekly buckets: gross requirements, scheduled receipts, projected-on-hand, net requirements, planned order receipts, planned order **releases** with lead-time offset (capability #2).
- **(c) Lot-sizing policies** — lot-for-lot, FOQ, min/max, EOQ — selectable per product (capability #3).
- **(d) Action/exception messages** — Release, Reschedule-In, Reschedule-Out, Expedite, Cancel-Supply, Below-Safety, Projected-Stockout — as first-class persisted planner output (capability #6, #9-foundation).
- **(e) Forecasting upgrade** — exponential smoothing + **service-level safety stock** `z · σ_LT-demand · √LT` (capability #5).
- **(f) Planner Workbench UI** — time-phased grid + action-message queue + pegging drill-down + firm/adjust-before-convert; wires the orphaned `convert` + `demand-forecast` (capability #13; fixes MRP-BUG-3).
- **Plus** MRP-BUG-1 (SaveChanges), MRP-BUG-2 (idempotency keying), MRP-BUG-6 (workbench-run caching).

> T1 is BOM-explosion-aware but routes **all** planned orders to purchase requisitions (make-vs-buy deferred to T2). Pegging is captured during explosion (parent→child link persisted) but the rich pegging UI lands in T1 as a read-only drawer.

### T2 — Make-vs-Buy + Manufacturing Planned Orders

- `Product.ProcurementType` (Make / Buy) flag + migration.
- Generic (non-glass) **Work Order** planned-order sink for Make items; Buy items continue to requisitions.
- Routing in the explosion: a Make item with a BOM triggers a planned work order **and** explodes its components; a Buy item triggers a requisition and stops.
- Full pegging chains usable for change-impact ("what happens if I cancel SO-123?").

### T3 — Firm Planned Orders & Planner Overrides

- Promote planned orders to a first-class persisted, re-plannable object with a **Firm** flag the next regeneration respects.
- Manual qty/date overrides that survive re-planning.
- Net-change (incremental) planning option alongside regenerative.

### T4 — Forecasting Maturity + ABC

- Weighted MA, seasonality/trend (Holt-Winters), forecast-consumption (net forecast against actual orders).
- `Product.AbcClass` + automated ABC classification job + per-class default policies/service levels.
- Per-product **service-level target** input feeding safety stock.

### T5 — Multi-Warehouse / Multi-Plant Planning

- Warehouse-dimensioned netting and transfer-order suggestions (capability already latent in `StockItem` warehouse rollup).
- Distribution requirements (DRP) across sites.

### T6 — Capacity (CRP) & Scheduling

- Rough-cut capacity vs work-center load; finite/infinite scheduling of planned work orders.

---

## 4. This-Sprint Design Pointer

The concrete, buildable design for **T1** — new entities, persistence + idempotent migration, application services/handlers/contracts, API endpoints, repositories, frontend components/hooks, and the full test plan — is specified in **`docs/modules/mrp-tranche-design.md`**, split into three non-overlapping build groups:

- **Group A — Backend Planning Engine** (pure compute: BOM explosion, time-phasing, lot-sizing, exception generation, forecasting; fixes MRP-BUG-1).
- **Group B — Backend Workbench API + Persistence + Migration** (persists the plan run, planned orders, action messages, pegging; idempotent migration `Phase72MrpPlanning`; workbench endpoints; idempotency key fixes MRP-BUG-2).
- **Group C — Frontend Workbench** (time-phased grid, action-message queue, pegging drawer, firm/adjust-before-convert; wires orphaned `convert`/`demand-forecast`).

---

## 5. Snapshot / Migration Follow-up

Per INVARIANTS §28 and §84, T1 adds a hand-authored **idempotent** migration (`Phase72MrpPlanning`) and does **not** touch `CoreAlignDbContextModelSnapshot.cs` (owned by the parallel glass agent). The new entities are registered via `IEntityTypeConfiguration` (picked up by `ApplyConfigurationsFromAssembly`) and accessed via `_context.Set<T>()` to avoid editing `CoreAlignDbContext` where possible. The snapshot-reconcile follow-up (add the new entities' property lines to the model snapshot before the next `dotnet ef migrations add`) is documented in **`docs/mrp-blockers.md`** as **ERP-MRP-001**.
