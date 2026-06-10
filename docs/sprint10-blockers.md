# Sprint 10 Blockers

## ERP-033 Customer Merge — migration snapshot conflict

A separate concurrent agent owns `CoreAlignDbContextModelSnapshot.cs` plus the
`*.csproj` files. Group B added the `CustomerMergeLog` entity + EF
configuration + hand-authored migration `20260608010000_Phase52CustomerMergeAndConcurrencyTokens.cs`
WITHOUT regenerating the snapshot or the Designer file.

Follow-ups required from the owner of the snapshot:

1. Run `dotnet ef migrations script --idempotent` against a fresh DB to confirm
   the hand-authored migration applies cleanly, OR regenerate the Designer +
   snapshot via `dotnet ef migrations add Phase52CustomerMergeAndConcurrencyTokens`
   (after temporarily removing the hand-authored file).
2. Concurrency token columns on `customers` (xmin / rowversion) — task spec
   called for adding these but the in-flight snapshot edit risk prevented a
   safe Add. The handler uses `UpdatedAtUtc` as a soft concurrency token in
   the meantime (±1s tolerance). When the snapshot owner is free, switch to
   `xmin` / `IHasConcurrencyToken` for hard optimistic concurrency.
3. Add `<DbSet<CustomerMergeLog>>` accessor on `CoreAlignDbContext` (optional
   convenience — the repository uses `Set<CustomerMergeLog>()` so it works
   today via auto-discovery from `IEntityTypeConfiguration`).

## ERP-028 Product Variants — migration snapshot conflict (Group C)

The originally-specified migration name `Phase53ProductVariants` collides with the
existing `Phase53FxRates` migration. Filed instead as
`20260608020000_Phase61ProductVariants.cs` because the snapshot
(`CoreAlignDbContextModelSnapshot.cs`) is owned by a concurrent agent.

Follow-ups required from the snapshot owner:

1. Regenerate the snapshot — the new `ProductVariant` entity is picked up
   automatically from `ProductVariantConfiguration` so running
   `dotnet ef migrations add <NextPhase>` against a freshly applied DB will
   re-emit the snapshot with `product_variants` included. No manual edits
   needed.
2. Verify the `ProductVariant.ConcurrencyToken` (bigint, IHasConcurrencyToken)
   participates in the snapshot's concurrency-token block.
3. No DbSet accessor on the context is required — the repository uses
   `Set<ProductVariant>()` via auto-discovery.

## TEST-007 N+1 Regression Guard — deferred

CLAUDE.md rule 14 mandates a DbCommand-interceptor based round-trip counter
that asserts an upper bound for representative read endpoints. The Group A
testing-maturity pass did NOT add this fixture because the integration test
host owns its own `CoreAlignDbContext` registration which currently belongs to
a concurrent agent's edit scope (see service registration files in the
warning list).

Follow-up required:

1. Implement an `IDbCommandInterceptor` in
   `server/tests/CoreAlign.Integration.Tests/Infra/RoundTripCounterInterceptor.cs`
   that counts `ReaderExecutedAsync` / `NonQueryExecutedAsync` calls.
2. Register the interceptor on the test fixture's
   `DbContextOptionsBuilder` (via the existing
   `CoreAlignWebApplicationFactory`).
3. Author `[Fact]`s for the three highest-traffic read endpoints
   (`GET /api/v1/products`, `GET /api/v1/customers`,
   `GET /api/v1/orders`) asserting `counter.Reads <= expected`.

## Cross-cutting blockers (post-Sprint-10 verifier scan)

### Frontend missing npm packages (parallel-agent territory — root package.json)

Verifier surfaced these TS2307 errors across all 3 SPAs (admin/customer-portal/b2b):

- `@sentry/react` — Sprint 2 observability deferred install
- `react-markdown` — legal pages (LegalLayout)
- `remark-gfm` — legal pages
- `@testing-library/react` — TEST-002 portal vitest backfill
- `@testing-library/user-event` — same
- `@playwright/test` — Sprint 9 TEST-003 already flagged

All admin SPA + portal builds currently fail typecheck because of these. None can be auto-added per defensive guard (root package.json is shared with parallel agent). Owner of root package.json should add them in a single batch.

### Vitest jest-dom matchers leak into prod tsc

When `@testing-library/jest-dom` is added (above), the augmentation file `src/test/setup.ts` (or equivalent) must `import '@testing-library/jest-dom'` so `expect(...).toBeInTheDocument()` types resolve. Otherwise `tsc -b` chokes on portal test files. Currently `tsc -b` includes test files in the prod build — consider excluding via `tsconfig.app.json` `exclude: ["**/*.test.tsx", "**/__tests__/**"]`.

### 2 non-auto-fixable lint errors (parallel-agent territory)

- `src/features/glass-enclosure/hooks/useGlassProjectQueries.ts:294` — `@typescript-eslint/consistent-type-imports` (rewrite `import('...')` annotation to `type`-imported alias).
- `src/features/onboarding/hooks/useOnboarding.ts:81` — `react-hooks/set-state-in-effect` (move setState out of effect, use derived state or key reset).

Both files are outside Sprint 10 scope; flagged for the next polish pass.

### Backend integration test regressions (parallel-agent territory)

13 integration tests fail (all in parallel-agent's in-flight features):

- WarrantyContractsControllerIntegrationTests (3) — CoverageType enum JSON contract mismatch
- InstallationAcceptanceControllerIntegrationTests (4) — endpoints returning unexpected status
- PurchaseRequisitionsControllerIntegrationTests (2) — submit/approve flow
- MrpControllerIntegrationTests (1) — outbox path
- PaymentDispatcherIntegrationTests (1) — idempotency duplicate-reject
- InstallationAcceptanceServiceTests (1) — unit
- StandardChecklistTests (1) — unit

All in `Warranty/Installation/PurchaseRequisitions/MRP/Payment` modules under active development by parallel agent. Not blocking Sprint 10 scope (Application.Tests Group A/B/C — 1401/1403 pass).

## ✅ RESOLVED 2026-06-04

### TEST-007 N+1 round-trip interceptor — DONE

- NEW `server/tests/CoreAlign.Integration.Tests/Infrastructure/DbCommandRoundTripInterceptor.cs` — AsyncLocal-scoped counter via `DbCommandInterceptor` (Reader / NonQuery / Scalar increments).
- NEW `NPlusOneRegressionTests.cs` — 3 `[Fact]`s asserting round-trip budget for `GET /api/v1/{Customers,Products,Orders}` (≤6/≤6/≤8).
- `CoreAlignWebApiFactory.cs` extended via `IDbContextOptionsConfiguration<CoreAlignDbContext>` (non-invasive: prod options unchanged, interceptor added only in test fixture).
- All 3 N+1 tests pass; observed totals well within budget.
- INVARIANTS.md line 34 rewritten as ACTIVE rule (budget + assertion pattern).
